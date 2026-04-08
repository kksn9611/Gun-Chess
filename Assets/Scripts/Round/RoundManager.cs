using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 라운드 진행을 관리한다.
/// 준비 → 전투 → 결과 → 준비 반복
/// 전투 시작 직전 플레이어 유닛 위치를 저장하고,
/// 전투 종료 후 저장된 위치로 복원하여 배치 상태를 유지한다.
/// </summary>
public class RoundManager : MonoBehaviour
{
    [Header("스테이지 데이터")]
    [SerializeField] private StageData[] stages;

    [Header("플레이어 초기 배치")]
    [SerializeField] private PlayerSpawnInfo[] playerSpawns;

    [Header("설정")]
    [SerializeField] private int currentRound = 0;
    private float resultPhaseDuration = 5f;
    private int previousRound;


    [Header("참조")]
    [SerializeField] private UnitSpawner unitSpawner;

    public int CurrentRound => currentRound;

    /// <summary>전투 시작 전 전장 유닛 위치 저장소</summary>
    private readonly Dictionary<UnitController, TileScript> savedFieldPositions = new();

    /// <summary>현재 라운드에 스폰된 적 유닛 참조 (준비 페이즈에서 미리 보여주기 위함)</summary>
    private readonly List<UnitController> previewEnemies = new();

    private void OnValidate() // 에디터 조작으로 스테이지 적 미리보기 가능
    {
        if (currentRound != previousRound && currentRound >= 1 && currentRound <= stages.Length)
        {
            SpawnEnemiesForPreview(stages[currentRound - 1]);
            previousRound = currentRound;
        }
    }
    private void OnEnable()
    {
        BattleManager.OnBattleEnd += OnBattleEnd;
    }

    private void OnDisable()
    {
        BattleManager.OnBattleEnd -= OnBattleEnd;
    }

    private IEnumerator Start()
    {
        // HexGridLayout / BenchLayout 이 타일을 생성할 때까지 대기
        yield return new WaitForSeconds(0.3f);

        // 플레이어 유닛 초기 배치
        SpawnPlayerUnitsForTest();

        // 1라운드 준비 시작 — 적 유닛을 미리보기
        currentRound = 1;
        previousRound = currentRound;
        SpawnEnemiesForPreview(stages[currentRound - 1]);
        Debug.Log($"[RoundManager] === 라운드 {currentRound} 준비 페이즈 ===");
    }

    /// <summary>
    /// Input System → Invoke Unity Events 에서 Space 키에 바인딩한다.
    /// Preparation 페이즈에서 전투를 시작한다.
    /// </summary>
    public void OnStartBattle(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        BeginBattle();
    }
    /// <summary>
    /// 현재 라운드의 전투를 시작
    /// </summary>
    public void BeginBattle()
    {
        if (BattleManager.Instance == null) return;
        if (BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation) return;

        if (currentRound < 1 || currentRound > stages.Length)
        {
            Debug.LogWarning($"[RoundManager] 유효하지 않은 라운드: {currentRound} (최대 {stages.Length})");
            return;
        }

        // 1) 플레이어 유닛 위치 저장 (전장 유닛만)
        SavePlayerUnitPositions();

        // 2) 미리 스폰된 적 유닛을 UnitManager에 등록
        RegisterPreviewEnemies();

        // 3) 전투 시작
        BattleManager.Instance.StartBattle();
        Debug.Log($"[RoundManager] === 라운드 {currentRound} 전투 시작 ===");
    }

    // ── 전투 종료 ────────────────────────────────────────────────

    private void OnBattleEnd(Team winner)
    {
        StartCoroutine(HandleBattleResult(winner));
    }

    /// <summary>
    /// 전투 결과를 처리한다.
    /// 결과 페이즈 대기 → 적 정리 → 타일 초기화 → 플레이어 복원 → 다음 라운드 준비.
    /// </summary>
    private IEnumerator HandleBattleResult(Team winner)
    {
        Debug.Log($"[RoundManager] 라운드 {currentRound} 결과: {winner} 승리");

        // 결과 페이즈 대기 (사망 연출 시간 확보)
        yield return new WaitForSeconds(resultPhaseDuration);

        // 적 유닛 정리
        ClearEnemyUnits();

        // 전장 타일 점유 상태 초기화 (벤치는 건드리지 않는다)
        TileManager.Instance.ClearAllOccupied();

        // 플레이어 유닛을 전투 시작 전 위치로 복원
        RestorePlayerPositions();

        // 다음 라운드로 진행
        currentRound++;

        if (currentRound > stages.Length)
        {
            Debug.Log("[RoundManager] === 모든 스테이지 클리어! ===");
            yield return new WaitForSeconds(resultPhaseDuration);
            BattleManager.Instance.ResetBattle();
            yield break;
        }

        // 다음 라운드 적 유닛을 미리 스폰하여 보여준다
        SpawnEnemiesForPreview(stages[currentRound - 1]);

        BattleManager.Instance.ResetBattle();
        Debug.Log($"[RoundManager] === 라운드 {currentRound} 준비 페이즈 ===");
    }


