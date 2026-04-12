using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스로 플레이어 유닛을 집어 타일에 배치
/// </summary>
public class UnitPlacer : MonoBehaviour
{
    [Header("세팅")]
    [SerializeField] private int    playerZoneMaxRow = 4;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float  dragHeightOffset;
    [SerializeField] private float  dragFollowSpeed;

    [Header("비주얼")]
    [SerializeField] private Material highlightMaterial;

    private UnitController heldUnit;      // 현재 들고 있는 유닛
    private BaseTile       originalTile;  // 집어 올리기 전 원래 타일(헥스 또는 벤치)

    private BaseTile hoveredTile;                // 마우스가 올려져 있는 타일
    private Material hoveredOriginalMaterial;   // 하이라이트 전 원본 머터리얼

    // 지면 Plane.Raycast (유닛 드래그용)
    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);


    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsActivePhase())
        {
            if (heldUnit != null) CancelPlacement();
            return;
        }

        HandleHover();

        if (heldUnit != null)
            UpdateDragVisuals();
    }

    /// <summary>
    /// BattleManager 인스턴스가 존재하면 모든 페이즈에서 동작 허용.
    /// </summary>
    private bool IsActivePhase()
        => BattleManager.Instance != null;

    /// <summary>
    /// Preparation 이외 페이즈(Battle / Result)에서는 벤치 전용 모드로 동작한다.
    /// </summary>
    private bool IsBenchOnlyPhase()
        => BattleManager.Instance != null
        && BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation;

    // ── 드래그 비주얼 ──────────────────────────────────────────

    private void UpdateDragVisuals()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.value);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hit    = ray.GetPoint(enter);
            Vector3 target = new Vector3(hit.x, hit.y + dragHeightOffset, hit.z);
            heldUnit.transform.position =
                Vector3.Lerp(heldUnit.transform.position, target, Time.deltaTime * dragFollowSpeed);
        }
    }

    public void OnPlaceClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnLeftClick();
        }
    }

    public void OnPlaceCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CancelPlacement();
        }
    }

    private void HandleHover()
    {
        // BaseTile 기반 레이캐스트 — 헥스·벤치 타일 모두 감지
        BaseTile tile = RaycastTarget<BaseTile>();

        if (tile == hoveredTile) return;

        ClearHover();

        if (tile != null && IsValidDropTarget(tile) && highlightMaterial != null)
        {
            MeshRenderer mr = tile.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                hoveredOriginalMaterial = mr.sharedMaterial;
                mr.sharedMaterial       = highlightMaterial;
                hoveredTile             = tile;
            }
        }
    }

    private void ClearHover()
    {
        if (hoveredTile == null) return;
        MeshRenderer mr = hoveredTile.GetComponent<MeshRenderer>();
        if (mr != null && hoveredOriginalMaterial != null)
            mr.sharedMaterial = hoveredOriginalMaterial;
        hoveredTile             = null;
        hoveredOriginalMaterial = null;
    }

    private void OnLeftClick()
    {
        if (heldUnit == null) TryPickUp();
        else                  TryDrop();
    }

    private void TryPickUp()
    {
        BaseTile tile = RaycastTarget<BaseTile>();
        if (tile == null || !tile.IsOccupied) return;

        // 배틀 페이즈: 벤치 슬롯에서만 집기 가능 (전장 유닛 조작 불가)
        if (IsBenchOnlyPhase())
        {
            if (!(tile is BenchTileScript)) return;
        }
        else
        {
            if (tile is TileScript hexTile && !IsPlayerZone(hexTile)) return;
        }

        UnitController unit = GetUnitOnTile(tile);
        if (unit == null || unit.CurrentTeam != Team.Player) return;

        heldUnit     = unit;
        originalTile = tile;
    }

    /// <summary>
    /// 유닛을 내려놓는다. 집은 위치(헥스/벤치) × 놓는 위치(헥스/벤치) × 점유 여부
    /// </summary>
    private void TryDrop()
    {
        BaseTile targetTile = RaycastTarget<BaseTile>();

        if (targetTile == null || !IsValidDropTarget(targetTile))
        {
            CancelPlacement();
            return;
        }
        if (targetTile == originalTile)
        {
            CancelPlacement();
            return;
        }

        bool heldFromHex  = originalTile is TileScript;
        bool targetIsHex  = targetTile   is TileScript;
        UnitController other = targetTile.IsOccupied ? GetUnitOnTile(targetTile) : null;

        if (heldFromHex && targetIsHex)
        {
            //헥스 → 헥스
            if (other != null)
            {
                // 스왑: 두 유닛 모두 UnitManager에 유지
                other.PlaceOnTile((TileScript)originalTile, clearCurrent: false);
                heldUnit.PlaceOnTile((TileScript)targetTile,   clearCurrent: false);
            }
            else
            {
                heldUnit.PlaceOnTile((TileScript)targetTile);
            }
        }
        else if (heldFromHex) // 헥스 → 벤치
        {
            var benchTarget = (BenchTileScript)targetTile;
            if (other != null)
            {
                // 스왑: other(벤치) → 헥스 / held(헥스) → 벤치
                BenchManager.Instance.RemoveUnit(other);
                UnitManager.Instance.AddUnit(other, other.CurrentTeam);
                other.PlaceOnTile((TileScript)originalTile, clearCurrent: false);

                UnitManager.Instance.RemoveUnit(heldUnit, heldUnit.CurrentTeam);
                BenchManager.Instance.AddUnit(heldUnit, benchTarget);
                heldUnit.PlaceOnBench(benchTarget, clearCurrent: false);
            }
            else
            {
                // 이동: held → 빈 벤치
                UnitManager.Instance.RemoveUnit(heldUnit, heldUnit.CurrentTeam);
                BenchManager.Instance.AddUnit(heldUnit, benchTarget);
                heldUnit.PlaceOnBench(benchTarget);
            }
        }
        else if (targetIsHex) // 벤치 → 헥스
        {
            var hexTarget    = (TileScript)targetTile;
            var benchOrigin  = (BenchTileScript)originalTile;
            if (other != null)
            {
                // 스왑: other(헥스) → 벤치 / held(벤치) → 헥스
                UnitManager.Instance.RemoveUnit(other, other.CurrentTeam);
                BenchManager.Instance.AddUnit(other, benchOrigin);
                other.PlaceOnBench(benchOrigin, clearCurrent: false);

                BenchManager.Instance.RemoveUnit(heldUnit);
                UnitManager.Instance.AddUnit(heldUnit, heldUnit.CurrentTeam);
                heldUnit.PlaceOnTile(hexTarget, clearCurrent: false);
            }
            else
            {
                // 이동: held → 빈 헥스
                BenchManager.Instance.RemoveUnit(heldUnit);
                UnitManager.Instance.AddUnit(heldUnit, heldUnit.CurrentTeam);
                heldUnit.PlaceOnTile(hexTarget);
            }
        }
        else // 벤치 → 벤치
        {
            var benchTarget = (BenchTileScript)targetTile;
            if (other != null)
            {
                // 스왑: 두 유닛 모두 BenchManager에 유지
                other.PlaceOnBench((BenchTileScript)originalTile, clearCurrent: false);
                heldUnit.PlaceOnBench(benchTarget, clearCurrent: false);
            }
            else
            {
                heldUnit.PlaceOnBench(benchTarget);
            }
        }

        heldUnit     = null;
        originalTile = null;
    }

    private void CancelPlacement()
    {
        if (heldUnit != null && originalTile != null)
        {
            if (originalTile is TileScript hexTile)
                heldUnit.PlaceOnTile(hexTile);
            else if (originalTile is BenchTileScript benchSlot)
                heldUnit.PlaceOnBench(benchSlot);
        }
        heldUnit     = null;
        originalTile = null;
    }
