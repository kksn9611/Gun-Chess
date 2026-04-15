using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛의 중앙 허브 컴포넌트.
/// 스탯, HP/MP, 배치, 시너지, 데미지 처리를 담당한다.
/// 전투 AI(FSM, 이동, 공격, 스킬)는 UnitAI 컴포넌트가 처리한다.
/// </summary>
public class UnitController : MonoBehaviour
{
    // ── 직렬화 필드 ──────────────────────────────────────────────

    [SerializeField] private UnitData unitData;
    [SerializeField] private float currentHp;
    [SerializeField] private float currentMp;
    [SerializeField] private float currentAtt;
    [SerializeField] private float currentDef;
    [SerializeField] private float currentAttRange;
    [SerializeField] private float currentAttSpd;
    [SerializeField] private float currentMoveSpd;
    [SerializeField] private float currentMaxMp;
    [SerializeField] private float mpGainOnAttack;
    [SerializeField] private float mpGainOnHit;
    [SerializeField] private BaseSkill skill;          // 이 유닛이 사용하는 스킬 (null이면 스킬 없음)
    [SerializeField] private float currentSkillDmgMul = 1f; // 스킬 데미지 배율 (기본 1.0 = 100%)
    [SerializeField] private TileScript currentTile;    // 현재 이 유닛이 점유 중인 헥스 타일
    [SerializeField] private BenchTileScript currentBenchTile; // 대기석 타일. null = 전장에 있음
    [SerializeField] private Vector2Int currentCoord;   // 현재 타일 좌표
    [SerializeField] private Team  currentTeam;

    // ── 시너지 버프 관리 ──
    /// <summary>Inspector에서 할당하는 공유 SynergyState 에셋</summary>
    [Header("시너지")]
    [SerializeField] private SynergyState synergyState;

    /// <summary>현재 적용 중인 시너지</summary>
    private readonly Dictionary<SynergyData, int> appliedSynergyTiers = new Dictionary<SynergyData, int>();

    /// <summary>같은 GameObject의 UnitAI 컴포넌트 캐시</summary>
    private UnitAI unitAI;

    // ── 이벤트 ───────────────────────────────────────────────────

    public Transform uiAnchor;
    public static event Action<UnitController> OnUnitSpawned;

    /// <summary>HP 변경 시 실행. (현재HP, 최대HP)</summary>
    public event Action<float, float> OnHpChanged;
    /// <summary>MP 변경 시 실행. (현재MP, 최대MP)</summary>
    public event Action<float, float> OnMpChanged;
    /// <summary>벤치 ↔ 전장 전환 시 실행. (true = 벤치에 있음)</summary>
    public event Action<bool> OnBenchState;

    /// <summary>공격이 적중했을 때 (공격자, 피격자, 피해량)</summary>
    public event Action<UnitController, UnitController, float> OnAttackHit;
    /// <summary>피해를 입기 직전 — 데미지 경감, 쉴드 등에 사용</summary>
    public event Action<float> OnBeforeTakeDamage;
    /// <summary>스킬을 사용하기 직전</summary>
    public event Action OnBeforeSkillCast;

    // ── 읽기 전용 프로퍼티 ────────────────────────────────────────

    public UnitData UnitData       => unitData;
    public BaseTile CurrentTile    => currentBenchTile != null ? (BaseTile)currentBenchTile : (BaseTile)currentTile;
    public Team     CurrentTeam    => currentTeam;
    public bool     IsOnBench      => currentBenchTile != null;
    public float    SkillDamageMultiplier => currentSkillDmgMul;

    /// <summary>현재 체력 (UnitAI에서 사망 판정에 사용)</summary>
    public float CurrentHp       => currentHp;
    /// <summary>현재 공격 사거리 (UnitAI에서 사거리 판정에 사용)</summary>
    public float CurrentAttRange => currentAttRange;
    /// <summary>현재 공격 속도 (UnitAI에서 공격 쿨타임 계산에 사용)</summary>
    public float CurrentAttSpd   => currentAttSpd;
    /// <summary>현재 이동 속도 (UnitAI에서 Lerp 소요 시간 계산에 사용)</summary>
    public float CurrentMoveSpd  => currentMoveSpd;
    /// <summary>현재 좌표 (UnitAI에서 거리 계산에 사용)</summary>
    public Vector2Int CurrentCoord => currentCoord;
    /// <summary>현재 점유 중인 헥스 타일 (UnitAI에서 이동/경로 계산에 사용)</summary>
    public TileScript CurrentHexTile => currentTile;

    // ── HP / MP ──────────────────────────────────────────────────

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

    /// <summary>MP를 획득한다. maxMp를 초과하지 않도록 클램프한다.</summary>
    private void GainMp(float amount)
    {
        if (amount <= 0f) return;
        SetMp(currentMp + amount);
    }

