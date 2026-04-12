using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour
{
    [SerializeField] private UnitData      unitData;
    [SerializeField] private float         currentHp;
    [SerializeField] private float         currentMp;
    [SerializeField] private float         currentAtt;
    [SerializeField] private float         currentDef;
    [SerializeField] private float         currentAttRange;
    [SerializeField] private float         currentAttSpd;
    [SerializeField] private float         currentMoveSpd;
    [SerializeField] private float         currentMaxMp;
    [SerializeField] private float         mpGainOnAttack;
    [SerializeField] private float         mpGainOnHit;
    [SerializeField] private BaseSkill     skill;          // 이 유닛이 사용하는 스킬 (null이면 스킬 없음)
    [SerializeField] private TileScript currentTile;    // 현재 이 유닛이 점유 중인 타일
    [SerializeField] private BenchTileScript currentBenchTile; // 대기석 타일. null = 전장에 있음
    [SerializeField] private Vector2Int    currentCoord;   // 현재 타일 좌표
    [SerializeField] private Team          currentTeam;
    [SerializeField] private UnitState     currentState = UnitState.Idle;
    [SerializeField] private UnitController currentTarget; // 현재 추격/공격 중인 적 유닛
    private Coroutine moveCoroutine;


    public Transform uiAnchor;
    public static event Action<UnitController> OnUnitSpawned;
    /// <summary>HP 변경 시 실행. (현재HP, 최대HP)</summary>
    public event Action<float, float> OnHpChanged;
    /// <summary>MP 변경 시 실행. (현재MP, 최대MP)</summary>
    public event Action<float, float> OnMpChanged;
    /// <summary>벤치 ↔ 전장 전환 시 실행. (true = 벤치에 있음)</summary>
    public event Action<bool> OnBenchState;
    private void SetHp(float value)
    {
        currentHp = Mathf.Clamp(value, 0f, unitData.maxHp);
        OnHpChanged?.Invoke(currentHp, unitData.maxHp);
    }

    private void SetMp(float value)
    {
        currentMp = Mathf.Clamp(value, 0f, currentMaxMp);
        OnMpChanged?.Invoke(currentMp, currentMaxMp);
    }

    // 유닛 데이터 읽기 전용 프로퍼티
    public UnitData UnitData { get => unitData; }
    public BaseTile CurrentTile => currentBenchTile != null ? (BaseTile)currentBenchTile : (BaseTile)currentTile;
    public Team CurrentTeam => currentTeam;
    public bool IsOnBench => currentBenchTile != null;

    /// <summary>
    /// 유닛을 헥스 타일에 배치한다. (준비 페이즈 배치용)
    /// clearCurrent=false : 스왑 시 원래 위치의 IsOccupied를 해제하지 않음
    /// </summary>
    public void PlaceOnTile(TileScript newTile, bool clearCurrent = true)
    {
        StopAllCoroutines();
        moveCoroutine = null;
        if (clearCurrent)
        {
            if (currentTile != null)      currentTile.IsOccupied = false;
            if (currentBenchTile != null) currentBenchTile.IsOccupied = false;
        }
        // 헥스에 배치 = 벤치 참조 해제 (clearCurrent 여부와 무관)
        currentTile      = newTile;
        currentBenchTile = null;
        currentCoord     = newTile.GetCoordinate();
        newTile.IsOccupied = true;
        OnBenchState?.Invoke(false);
        StartCoroutine(MoveToTileSmoothly(newTile.transform.position, clearCurrent ? 0.1f : 0.2f));
    }

    /// <summary>
    /// 유닛을 벤치 슬롯에 배치한다.
    /// clearCurrent=false : 스왑 시 원래 위치의 IsOccupied를 해제하지 않음
    /// </summary>
    public void PlaceOnBench(BenchTileScript slot, bool clearCurrent = true)
    {
        StopAllCoroutines();
        moveCoroutine = null;
        if (clearCurrent)
        {
            if (currentTile != null)      currentTile.IsOccupied = false;
            if (currentBenchTile != null) currentBenchTile.IsOccupied = false;
        }
        // 벤치에 배치 = 헥스 참조 해제 (clearCurrent 여부와 무관)
        currentTile      = null;
        currentBenchTile = slot;
        currentCoord = slot.GetCoordinate();
        slot.IsOccupied  = true;
        OnBenchState?.Invoke(true);
        StartCoroutine(MoveToTileSmoothly(slot.transform.position, 0.2f));
    }

    private IEnumerator MoveToTileSmoothly(Vector3 targetPosition, float duration)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null; 
        }
        // 위치 보정
        transform.position = targetPosition;
    }

    /// <summary>
    /// UnitSpawner.SpawnUnit()에서 호출된다.
    /// 스탯을 UnitData로부터 복사하고, 타일을 점유한다.
    /// BaseTile을 받으므로 전장(TileScript)과 벤치(BenchTileScript) 모두 지원한다.
    /// </summary>
    public void Initialize(UnitData data, BaseTile spawnTile, Team team)
    {
        unitData        = data;
        currentAtt      = unitData.att;
        currentDef      = unitData.def;
        currentAttRange = unitData.attRange;
        currentAttSpd   = unitData.attSpd;
        currentMoveSpd  = unitData.moveSpd;
        currentMaxMp    = unitData.maxMp;
        mpGainOnAttack  = unitData.mpGainOnAttack;
        mpGainOnHit     = unitData.mpGainOnHit;
        skill           = unitData.skill;
        currentTeam     = team;
        currentCoord    = spawnTile.GetCoordinate();
        spawnTile.IsOccupied = true;

        // 타일 종류에 따라 전장/벤치 참조를 분기 설정
        if (spawnTile is BenchTileScript benchTile)
        {
            currentBenchTile = benchTile;
            currentTile      = null;
        }
        else if (spawnTile is TileScript hexTile)
        {
            currentTile      = hexTile;
            currentBenchTile = null;
        }

        OnUnitSpawned?.Invoke(this);
        SetHp(unitData.maxHp);
        SetMp(0f);
        Debug.Log($"{unitData.unitName} @ {currentCoord} ({currentTeam}팀)");
        // 전투 시작은 BattleManager.OnBattleStart 이벤트
        // Initialize() 시점에 EnterIdleState() 호출 XX

    }
    /// <summary>
    /// BattleManager 이벤트 구독
    /// 활성화될 때 BattleManager.OnBattleStart 이벤트를 구독
    /// </summary>
    private void OnEnable()
    {
        if (IsOnBench) return;
        BattleManager.OnBattleStart += EnterIdleState;

        // 전투 중에 소환된 유닛 처리
        if (BattleManager.Instance != null &&
            BattleManager.Instance.CurrentPhase == BattleManager.Phase.Battle && !IsOnBench)
        {
            EnterIdleState();
        }
    }

    /// <summary>
    /// 이벤트 구독을 해제
    /// </summary>
    private void OnDisable()
    {
        BattleManager.OnBattleStart -= EnterIdleState;
    }

    // 실제 동작은 코루틴·EnterState() 종류에서 처리
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
    /// <summary>
    /// Idle로 복귀할 때 호출
    /// 가장 가까운 타겟을 찾아 이동 또는 공격 상태로 즉시 전환
    /// 타겟이 없으면 Idle을 유지하며 대기
    /// </summary>
    public void EnterIdleState()
    {
        // 벤치 유닛은 전투 AI를 실행하지 않는다
        if (IsOnBench) return;

        currentState  = UnitState.Idle;
        currentTarget = FindClosestTarget();

        if (currentTarget == null) return;

        int distance = HexCoordCal.GetDistance(currentCoord, currentTarget.currentCoord);

        if (distance <= currentAttRange)
            EnterAttackState(); // 이미 사거리 안 → 바로 공격
        else
            EnterMoveState();   // 사거리 밖 → 추격 이동
    }
    /// <summary>
    /// 공격 상태로 전환한다.
    /// </summary>
    public void EnterAttackState()
    {
        // StopMovement()가 현재 타일 위치로 스냅해준다.
        StopMovement();

        currentState = UnitState.Attacking;
        StartCoroutine(AttackCoroutine());
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
        moveCoroutine = StartCoroutine(MoveCoroutine());
    }

    /// <summary>
    /// 실행 중인 이동 중단하고, 오브젝트 위치를 타일로 변경.
    /// EnterAttackState(), Die(), 강제 재시작 시 반드시 호출한다.
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
        if (currentTile != null)
            transform.position = currentTile.transform.position;
    }

    /// <summary>
    /// 타겟을 향해 한 칸씩 이동
    /// 타일 도착마다 타겟 유효성 → 사거리 → 목적지 → 경로 평가 실행
    /// </summary>
    private IEnumerator MoveCoroutine()
    {
        while (true)
        {
            
            // 이동 중 타겟이 사망하거나 제거됐으면 새 타겟을 찾으러 Idle 복귀
            if (currentTarget == null || currentTarget.currentHp <= 0)
            {
                EnterIdleState();
                yield break; // 코루틴 종료
            }

            // 사거리 재확인
            // 한 칸 이동할 때마다 사거리 안에 들어왔는지 체크한다
            int distToTarget = HexCoordCal.GetDistance(currentCoord, currentTarget.currentCoord);
            if (distToTarget <= currentAttRange)
            {
                EnterAttackState();
                yield break; // 코루틴 종료 (EnterAttackState 내부에서 StopMovement 호출)
            }

            // 타겟 타일의 인접 타일 중 비어있고 나에게 가장 가까운 타일을 선택
            TileScript destination = GetBestAdjacentTile(currentTarget.currentTile);

            if (destination == null)
            {
                // 타겟 주변 모든 타일이 다른 유닛으로 막혀있다.
                // 1프레임 기다렸다가 다시 시도 — 다른 유닛이 이동하면 자리가 생긴다.
                yield return null;
                continue;
            }


            // 경로 계산 항상 최신 상태를 반영
            List<TileScript> path = Pathfinder.FindPath(currentTile, destination);

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
            currentTile.IsOccupied = false;
            nextTile.IsOccupied    = true;

            // 물리적 이동
            // LerpToTile이 완료될 때까지 이 줄에서 대기 (매 프레임 yield됨)
            yield return StartCoroutine(LerpToTile(nextTile));

            // 논리 좌표 확정 
            // Lerp 완료 후 내부 상태를 새 타일로 업데이트
            currentTile  = nextTile;
            currentCoord = nextTile.GridCoordinate;
            currentTarget = FindClosestTarget(); // 가까운 타겟 재설정
            yield return new WaitForSeconds(0.05f); // 

        }
    }

    /// <summary>
    /// targetTile의 Neighbors 중에서
    /// 비어있고 자신에게 가장 가까운 타일을 반환
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
        // duration = 타일 1칸 이동에 걸리는 시간
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

        // 이동 완료 후 위치 맞추기
        transform.position = endPos;
    }

    /// <summary>
    /// 적 팀 유닛 중 가장 가까운 생존 유닛을 반환한다.
    /// 거리가 같을 경우 공격 사거리가 짧은 적을 우선한다.
    /// 적이 없거나 모두 사망했으면 null을 반환한다.
    /// </summary>
    public UnitController FindClosestTarget()
    {
        UnitController closestTarget = null;
        int            minDistance   = int.MaxValue;

        // GetEnemiesOf: IReadOnlyList로 적 리스트 받기
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
                // 거리 동률: 사거리가 짧은 적을 우선
                if (target.currentAttRange < closestTarget.currentAttRange)
                    closestTarget = target;
            }
        }

        //if (closestTarget != null)
        //    Debug.Log($"[타겟] {currentTeam}[{unitData.unitName}] → {closestTarget.currentTeam}[{closestTarget.unitData.unitName}]");

        return closestTarget;
    }

    /// <summary>
    /// 공격 코루틴, 공격 속도에 따라 공격, searchInterval 값마다 타겟을 재탐색해 갱신한다.
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        float attackCooldown = 1f / currentAttSpd;
        float searchInterval = 0.2f;
        float searchTimer = 0f;
        
        while(true)
        {
            // null, 사망 체크
            if (currentTarget == null || currentTarget.currentHp <= 0)
            {
                EnterIdleState();
                yield break;
            }

            // 타겟이 사거리 밖으로 이동했으면 추격
            int distToTarget = HexCoordCal.GetDistance(currentCoord, currentTarget.currentCoord);
            if (distToTarget > currentAttRange)
            {
                EnterMoveState();
                yield break;
            }
            // 공격
            currentTarget.TakeDamage(currentAtt);
            GainMp(mpGainOnAttack);

            // MP가 충분하면 스킬 시전 후 복귀
            if (CanCastSkill())
            {
                yield return StartCoroutine(CastSkill());
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
                searchTimer += deltaTime;

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


    /// <summary>
    /// 방어력(def)을 반영해 실제 데미지를 계산하고 HP를 감소시킨다.
    /// HP가 0 이하가 되면 Die()를 호출한다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (currentState == UnitState.Dead) return;
        // def = % 데미지 감소율.
        float actualDamage = damage * (1f - currentDef / 100f);
        SetHp(currentHp - actualDamage);

        // 피격 시 MP 획득
        GainMp(mpGainOnHit);

        if (currentHp <= 0f)
            Die();
    }

    /// <summary>
    /// MP를 획득한다. maxMp를 초과하지 않도록 클램프한다.
    /// </summary>
    private void GainMp(float amount)
    {
        if (amount <= 0f) return;
        SetMp(currentMp + amount);
    }

    /// <summary>
    /// 스킬 시전 가능 여부를 반환한다.
    /// 스킬이 할당되어 있고, MP가 maxMp 이상이면 true.
    /// </summary>
    private bool CanCastSkill()
    {
        return skill != null && currentMp >= currentMaxMp;
    }

    /// <summary>
    /// 스킬을 시전하는 코루틴.
    /// 상태를 Casting으로 전환하고, 스킬의 Execute()를 실행한 뒤 MP를 초기화한다.
    /// </summary>
    private IEnumerator CastSkill()
    {
        StopMovement();
        currentState = UnitState.Casting;
        Debug.Log($"[스킬] {unitData.unitName} → {skill.skillName} 시전!");

        // 스킬 실행 (코루틴이므로 시전 시간, 이펙트 등을 스킬 내부에서 처리)
        yield return StartCoroutine(skill.Execute(this));

        // MP 초기화
        SetMp(0f);
        Debug.Log($"[스킬] {unitData.unitName} → {skill.skillName} 시전 완료, MP 초기화");
    }

    /// <summary>
    /// 유닛 사망 처리.
    /// </summary>
    public void Die()
    {
        // 모든 코루틴을 즉시 중단
        StopAllCoroutines();
        moveCoroutine = null;
        currentState = UnitState.Dead;

        // 점유 중인 타일을 해제해 다른 유닛이 해당 타일로 이동 가능하게 한다
        if (currentTile != null)
        {
            currentTile.IsOccupied = false;
            currentTile            = null;
        }
        if (currentBenchTile != null)
        {
            Debug.LogWarning("벤치 타일에서 Die() 호출! 확인 필요");
            currentBenchTile.IsOccupied = false;
            currentBenchTile            = null;
        }

        UnitManager.Instance.RemoveUnit(this, currentTeam);
        UnitManager.Instance.CheckBattleEnd();

        Debug.Log($"{gameObject} 사망");
        // 2초 후 오브젝트 파괴
        StartCoroutine(DestroyAfterDelay(2f));
    }

    /// <summary>
    /// 라운드 전환 시 스탯과 상태를 초기화한다.
    /// RoundManager.RestorePlayerPositions()에서 호출된다.
    /// </summary>
    public void ResetForNewRound()
    {
        StopAllCoroutines();
        moveCoroutine  = null;
        currentState   = UnitState.Idle;
        currentTarget  = null;

        // UnitData 기준으로 스탯 초기화
        currentAtt      = unitData.att;
        currentDef      = unitData.def;
        currentAttRange = unitData.attRange;
        currentAttSpd   = unitData.attSpd;
        currentMoveSpd  = unitData.moveSpd;
        currentMaxMp    = unitData.maxMp;
        mpGainOnAttack  = unitData.mpGainOnAttack;
        mpGainOnHit     = unitData.mpGainOnHit;

        SetHp(unitData.maxHp);
        SetMp(0f);
    }

    /// <summary>
    /// delay초 후 플레이어 유닛은 비활성화, 적 유닛은 파괴
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentTeam == Team.Player)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }
}