/// <summary>
    /// 유효한 드롭 대상인지 확인한다.
    /// - 배틀 페이즈: 벤치 슬롯만 유효 (전장 배치 불가)
    /// - 준비 페이즈: 헥스(PlayerZone) + 벤치 모두 유효
    /// </summary>
    private bool IsValidDropTarget(BaseTile tile)
    {
        if (IsBenchOnlyPhase())
            return tile is BenchTileScript;

        if (tile is TileScript hexTile) return IsPlayerZone(hexTile);
        if (tile is BenchTileScript)    return true;
        return false;
    }

    private bool IsPlayerZone(TileScript tile)
        => tile.GridCoordinate.y < playerZoneMaxRow;

    /// <summary>
    /// 마우스 위치에서 Physics.Raycast 를 쏴 T 컴포넌트를 반환한다.
    /// </summary>
    private T RaycastTarget<T>() where T : Component
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.value);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider.GetComponent<T>();
        return null;
    }

    /// <summary>
    /// 해당 타일 위에 있는 플레이어 유닛을 반환한다.
    /// 헥스 전장(UnitManager)과 벤치(BenchManager) 모두 탐색한다.
    /// </summary>
    private UnitController GetUnitOnTile(BaseTile tile)
    {
        foreach (UnitController unit in UnitManager.Instance.playerUnits)
            if (unit != null && unit.CurrentTile == tile) return unit;
        foreach (UnitController unit in BenchManager.Instance.benchUnits)
            if (unit != null && unit.CurrentTile == tile) return unit;
        return null;
    }
}