    // ── 초기화 ───────────────────────────────────────────────────

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
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
    }

    // ── 시너지 이벤트 구독 ────────────────────────────────────────

    private void OnEnable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged += OnSynergyChanged;
    }

    private void OnDisable()
    {
        if (synergyState != null)
            synergyState.OnSynergyChanged -= OnSynergyChanged;
    }

    // ── 배치 ─────────────────────────────────────────────────────

    /// <summary>
    /// 유닛을 헥스 타일에 배치한다. (준비 페이즈 배치용)
    /// clearCurrent=false : 스왑 시 원래 위치의 IsOccupied를 해제하지 않음
    /// </summary>
    public void PlaceOnTile(TileScript newTile, bool clearCurrent = true)
    {
        if (unitAI != null)
        {
            unitAI.ResetState();
        }
        else
        {
            StopAllCoroutines();
        }

        if (clearCurrent)
        {
            if (currentTile != null)      currentTile.IsOccupied = false;
            if (currentBenchTile != null) currentBenchTile.IsOccupied = false;
        }
        // 헥스에 배치 = 벤치 참조 해제
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
        if (unitAI != null)
        {
            unitAI.ResetState();
        }
        else
        {
            StopAllCoroutines();
        }

        if (clearCurrent)
        {
            if (currentTile != null)      currentTile.IsOccupied = false;
            if (currentBenchTile != null) currentBenchTile.IsOccupied = false;
        }
        // 벤치에 배치 = 헥스 참조 해제
        currentTile      = null;
        currentBenchTile = slot;
        currentCoord     = slot.GetCoordinate();
        slot.IsOccupied  = true;
        OnBenchState?.Invoke(true);
        StartCoroutine(MoveToTileSmoothly(slot.transform.position, 0.2f));
    }

    /// <summary>배치 시 부드러운 이동 연출용 코루틴</summary>
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
        transform.position = targetPosition;
    }

    // ── 타일 상태 갱신 (UnitAI에서 이동 완료 시 호출) ─────────────

    /// <summary>
    /// UnitAI의 MoveCoroutine에서 Lerp 완료 후 호출하여
    /// 논리 타일과 좌표를 갱신한다.
    /// </summary>
    public void SetCurrentTile(TileScript newTile)
    {
        currentTile  = newTile;
        currentCoord = newTile.GridCoordinate;
    }

    // ── 전투 액션 (UnitAI에서 호출) ──────────────────────────────

    /// <summary>
    /// 타겟에게 공격을 수행한다. 데미지 적용, MP 획득, 이벤트 발행을 처리한다.
    /// UnitAI.AttackCoroutine()에서 호출된다.
    /// </summary>
    public void PerformAttack(UnitController target)
    {
        target.TakeDamage(currentAtt);
        OnAttackHit?.Invoke(this, target, currentAtt);
        GainMp(mpGainOnAttack);
    }

    /// <summary>
    /// 스킬 시전 가능 여부를 반환한다.
    /// 스킬이 할당되어 있고, MP가 maxMp 이상이면 true.
    /// </summary>
    public bool CanCastSkill()
    {
        return skill != null && currentMp >= currentMaxMp;
    }

    /// <summary>
    /// 스킬을 시전하는 코루틴.
    /// UnitAI.AttackCoroutine()에서 yield return으로 호출된다.
    /// </summary>
    public IEnumerator CastSkillCoroutine()
    {
        // 스킬 시전 직전 이벤트 발행 (스킬 증폭, 마나 페이백 등)
        OnBeforeSkillCast?.Invoke();
        Debug.Log($"[스킬] {unitData.unitName} → {skill.skillName} 시전!");

        // 스킬 실행 (코루틴이므로 시전 시간, 이펙트 등을 스킬 내부에서 처리)
        yield return StartCoroutine(skill.Execute(this));

        // MP 초기화
        SetMp(0f);
        Debug.Log($"[스킬] {unitData.unitName} → {skill.skillName} 시전 완료, MP 초기화");
    }

    // ── 데미지 / 사망 ────────────────────────────────────────────

    /// <summary>
    /// 방어력(def)을 반영해 실제 데미지를 계산하고 HP를 감소시킨다.
    /// HP가 0 이하가 되면 Die()를 호출한다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (unitAI != null && unitAI.CurrentState == UnitState.Dead) return;
        // 피격 직전 이벤트 발행 (쉴드, 데미지 경감 등 외부 로직이 개입 가능)
        OnBeforeTakeDamage?.Invoke(damage);
        // def = % 데미지 감소율
        float actualDamage = damage * (1f - currentDef / 100f);
        SetHp(currentHp - actualDamage);

        // 피격 시 MP 획득
        GainMp(mpGainOnHit);

        if (currentHp <= 0f)
            Die();
    }

    /// <summary>
    /// 유닛 사망 처리.
    /// </summary>
    public void Die()
    {
        // UnitAI의 모든 코루틴을 중단하고 Dead 상태로 전환
        if (unitAI != null)
            unitAI.EnterDeadState();

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
        StartCoroutine(DestroyAfterDelay(2f));
    }

    /// <summary>
    /// 라운드 전환 시 스탯과 상태를 초기화한다.
    /// RoundManager.RestorePlayerPositions()에서 호출된다.
    /// </summary>
    public void ResetForNewRound()
    {
        // UnitAI 상태 초기화
        if (unitAI != null)
            unitAI.ResetState();
        else
            StopAllCoroutines();

        // 시너지 버프를 먼저 해제한 뒤 기본 스탯으로 초기화
        RemoveAllSynergyBuffs();

        // UnitData 기준으로 스탯 초기화
        currentAtt      = unitData.att;
        currentDef      = unitData.def;
        currentAttRange = unitData.attRange;
        currentAttSpd   = unitData.attSpd;
        currentMoveSpd  = unitData.moveSpd;
        currentMaxMp    = unitData.maxMp;
        mpGainOnAttack  = unitData.mpGainOnAttack;
        mpGainOnHit     = unitData.mpGainOnHit;
        currentSkillDmgMul = 1f;

        SetHp(unitData.maxHp);
        SetMp(0f);
    }

    // ── 시너지 버프 적용/해제 ─────────────────────────────────────

    /// <summary>
    /// SynergyState.OnSynergyChanged 이벤트 핸들러.
    /// 자신이 가진 SynergyData[]를 현재 SynergyState와 대조하여
    /// 버프를 적용하거나 해제한다.
    /// </summary>
    private void OnSynergyChanged()
    {
        if (unitData == null || unitData.synergies == null) return;
        if (synergyState == null) return;

        // 플레이어 유닛만 시너지 버프를 적용한다
        if (currentTeam != Team.Player) return;

        foreach (var synergy in unitData.synergies)
        {
            if (synergy == null) continue;

            int newTierIndex = synergyState.GetActiveTierIndex(synergy);
            appliedSynergyTiers.TryGetValue(synergy, out int oldTierIndex);

            // 이전에 적용된 적 없으면 -1로 초기화
            if (!appliedSynergyTiers.ContainsKey(synergy))
                oldTierIndex = -1;

            // 구간이 변경되지 않았으면 스킵
            if (newTierIndex == oldTierIndex) continue;

            // 이전 구간의 효과 제거
            if (oldTierIndex >= 0 && oldTierIndex < synergy.tiers.Length)
            {
                var oldBehaviors = synergy.tiers[oldTierIndex].behaviors;
                if (oldBehaviors != null)
                {
                    foreach (var behavior in oldBehaviors)
                        behavior?.Remove(this);
                }
            }

            // 새 구간의 효과 적용
            if (newTierIndex >= 0 && newTierIndex < synergy.tiers.Length)
            {
                var newBehaviors = synergy.tiers[newTierIndex].behaviors;
                if (newBehaviors != null)
                {
                    foreach (var behavior in newBehaviors)
                        behavior?.Apply(this);
                }
            }

            // 적용 상태 갱신
            appliedSynergyTiers[synergy] = newTierIndex;
        }
    }

    /// <summary>
    /// 모든 시너지 버프를 제거하고 기본 스탯으로 원복한다.
    /// 라운드 리셋 시 호출한다.
    /// </summary>
    private void RemoveAllSynergyBuffs()
    {
        if (unitData == null || unitData.synergies == null) return;

        foreach (var pair in appliedSynergyTiers)
        {
            SynergyData synergy = pair.Key;
            int tierIndex = pair.Value;

            if (tierIndex >= 0 && synergy != null && tierIndex < synergy.tiers.Length)
            {
                var behaviors = synergy.tiers[tierIndex].behaviors;
                if (behaviors != null)
                {
                    foreach (var behavior in behaviors)
                        behavior?.Remove(this);
                }
            }
        }
        appliedSynergyTiers.Clear();
    }

    // ── 시너지 스탯 보정 ───────────────────────────────────────

    /// <summary>
    /// 특정 스탯을 % 단위로 보정한다.
    /// percentDelta가 양수이면 증가, 음수이면 감소.
    /// 기본값(UnitData) 기준의 비율을 현재 스탯에 가감한다.
    /// 예: Att 기본 100, percentDelta=20 → currentAtt += 20
    /// </summary>
    public void ApplyStatModifier(StatType stat, float percentDelta)
    {
        switch (stat)
        {
            case StatType.Att:
                currentAtt += unitData.att * (percentDelta / 100f);
                break;
            case StatType.Def:
                currentDef += unitData.def * (percentDelta / 100f);
                break;
            case StatType.AttSpd:
                currentAttSpd += unitData.attSpd * (percentDelta / 100f);
                break;
            case StatType.MaxHp:
                float hpDelta = unitData.maxHp * (percentDelta / 100f);
                SetHp(currentHp + hpDelta);
                break;
            case StatType.MoveSpd:
                currentMoveSpd += unitData.moveSpd * (percentDelta / 100f);
                break;
            case StatType.MpGain:
                mpGainOnAttack += unitData.mpGainOnAttack * (percentDelta / 100f);
                mpGainOnHit    += unitData.mpGainOnHit    * (percentDelta / 100f);
                break;
            case StatType.SkillDmg:
                currentSkillDmgMul += percentDelta / 100f;
                break;
        }
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
