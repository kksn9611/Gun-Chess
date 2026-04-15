using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛의 전투 AI
/// FSM 상태 전환과 코루틴 관리.
/// 같은 GameObject의 UnitController에서 스탯·데미지 등 데이터를 참조.
/// </summary>
[RequireComponent(typeof(UnitController))]
public class UnitAI : MonoBehaviour
{
    private UnitController unit;
    private Coroutine moveCoroutine;
    [Header("AI 상태")]
    [SerializeField] private UnitState currentState = UnitState.Idle; // 유닛 상태
    [SerializeField] private UnitController currentTarget; // 현재 타겟 적 유닛

    /// <summary>현재 AI 상태</summary>
    public UnitState CurrentState => currentState;
    /// <summary>현재 타겟</summary>
    public UnitController CurrentTarget => currentTarget;

    private void Awake()
    {
        unit = GetComponent<UnitController>();
    }

    private void OnEnable()
    {
        if (unit != null && unit.IsOnBench) return;

        BattleManager.OnBattleStart += EnterIdleState;

        // 전투 중에 활성화된 유닛 처리
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

    // FSM 루프 //

    /// <summary>
    /// 상태별 매 프레임 처리가 필요할 때 사용.
    /// 현재는 빈 상태.
    /// </summary>
    private void Update()
    {
        switch (currentState)
        {
            case UnitState.Idle:
                break;
            case UnitState.Moving:
                break;
            case UnitState.Attacking:
                break;
            case UnitState.Casting:
                break;
            case UnitState.Dead:
                break;
        }
    }

    // 상태 전환 //

    /// <summary>
    /// Idle로 복귀할 때 호출.
    /// 가장 가까운 타겟을 찾아 이동 또는 공격 상태로 즉시 전환.
    /// 타겟이 없으면 Idle을 유지하며 대기.
    /// </summary>
    public void EnterIdleState()
    {
        // 벤치 유닛은 전투 AI를 실행하지 않는다
        if (unit.IsOnBench) return;

        currentState  = UnitState.Idle;
        currentTarget = FindClosestTarget();

        if (currentTarget == null) return;

        int distance = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);

        if (distance <= unit.CurrentAttRange)
            EnterAttackState(); // 이미 사거리 안 → 바로 공격
        else
            EnterMoveState();   // 사거리 밖 → 추격 이동
    }
    /// <summary>
    /// 공격 상태로 전환
    /// </summary>
    public void EnterAttackState()
    {
        StopMovement();
        currentState = UnitState.Attacking;
        StartCoroutine(AttackCoroutine());
    }
    /// <summary>
    /// 이동 상태로 전환.
    /// 이전 이동 코루틴이 있으면 정리 후 새 코루틴을 시작한다.
    /// </summary>
    public void EnterMoveState()
    {
        StopMovement();
        currentState  = UnitState.Moving;
        moveCoroutine = StartCoroutine(MoveCoroutine());
    }
    /// <summary>
    /// 사망 상태로 전환. UnitController.Die()에서 호출.
    /// </summary>
    public void EnterDeadState()
    {
        StopAllCoroutines();
        moveCoroutine = null;
        currentState  = UnitState.Dead;
    }
    /// <summary>
    /// 라운드 전환 시 AI 상태를 초기화.
    /// UnitController.ResetForNewRound()에서 호출.
    /// </summary>
    public void ResetState()
    {
        StopAllCoroutines();
        moveCoroutine  = null;
        currentState   = UnitState.Idle;
        currentTarget  = null;
    }

    // 이동 제어 //

