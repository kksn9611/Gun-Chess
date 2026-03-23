using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // 직렬화 필드 — Inspector에서 런타임 값을 확인할 수 있다
    // ─────────────────────────────────────────────────────────────
    [SerializeField] private UnitData      unitData;
    [SerializeField] private float         currentHp;
    [SerializeField] private float         currentMp;
    [SerializeField] private float         currentAtt;
    [SerializeField] private float         currentDef;
    [SerializeField] private float         currentAttRange;
    [SerializeField] private float         currentAttSpd;
    [SerializeField] private float         currentMoveSpd;
    [SerializeField] private TileScript    currentTile;    // 현재 이 유닛이 점유 중인 타일
    [SerializeField] private Vector2Int    currentCoord;   // 현재 그리드 좌표 (타일 논리 좌표)
    [SerializeField] private Team          currentTeam;
    [SerializeField] private UnitState     currentState = UnitState.Idle;
    [SerializeField] private UnitController currentTarget; // 현재 추격/공격 중인 적 유닛

    // 이동 코루틴 참조 — StopCoroutine으로 외부에서 명시적 중단하기 위해 보관
    // EnterAttackState(), Die() 등 상태 전환 시 반드시 이 참조를 통해 정리한다
    private Coroutine _moveCoroutine;

    // 유닛 데이터 읽기 전용 프로퍼티
    public UnitData UnitData { get => unitData; }

    // ─────────────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// UnitSpawner.SpawnUnit()에서 호출된다.
    /// 스탯을 UnitData로부터 복사하고, 타일을 점유한 뒤
    /// EnterIdleState()로 AI를 시작한다.
    /// </summary>
    public void Initialize(UnitData data, TileScript spawnTile, Team team)
    {
        unitData        = data;
        currentHp       = unitData.maxHp;
        currentMp       = 0f;
        currentAtt      = unitData.att;
        currentDef      = unitData.def;
        currentAttRange = unitData.attRange;
        currentAttSpd   = unitData.attSpd;
        currentMoveSpd  = unitData.moveSpd;
        currentTeam     = team;
        currentCoord    = spawnTile.GridCoordinate;
        currentTile     = spawnTile;
        spawnTile.IsOccupied = true; // 소환 타일 점유 선언

        Debug.Log($"[소환] {unitData.unitName} @ {currentCoord} ({currentTeam}팀)");

        // 소환 직후 Idle 상태로 진입해 첫 번째 행동을 결정한다.
        // Update()의 Idle case에는 코드가 없으므로, 이 호출이 없으면 유닛이 영원히 대기한다.
        EnterIdleState();
    }

    // ─────────────────────────────────────────────────────────────
    // Update — 상태 감시 전용, 실제 동작은 코루틴·EnterXxx()에서 처리
    // ─────────────────────────────────────────────────────────────
    private void Update()
    {
        switch (currentState)
        {
            case UnitState.Idle:
                // EnterIdleState()는 상태 전환 시점에 1회 호출된다.
                // 매 프레임 여기서 재호출하지 않는다 — 그렇게 하면 O(N²) 탐색이 발생한다.
                break;

            case UnitState.Moving:
                // MoveCoroutine()이 이동 전체를 담당한다. Update에서 추가 처리 없음.
                break;

            case UnitState.Attacking:
                // TODO: AttackCoroutine()이 담당 예정 (미구현)
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 상태 전환 메서드
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 어떤 상태에서든 Idle로 복귀할 때 호출한다.
    /// 가장 가까운 타겟을 찾아 이동 또는 공격 상태로 즉시 전환한다.
    /// 타겟이 없으면 Idle을 유지하며 대기한다.
    /// </summary>
    public void EnterIdleState()
    {
        currentState  = UnitState.Idle;
        currentTarget = FindClosestTarget();

        // 타겟 없음: 적 전멸 또는 전투 전 상태. 그냥 대기.
        if (currentTarget == null) return;

        int distance = HexCoordCal.GetDistance(currentCoord, currentTarget.currentCoord);

        if (distance <= currentAttRange)
            EnterAttackState(); // 이미 사거리 안 → 바로 공격
        else
            EnterMoveState();   // 사거리 밖 → 추격 이동
    }

    /// <summary>
    /// 공격 상태로 전환한다.
    /// 이동 코루틴이 실행 중이라면 먼저 중단하고 오브젝트 위치를 정렬한다.
    /// </summary>
    public void EnterAttackState()
    {
        // LerpToTile 중간에 전환될 경우 오브젝트가 타일 사이에 떠 있을 수 있다.
        // StopMovement()가 현재 타일 위치로 스냅해준다.
        StopMovement();

        currentState = UnitState.Attacking;
        // TODO: _attackCoroutine = StartCoroutine(AttackCoroutine()); — 미구현
    }

    /// <summary>
    /// 이동 상태로 전환한다.
    /// 이전 이동 코루틴이 있으면 정리 후 새 코루틴을 시작한다.
    /// </summary>
    public void EnterMoveState()
    {
        // 이미 이동 중이더라도 타겟이 바뀌었을 수 있으므로 재시작한다
        StopMovement();

        currentState   = UnitState.Moving;
        _moveCoroutine = StartCoroutine(MoveCoroutine());
    }

    // ─────────────────────────────────────────────────────────────
    // 이동 시스템
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 실행 중인 이동 코루틴을 중단하고, 오브젝트 위치를 현재 타일로 스냅한다.
    /// EnterAttackState(), Die(), 강제 재시작 시 반드시 호출한다.
    /// </summary>
    private void StopMovement()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        // Lerp 도중 중단됐다면 오브젝트가 두 타일 사이에 위치한다.
        // 현재 논리 타일의 정확한 위치로 강제 이동한다.
        if (currentTile != null)
            transform.position = currentTile.transform.position;
    }

    /// <summary>
    /// 타겟을 향해 한 칸씩 이동하는 메인 코루틴.
    /// 매 타일 도착마다 타겟 유효성 → 사거리 → 목적지 → 경로를 순서대로 재평가한다.
    /// </summary>
    private IEnumerator MoveCoroutine()
    {
        while (true)
        {
            // ── Step 1. 타겟 유효성 확인 ──────────────────────────
            // 이동 중 타겟이 사망하거나 제거됐으면 새 타겟을 찾으러 Idle 복귀
            if (currentTarget == null || currentTarget.currentHp <= 0)
            {
                Debug.Log($"[이동] {unitData.unitName}: 타겟 소멸 → Idle 복귀");
                EnterIdleState();
                yield break; // 코루틴 종료
            }

            // ── Step 2. 사거리 재확인 ─────────────────────────────
            // 한 칸 이동할 때마다 사거리 안에 들어왔는지 체크한다
            int distToTarget = HexCoordCal.GetDistance(currentCoord, currentTarget.currentCoord);
            if (distToTarget <= currentAttRange)
            {
                Debug.Log($"[이동] {unitData.unitName}: 사거리 진입(거리={distToTarget}) → 공격 전환");
                EnterAttackState();
                yield break; // 코루틴 종료 (EnterAttackState 내부에서 StopMovement 호출)
            }

            // ── Step 3. 이동 목적지 결정 ──────────────────────────
            // 타겟 타일의 인접 타일 중 비어있고 나에게 가장 가까운 타일을 선택
            TileScript destination = GetBestAdjacentTile(currentTarget.currentTile);

            if (destination == null)
            {
                // 타겟 주변 모든 타일이 다른 유닛으로 막혀있다.
                // 1프레임 기다렸다가 다시 시도 — 다른 유닛이 이동하면 자리가 생긴다.
                Debug.Log($"[이동] {unitData.unitName}: 목적지 없음(주변 막힘) → 1프레임 대기");
                yield return null;
                continue;
            }

            // ── Step 4. 경로 계산 ─────────────────────────────────
            // 정적 메서드라 다중 유닛이 동시에 호출해도 충돌 없음
            // 매 타일마다 재계산해 항상 최신 장애물 상태를 반영한다
            List<TileScript> path = Pathfinder.FindPath(currentTile, destination);

            if (path == null || path.Count == 0)
            {
                // 길이 완전히 막힌 경우 (예: 사방이 유닛으로 둘러싸임)
                // 1프레임 대기 후 재시도
                Debug.Log($"[이동] {unitData.unitName}: 경로 없음 → 1프레임 대기");
                yield return null;
                continue;
            }

            // ── Step 5. 다음 타일 선택 및 사전 점유 확인 ─────────
            // path[0] = 지금 당장 이동할 한 칸 (경로 전체를 한 번에 가지 않는다)
            TileScript nextTile = path[0];

            // Step 3~4 계산 후 다른 유닛이 먼저 해당 타일을 점유했을 수 있다
            if (nextTile.IsOccupied)
            {
                // 선점됐다면 이번 프레임은 건너뛰고 다음 프레임에 경로 재계산
                Debug.Log($"[이동] {unitData.unitName}: 다음 타일 {nextTile.GridCoordinate} 선점됨 → 재계산");
                yield return null;
                continue;
            }

            // ── Step 6. 타일 점유 상태 갱신 ──────────────────────
            // Lerp 이전에 처리한다:
            //   출발 타일 해제 → 이동 중에도 다른 유닛이 해당 자리로 올 수 있게 허용
            //   도착 타일 점유 → 이동 중 다른 유닛의 중복 진입 차단
            currentTile.IsOccupied = false;
            nextTile.IsOccupied    = true;

            // ── Step 7. 물리적 이동 (Lerp) ───────────────────────
            // LerpToTile이 완료될 때까지 이 줄에서 대기 (매 프레임 yield됨)
            yield return StartCoroutine(LerpToTile(nextTile));

            // ── Step 8. 논리 좌표 확정 ───────────────────────────
            // Lerp 완료 후 내부 상태를 새 타일로 업데이트
            currentTile  = nextTile;
            currentCoord = nextTile.GridCoordinate;

            // ─────────────────────────────────────────────────────
            // 루프 처음으로 돌아가 타겟·사거리·경로를 다시 평가한다
            // ─────────────────────────────────────────────────────
        }
    }

    /// <summary>
    /// 타겟 타일(targetTile)의 인접 타일(Neighbors) 중에서
    /// 비어있고(IsOccupied == false) 자신에게 가장 가까운 타일을 반환한다.
    /// 모든 인접 타일이 점유되어 있으면 null을 반환한다.
    /// </summary>
    private TileScript GetBestAdjacentTile(TileScript targetTile)
    {
        TileScript best     = null;
        int        bestDist = int.MaxValue;

        foreach (TileScript neighbor in targetTile.Neighbors)
        {
            // 점유된 타일은 후보에서 제외
            if (neighbor.IsOccupied) continue;

            // 자신이 이미 해당 인접 타일에 있는 경우:
            // 사거리 체크(Step 2)에서 공격 전환이 먼저 돼야 하지만 방어적으로 처리
            if (neighbor == currentTile) return currentTile;

            // 현재 위치 → 후보 타일까지의 헥스 거리 (Heuristic, 빠른 계산)
            int dist = HexCoordCal.GetDistance(currentCoord, neighbor.GridCoordinate);

            if (dist < bestDist)
            {
                bestDist = dist;
                best     = neighbor;
            }
        }

        return best; // null = 모든 인접 타일이 점유됨
    }

    /// <summary>
    /// 현재 위치에서 목표 타일 위치까지 선형 보간(Lerp)으로 이동한다.
    /// moveSpd(타일/초) 값으로 소요 시간을 결정한다.
    /// 이동 완료 시 코루틴이 종료된다.
    /// </summary>
    private IEnumerator LerpToTile(TileScript tile)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos   = tile.transform.position;

        // moveSpd = 초당 이동할 수 있는 타일 수
        // duration = 타일 1칸 이동에 걸리는 시간(초)
        // 예) moveSpd=3 → 0.33초/칸,  moveSpd=1 → 1초/칸
        float duration = 1f / currentMoveSpd;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            // t = 0.0(출발) ~ 1.0(도착) 사이의 보간 비율
            float t = elapsed / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            elapsed += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 부동소수점 누적 오차로 endPos에 정확히 도달하지 못할 수 있으므로 스냅
        transform.position = endPos;
    }

    // ─────────────────────────────────────────────────────────────
    // 타겟 탐색
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 적 팀 유닛 중 가장 가까운 생존 유닛을 반환한다.
    /// 거리가 같을 경우 공격 사거리가 짧은 적(더 위협적인 적)을 우선한다.
    /// 적이 없거나 모두 사망했으면 null을 반환한다.
    /// </summary>
    public UnitController FindClosestTarget()
    {
        UnitController closestTarget = null;
        int            minDistance   = int.MaxValue;

        // GetEnemiesOf: 자신 팀의 반대 팀 리스트를 IReadOnlyList로 반환 (직접 수정 불가)
        IReadOnlyList<UnitController> targetList = UnitManager.Instance.GetEnemiesOf(currentTeam);

        foreach (UnitController target in targetList)
        {
            // null이거나 이미 사망한(HP <= 0) 유닛은 건너뜀
            if (target == null || target.currentHp <= 0) continue;

            int distance = HexCoordCal.GetDistance(currentCoord, target.currentCoord);

            if (distance < minDistance)
            {
                minDistance   = distance;
                closestTarget = target;
            }
            else if (distance == minDistance && closestTarget != null)
            {
                // 거리 동률: 사거리가 짧은 적(공격 범위가 좁은 = 더 위협적)을 우선
                if (target.currentAttRange < closestTarget.currentAttRange)
                    closestTarget = target;
            }
        }

        if (closestTarget != null)
            Debug.Log($"[타겟] {currentTeam}[{unitData.unitName}] → {closestTarget.currentTeam}[{closestTarget.unitData.unitName}]");

        return closestTarget;
    }

    // ─────────────────────────────────────────────────────────────
    // 전투 — 데미지 수신 / 사망
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 공격 유닛이 호출한다. 방어력(def)을 반영해 실제 데미지를 계산하고 HP를 감소시킨다.
    /// HP가 0 이하가 되면 Die()를 호출한다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        // def = % 데미지 감소율. def=20 → 실제 데미지 = damage × 0.80
        float actualDamage = damage * (1f - currentDef / 100f);
        currentHp -= actualDamage;

        if (currentHp <= 0f)
        {
            currentHp = 0f;
            Die();
        }
    }

    /// <summary>
    /// 유닛 사망 처리.
    /// 모든 코루틴 중단 → 타일 해제 → 팀 목록에서 제거 → 오브젝트 파괴 순으로 처리한다.
    /// </summary>
    public void Die()
    {
        // 이동·공격 등 실행 중인 모든 코루틴을 즉시 중단
        // (StopMovement 대신 StopAllCoroutines를 써서 공격 코루틴도 함께 정리)
        StopAllCoroutines();
        _moveCoroutine = null; // 참조 초기화 (이미 중단됐지만 null로 명시)

        currentState = UnitState.Dead;

        // 점유 중인 타일을 해제해 다른 유닛이 해당 타일로 이동 가능하게 한다
        if (currentTile != null)
        {
            currentTile.IsOccupied = false;
            currentTile            = null;
        }

        UnitManager.Instance.RemoveUnit(this, currentTeam);
        // TODO: UnitManager.Instance.OnUnitDied(currentTeam); — 승패 판정 (Phase 1-4에서 구현)

        // 3초 후 오브젝트 파괴 (사망 이펙트/애니메이션 재생 시간 확보용)
        Destroy(gameObject, 3f);
    }
}