    /// <summary>
    /// Inspector에 설정된 플레이어 유닛을 벤치에 초기 배치한다.
    /// 빈 벤치 슬롯을 순서대로 할당하며, 슬롯이 부족하면 경고를 출력한다.
    /// </summary>
    private void SpawnPlayerUnitsForTest()
    {
        foreach (var spawn in playerSpawns)
        {
            BenchTileScript slot = BenchManager.Instance.GetEmptySlot();
            if (slot == null)
            {
                Debug.LogWarning($"[RoundManager] 벤치 슬롯 부족 — {spawn.unitData.unitName} 배치 실패");
                continue;
            }

            UnitController unit = unitSpawner.SpawnUnit(spawn.unitData, slot, Team.Player, register: false);
            if (unit != null)
                BenchManager.Instance.AddUnit(unit, slot);
        }
    }

    /// <summary>
    /// 준비 페이즈에서 적 유닛을 미리 스폰하여 보여주는 함수
    /// </summary>
    private void SpawnEnemiesForPreview(StageData stage)
    {
        // 기존 미리보기 적을 파괴한 뒤 리스트를 비운다
        ClearPreviewEnemies();

        foreach (var enemy in stage.enemies)
        {
            TileScript tile = TileManager.Instance.GetTile(enemy.spawnCoordinate);
            if (tile != null)
            {
                UnitController unit = unitSpawner.SpawnUnit(enemy.unitData, tile, Team.Enemy, register: false);
                if (unit != null)
                    previewEnemies.Add(unit);
            }
            else
            {
                Debug.LogWarning($"[RoundManager] 적 스폰 타일 ({enemy.spawnCoordinate}) 없음");
            }
        }
    }

    /// <summary>
    /// 미리 스폰된 적 유닛을 UnitManager에 등록하여 전투에 참여시킨다.
    /// 전투 시작 시 호출한다.
    /// </summary>
    private void RegisterPreviewEnemies()
    {
        foreach (var unit in previewEnemies)
        {
            if (unit != null)
                UnitManager.Instance.AddUnit(unit, Team.Enemy);
        }
        previewEnemies.Clear();
    }

    /// <summary>전투 시작 전 전장 위 플레이어 유닛의 현재 위치를 저장</summary>
    private void SavePlayerUnitPositions()
    {
        savedFieldPositions.Clear();

        foreach (var unit in UnitManager.Instance.playerUnits)
        {
            if (unit != null && !unit.IsOnBench && unit.CurrentTile is TileScript hexTile)
                savedFieldPositions[unit] = hexTile;
        }

        Debug.Log($"[RoundManager] 위치 저장 — 전장 {savedFieldPositions.Count}기");
    }

    /// <summary>
    /// 저장된 위치로 플레이어 유닛을 복원
    /// 사망 유닛은 재활성화, 스탯 초기화, 원래 타일에 재배치한다.
    /// </summary>
    private void RestorePlayerPositions()
    {
        // 플레이어 유닛 목록 비우기
        UnitManager.Instance.ClearTeam(Team.Player);

        // 전장 유닛 복원
        foreach (var pair in savedFieldPositions)
        {
            UnitController unit = pair.Key;
            TileScript tile     = pair.Value;
            if (unit == null) continue;

            // 사망으로 비활성화된 유닛 재활성화
            unit.gameObject.SetActive(true);
            // 스탯·상태 초기화
            unit.ResetForNewRound();
            // 저장된 타일에 배치
            unit.PlaceOnTile(tile);
            // UnitManager에 재등록
            UnitManager.Instance.AddUnit(unit, Team.Player);
        }

        Debug.Log($"[RoundManager] 플레이어 유닛 복원 완료");
    }


    /// <summary>
    /// 미리보기 유닛을 모두 파괴하고 리스트를 비운다.
    /// SpawnEnemiesForPreview() 재호출 또는 라운드 전환 시 사용
    /// </summary>
    private void ClearPreviewEnemies()
    {
        foreach (var enemy in previewEnemies)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                // 점유 중인 타일 해제
                if (enemy.CurrentTile != null)
                    enemy.CurrentTile.IsOccupied = false;
                Destroy(enemy.gameObject);
            }
        }
        previewEnemies.Clear();
    }

    /// <summary>
    /// 살아있는 적 유닛을 모두 파괴하고 목록을 정리한다.
    /// </summary>
    private void ClearEnemyUnits()
    {
        // enemyUnitList를 복사 후 순회 (Destroy 중 리스트 변경 방지)
        var remaining = new List<UnitController>(UnitManager.Instance.enemyUnits);
        foreach (var enemy in remaining)
        {
            if (enemy != null && enemy.gameObject != null)
            {
                enemy.StopAllCoroutines();
                Destroy(enemy.gameObject);
            }
        }

        // 적 유닛 목록 일괄 정리
        UnitManager.Instance.ClearTeam(Team.Enemy);
    }
}

[System.Serializable]
public class PlayerSpawnInfo
{
    public UnitData unitData;
    //public Vector2Int spawnCoordinate;
}
