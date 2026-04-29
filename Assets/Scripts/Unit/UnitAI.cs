using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Unit combat AI.
/// FSM state transitions and async task management.
/// References stats/damage data from UnitController on the same GameObject.
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitAI : MonoBehaviour
{
    private UnitController unit;
    private CancellationTokenSource aiCts;

    [Header("AI State")]
    [SerializeField] private UnitState currentState = UnitState.Idle; // Unit state
    [SerializeField] private UnitController currentTarget; // Current target enemy

    public event Action<UnitState> OnStateChanged; // State change event
    /// <summary>Current AI state</summary>
    public UnitState CurrentState => currentState;
    /// <summary>Current target</summary>
    public UnitController CurrentTarget => currentTarget;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
    }

    private void OnEnable()
    {
        if (unit != null && unit.IsOnBench) return;

        BattleManager.OnBattleStart += OnBattleStartHandler;
    }
    private void OnDisable()
    {
        BattleManager.OnBattleStart -= OnBattleStartHandler;
    }
    private void OnBattleStartHandler()
    {
        if (unit != null && unit.IsOnBench) return;
        StartBattleWithStaggerAsync(ResetToken()).Forget();
    }
    /// <summary> start battle with random dealy./// </summary>
    private async UniTaskVoid StartBattleWithStaggerAsync(CancellationToken ct)
    {
        int randomDelay = UnityEngine.Random.Range(0, 200);
        await UniTask.Delay(randomDelay, cancellationToken: ct);
        EnterIdleState();
    }
    private void OnDestroy()
    {
        CancelAI();
    }

    /// <summary>Cancel all running AI tasks.</summary>
    private void CancelAI()
    {
        aiCts?.Cancel();
        aiCts?.Dispose();
        aiCts = null;
    }

    /// <summary>Create a fresh CTS and return its token.</summary>
    private CancellationToken ResetToken()
    {
        CancelAI();
        aiCts = new CancellationTokenSource();
        return aiCts.Token;
    }

    // FSM Loop //

    // State Transitions //

    /// <summary>
    /// Return to Idle.
    /// Find closest target and immediately transition to Move or Attack.
    /// If no target exists, remain Idle.
    /// </summary>
    public void EnterIdleState()
    {
        // Bench units do not run combat AI
        if (unit.IsOnBench) return;

        currentState  = UnitState.Idle;
        OnStateChanged?.Invoke(CurrentState);
        currentTarget = FindClosestTarget();

        if (currentTarget == null) return;

        int distance = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);

        if (distance <= unit.Stats.CurrentAttRange)
            EnterAttackState(); // Already in range -> attack
        else
            EnterMoveState();   // Out of range -> chase
    }
    /// <summary>
    /// Transition to Attack state.
    /// </summary>
    public void EnterAttackState()
    {
        unit.Movement.StopMovement();
        currentState = UnitState.Attacking;
        OnStateChanged?.Invoke(CurrentState);
        AttackAsync(ResetToken()).Forget();
    }
    /// <summary>
    /// Transition to Move state.
    /// Cancel any existing AI task before starting a new one.
    /// </summary>
    public void EnterMoveState()
    {
        unit.Movement.StopMovement();
        currentState  = UnitState.Moving;
        OnStateChanged?.Invoke(CurrentState);
        MoveAsync(ResetToken()).Forget();
    }
    /// <summary>
    /// Transition to Cast state. Returns to Idle after cast completes.
    /// </summary>
    public void EnterCastState()
    {
        currentState = UnitState.Casting;
        OnStateChanged?.Invoke(currentState);
        CastAndReturnToIdleAsync(ResetToken()).Forget();
    }

    /// <summary>Wrapper: cast skill then return to Idle.</summary>
    private async UniTask CastAndReturnToIdleAsync(CancellationToken ct)
    {
        await unit.CastSkillAsync();
        if (ct.IsCancellationRequested) return;
        EnterIdleState();
    }
    /// <summary>
    /// Transition to Dead state. Called by UnitController.Die().
    /// </summary>
    public void EnterDeadState()
    {
        CancelAI();
        unit.Animator.ResumeAnimation();
        unit.Animator.ResetTriggers();
        currentState  = UnitState.Dead;
        OnStateChanged?.Invoke(CurrentState);
    }
    /// <summary>
    /// Reset AI state on round transition.
    /// Called by UnitController.ResetForNewRound().
    /// </summary>
    public void ResetState()
    {
        CancelAI();
        currentState   = UnitState.Idle;
        OnStateChanged?.Invoke(CurrentState);
        currentTarget  = null;
    }

    // Target Search //

    /// <summary>
    /// Return the closest enemy unit by priority. Returns null if none found.
    /// </summary>
    public UnitController FindClosestTarget()
    {
        UnitController closestTarget = null;
        int minDistance   = int.MaxValue;

        IReadOnlyList<UnitController> targetList = UnitManager.Instance.GetEnemiesOf(unit.CurrentTeam);

        foreach (UnitController target in targetList)
        {
            if (target == null || target.Stats.CurrentHp <= 0) continue;

            int distance = HexCoordCal.GetDistance(unit.CurrentCoord, target.CurrentCoord);

            if (distance < minDistance)
            {
                minDistance    = distance;
                closestTarget = target;
            }
            else if (distance == minDistance && closestTarget != null)
            {
                // Tiebreaker: prefer enemy with shorter attack range
                if (target.Stats.CurrentAttRange < closestTarget.Stats.CurrentAttRange)
                    closestTarget = target;
            }
        }

        return closestTarget;
    }

    // Move Async //

    /// <summary>
    /// Move toward target one tile at a time.
    /// On each arrival: validate target -> check range -> pick destination -> pathfind.
    /// </summary>
    private async UniTask MoveAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Target died or was removed -- return to Idle to find a new target
            if (currentTarget == null || currentTarget.Stats.CurrentHp <= 0)
            {
                EnterIdleState();
                return;
            }

            // Re-check range -- may have entered attack range after one step
            int distToTarget = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);
            if (distToTarget <= unit.Stats.CurrentAttRange)
            {
                EnterAttackState();
                return;
            }

            // Pick the closest unoccupied neighbor of the target tile
            TileScript destination = GetBestAdjacentTile(currentTarget.CurrentHexTile);

            if (destination == null)
            {
                // All tiles around target are blocked -- wait one frame and retry
                await UniTask.Yield(ct);
                continue;
            }

            // Pathfind -- always reflects latest occupancy state
            List<TileScript> path = Pathfinder.FindPath(unit.CurrentHexTile, destination);

            if (path == null || path.Count == 0)
            {
                // Path completely blocked -- wait and retry
                await UniTask.Yield(ct);
                continue;
            }

            // Check if next tile was claimed by another unit
            TileScript nextTile = path[0];
            if (nextTile.IsOccupied)
            {
                // Skip this frame, recalculate next frame
                await UniTask.Yield(ct);
                continue;
            }

            // Update tile occupancy
            //   Release departure tile -> allow other units to enter
            //   Occupy arrival tile -> block duplicate entry during lerp
            unit.CurrentHexTile.IsOccupied = false;
            nextTile.IsOccupied            = true;
            // Update internal state to new tile before physical move
            unit.SetCurrentTile(nextTile);

            // Physical movement -- wait for lerp to complete
            await unit.Movement.LerpToTileAsync(nextTile);
            if (ct.IsCancellationRequested) return;

            currentTarget = FindClosestTarget(); // Re-evaluate closest target
            await UniTask.Delay(50, cancellationToken: ct); // Brief movement delay
        }
    }
    /// <summary>
    /// Return the closest unoccupied neighbor of targetTile.
    /// </summary>
    private TileScript GetBestAdjacentTile(TileScript targetTile)
    {
        if (targetTile == null) return null;

        TileScript best     = null;
        int        bestDist = int.MaxValue;

        foreach (TileScript neighbor in targetTile.Neighbors)
        {
            if (neighbor.IsOccupied) continue;

            // Already on this neighbor tile
            if (neighbor == unit.CurrentHexTile) return unit.CurrentHexTile;

            int dist = HexCoordCal.GetDistance(unit.CurrentCoord, neighbor.GridCoordinate);

            if (dist < bestDist)
            {
                bestDist = dist;
                best     = neighbor;
            }
        }
        return best;
    }


    // Attack Async //

    /// <summary>
    /// Attack based on attack speed, re-search target every searchInterval.
    /// </summary>
    private async UniTask AttackAsync(CancellationToken ct)
    {
        float searchInterval = 0.2f;
        float searchTimer    = 0f;

        while (!ct.IsCancellationRequested)
        {
            // Null / death check
            if (currentTarget == null || currentTarget.Stats.CurrentHp <= 0)
            {
                EnterIdleState();
                return;
            }

            // Target moved out of range -> chase
            int distToTarget = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);
            if (distToTarget > unit.Stats.CurrentAttRange)
            {
                EnterMoveState();
                return;
            }
            // Look target
            unit.Movement.LookAtTarget(currentTarget.transform, ct).Forget();
            // Cast skill if MP is full
            if (unit.Stats.CanCastSkill())
            {
                EnterCastState();
                return;
            }
            // Execute attack -- damage, MP gain, events handled by UnitController
            unit.PerformAttack(currentTarget);

            // Attack cooldown (refresh attack speed each loop)
            float attackCooldown = 1f / unit.Stats.CurrentAttSpd;
            float cooldownTimer  = 0f;

            while (cooldownTimer < attackCooldown)
            {
                float deltaTime = Time.deltaTime;
                cooldownTimer += deltaTime;
                searchTimer   += deltaTime;


                // Re-search target + refresh attack speed every searchInterval
                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
                    attackCooldown = 1f / unit.Stats.CurrentAttSpd;

                    UnitController searchedTarget = FindClosestTarget();
                    if (searchedTarget != null && searchedTarget != currentTarget)
                    {
                        currentTarget = searchedTarget;
                        unit.Movement.LookAtTarget(currentTarget.transform, ct).Forget();
                    }
                }
                await UniTask.Yield(ct);
            }
        }
    }
}
