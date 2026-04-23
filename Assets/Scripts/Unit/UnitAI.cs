using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit combat AI.
/// FSM state transitions and coroutine management.
/// References stats/damage data from UnitController on the same GameObject.
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitAI : MonoBehaviour
{
    private UnitController unit;
    private Coroutine moveCoroutine;

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

        BattleManager.OnBattleStart += EnterIdleState;

        // Handle units enabled mid-battle
        if (BattleManager.Instance != null &&
            BattleManager.Instance.CurrentPhase == BattleManager.Phase.Battle &&
            unit != null && !unit.IsOnBench)
        {
            EnterIdleState();
        }
    }
    private void OnDisable()
    {
        BattleManager.OnBattleStart -= EnterIdleState;
    }

    // FSM Loop //

    // /// <summary>
    // /// Per-frame processing per state, if needed.
    // /// </summary>
    // private void Update()
    // {
    //     switch (currentState)
    //     {
    //         case UnitState.Idle:
    //             break;
    //         case UnitState.Moving:
    //             break;
    //         case UnitState.Attacking:
    //             break;
    //         case UnitState.Casting:
    //             break;
    //         case UnitState.Dead:
    //             break;
    //     }
    // }

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
            EnterAttackState(); // Already in range → attack
        else
            EnterMoveState();   // Out of range → chase
    }
    /// <summary>
    /// Transition to Attack state.
    /// </summary>
    public void EnterAttackState()
    {
        unit.Movement.StopMovement();
        currentState = UnitState.Attacking;
        OnStateChanged?.Invoke(CurrentState);
        StartCoroutine(AttackCoroutine());
    }
    /// <summary>
    /// Transition to Move state.
    /// Stop any existing move coroutine before starting a new one.
    /// </summary>
    public void EnterMoveState()
    {
        unit.Movement.StopMovement();
        currentState  = UnitState.Moving;
        OnStateChanged?.Invoke(CurrentState);
        moveCoroutine = StartCoroutine(MoveCoroutine());
    }
    /// <summary>
    /// Transition to Cast state. Returns to Idle after cast completes.
    /// </summary>
    public void EnterCastState()
    {
        StopAllCoroutines();
        moveCoroutine = null;
        currentState = UnitState.Casting;
        OnStateChanged?.Invoke(currentState);
        StartCoroutine(CastAndReturnToIdle());
    }

    /// <summary>Wrapper: cast skill then return to Idle.</summary>
    private IEnumerator CastAndReturnToIdle()
    {
        yield return StartCoroutine(unit.CastSkillCoroutine());
        EnterIdleState();
    }
    /// <summary>
    /// Transition to Dead state. Called by UnitController.Die().
    /// </summary>
    public void EnterDeadState()
    {
        StopAllCoroutines();
        moveCoroutine = null;
        currentState  = UnitState.Dead;
        OnStateChanged?.Invoke(CurrentState);
    }
    /// <summary>
    /// Reset AI state on round transition.
    /// Called by UnitController.ResetForNewRound().
    /// </summary>
    public void ResetState()
    {
        StopAllCoroutines();
        moveCoroutine  = null;
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

    // Move Coroutine //

    /// <summary>
    /// Move toward target one tile at a time.
    /// On each arrival: validate target → check range → pick destination → pathfind.
    /// </summary>
    private IEnumerator MoveCoroutine()
    {
        while (true)
        {
            // Target died or was removed — return to Idle to find a new target
            if (currentTarget == null || currentTarget.Stats.CurrentHp <= 0)
            {
                EnterIdleState();
                yield break;
            }

            // Re-check range — may have entered attack range after one step
            int distToTarget = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);
            if (distToTarget <= unit.Stats.CurrentAttRange)
            {
                EnterAttackState();
                yield break;
            }

            // Pick the closest unoccupied neighbor of the target tile
            TileScript destination = GetBestAdjacentTile(currentTarget.CurrentHexTile);

            if (destination == null)
            {
                // All tiles around target are blocked — wait one frame and retry
                yield return null;
                continue;
            }

            // Pathfind — always reflects latest occupancy state
            List<TileScript> path = Pathfinder.FindPath(unit.CurrentHexTile, destination);

            if (path == null || path.Count == 0)
            {
                // Path completely blocked — wait and retry
                yield return null;
                continue;
            }

            // Check if next tile was claimed by another unit
            TileScript nextTile = path[0];
            if (nextTile.IsOccupied)
            {
                // Skip this frame, recalculate next frame
                yield return null;
                continue;
            }

            // Update tile occupancy
            //   Release departure tile → allow other units to enter
            //   Occupy arrival tile → block duplicate entry during lerp
            unit.CurrentHexTile.IsOccupied = false;
            nextTile.IsOccupied            = true;
            // Update internal state to new tile before physical move
            unit.SetCurrentTile(nextTile);

            // Physical movement — wait for lerp to complete
            yield return StartCoroutine(unit.Movement.LerpToTile(nextTile));

            currentTarget = FindClosestTarget(); // Re-evaluate closest target
            yield return new WaitForSeconds(0.05f); // Brief movement delay
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


    // Attack Coroutine //

    /// <summary>
    /// Attack based on attack speed, re-search target every searchInterval.
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        float searchInterval = 0.2f;
        float searchTimer    = 0f;

        while (true)
        {
            // Null / death check
            if (currentTarget == null || currentTarget.Stats.CurrentHp <= 0)
            {
                EnterIdleState();
                yield break;
            }

            // Target moved out of range → chase
            int distToTarget = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);
            if (distToTarget > unit.Stats.CurrentAttRange)
            {
                EnterMoveState();
                yield break;
            }

            // Execute attack — damage, MP gain, events handled by UnitController
            unit.PerformAttack(currentTarget);

            // Cast skill if MP is full
            if (unit.Stats.CanCastSkill())
            {
                currentState = UnitState.Casting;
                OnStateChanged?.Invoke(currentState);
                yield return StartCoroutine(unit.CastSkillCoroutine());
                EnterIdleState();
                yield break;
            }

            // Attack cooldown (refresh attack speed each loop)
            float attackCooldown = 1f / unit.Stats.CurrentAttSpd;
            float cooldownTimer  = 0f;

            while (cooldownTimer < attackCooldown)
            {
                float deltaTime = Time.deltaTime;
                cooldownTimer += deltaTime;
                searchTimer   += deltaTime;

                if (currentTarget != null)
                {
                    unit.Movement.LookAtTarget(currentTarget.transform);
                }

                // Re-search target + refresh attack speed every searchInterval
                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
                    attackCooldown = 1f / unit.Stats.CurrentAttSpd;

                    UnitController searchedTarget = FindClosestTarget();
                    if (searchedTarget != null && searchedTarget != currentTarget)
                    {
                        currentTarget = searchedTarget;
                    }
                }
                yield return null;
            }
        }
    }
}