    /// <summary>
    /// 실행 중인 이동을 중단, 위치를 현재 타일로 스냅.
    /// EnterAttackState(), Die(), 강제 재시작 시 호출.
    /// </summary>
    private void StopMovement()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
        // Lerp 도중 중단됐다면 오브젝트가 두 타일 사이에 위치한다.
        // 현재 논리 타일의 정확한 위치로 강제 이동한다.
        if (unit.CurrentHexTile != null)
            transform.position = unit.CurrentHexTile.transform.position;
    }

    // 타겟 탐색 //

    /// <summary>
    /// 가장 가까운 적 유닛을 우선순위에 따라 반환.
    /// 없으면 null 반환.
    /// </summary>
    public UnitController FindClosestTarget()
    {
        UnitController closestTarget = null;
        int minDistance   = int.MaxValue;

        IReadOnlyList<UnitController> targetList = UnitManager.Instance.GetEnemiesOf(unit.CurrentTeam);

        foreach (UnitController target in targetList)
        {
            if (target == null || target.CurrentHp <= 0) continue;

            int distance = HexCoordCal.GetDistance(unit.CurrentCoord, target.CurrentCoord);

            if (distance < minDistance)
            {
                minDistance    = distance;
                closestTarget = target;
            }
            else if (distance == minDistance && closestTarget != null)
            {
                // 거리 동률: 사거리가 짧은 적을 우선
                if (target.CurrentAttRange < closestTarget.CurrentAttRange)
                    closestTarget = target;
            }
        }

        return closestTarget;
    }

    // 이동 코루틴 //

    /// <summary>
    /// 타겟을 향해 한 칸씩 이동.
    /// 타일 도착마다 타겟 유효성 → 사거리 → 목적지 → 경로 평가 실행.
    /// </summary>
    private IEnumerator MoveCoroutine()
    {
        while (true)
        {
            // 이동 중 타겟이 사망하거나 제거됐으면 새 타겟을 찾으러 Idle 복귀
            if (currentTarget == null || currentTarget.CurrentHp <= 0)
            {
                EnterIdleState();
                yield break;
            }

            // 사거리 재확인 — 한 칸 이동할 때마다 사거리 안에 들어왔는지 체크
            int distToTarget = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);
            if (distToTarget <= unit.CurrentAttRange)
            {
                EnterAttackState();
                yield break;
            }

            // 타겟 타일의 인접 타일 중 비어있고 나에게 가장 가까운 타일을 선택
            TileScript destination = GetBestAdjacentTile(currentTarget.CurrentHexTile);

            if (destination == null)
            {
                // 타겟 주변 모든 타일이 막혀있다면
                // 1프레임 기다렸다가 다시 시도
                yield return null;
                continue;
            }

            // 경로 계산 — 항상 최신 상태를 반영
            List<TileScript> path = Pathfinder.FindPath(unit.CurrentHexTile, destination);

            if (path == null || path.Count == 0)
            {
                // 길이 완전히 막힌 경우 대기 후 재시도
                yield return null;
                continue;
            }

            // 이동 타일 선택 후 점유 확인
            TileScript nextTile = path[0];
            if (nextTile.IsOccupied)
            {
                // 선점됐다면 이번 프레임은 건너뛰고 다음 프레임에 경로 재계산
                yield return null;
                continue;
            }

            // 타일 점유 상태 갱신
            //   출발 타일 해제 → 이동 중에도 다른 유닛이 해당 자리로 올 수 있게 허용
            //   도착 타일 점유 → 이동 중 다른 유닛의 중복 진입 차단
            unit.CurrentHexTile.IsOccupied = false;
            nextTile.IsOccupied            = true;

            // 물리적 이동 — LerpToTile이 완료될 때까지 대기
            yield return StartCoroutine(LerpToTile(nextTile));

            // 이동 후 내부 상태를 새 타일로 업데이트
            unit.SetCurrentTile(nextTile);
            currentTarget = FindClosestTarget(); // 가까운 타겟 재설정
            yield return new WaitForSeconds(0.05f); // 약간의 이동 대기시간
        }
    }
    /// <summary>
    /// targetTile의 Neighbors 중에서
    /// 비어있고 자신에게 가장 가까운 타일을 반환.
    /// </summary>
    private TileScript GetBestAdjacentTile(TileScript targetTile)
    {
        if (targetTile == null) return null;

        TileScript best     = null;
        int        bestDist = int.MaxValue;

        foreach (TileScript neighbor in targetTile.Neighbors)
        {
            if (neighbor.IsOccupied) continue;

            // 자신이 이미 해당 인접 타일에 있는 경우
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
    /// <summary>
    /// 현재 위치에서 목표 타일 위치까지 Leap 이동.
    /// 1/moveSpd 값으로 소요 시간을 결정.
    /// </summary>
    private IEnumerator LerpToTile(TileScript tile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos   = tile.transform.position;

        float duration = 1f / unit.CurrentMoveSpd;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = endPos;
    }

    // 공격 코루틴 //

    /// <summary>
    /// 공격 속도에 따라 공격하고, searchInterval마다 타겟 재탐색.
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        float attackCooldown = 1f / unit.CurrentAttSpd;
        float searchInterval = 0.2f;
        float searchTimer    = 0f;

        while (true)
        {
            // null, 사망 체크
            if (currentTarget == null || currentTarget.CurrentHp <= 0)
            {
                EnterIdleState();
                yield break;
            }

            // 타겟이 사거리 밖으로 이동했으면 추격
            int distToTarget = HexCoordCal.GetDistance(unit.CurrentCoord, currentTarget.CurrentCoord);
            if (distToTarget > unit.CurrentAttRange)
            {
                EnterMoveState();
                yield break;
            }

            // 공격 실행 — 데미지 적용, MP 획득, 이벤트 발행은 UnitController가 처리
            unit.PerformAttack(currentTarget);

            // MP가 충분하면 스킬 시전 후 복귀
            if (unit.CanCastSkill())
            {
                yield return StartCoroutine(unit.CastSkillCoroutine());
                // 스킬 시전 후 다시 Idle → 타겟 탐색부터 재시작
                EnterIdleState();
                yield break;
            }

            float cooldownTimer = 0f;

            // 공격 쿨타임
            while (cooldownTimer < attackCooldown)
            {
                float deltaTime = Time.deltaTime;
                cooldownTimer += deltaTime;
                searchTimer   += deltaTime;

                // searchInterval마다 타겟 재검색
                if (searchTimer >= searchInterval)
                {
                    searchTimer = 0f;
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
