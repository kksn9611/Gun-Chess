using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 준비 페이즈에서 마우스로 플레이어 유닛을 집어 PlayerZone 타일에 배치하는 컴포넌트.
/// BattleManager.Phase.Preparation 일 때만 동작한다.
/// </summary>
public class UnitPlacer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("y < playerZoneMaxRow 인 타일을 PlayerZone으로 간주한다.")]
    [SerializeField] private int playerZoneMaxRow = 4;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float dragHeightOffset;
    [SerializeField] private float dragFollowSpeed;

    [Header("Visual")]
    [SerializeField] private Material highlightMaterial;

    
    private UnitController heldUnit;        // 현재 들고 있는 유닛
    private TileScript     originalTile;    // 집어 올리기 전 타일

    private TileScript hoveredTile;                 // 마우스가 올려져 있는 타일
    private Material   hoveredOriginalMaterial;     // 하이라이트 전 원본 머터리얼

    // 지면 Plane.Raycast
    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);


    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        // 준비 페이즈 이외에는 동작하지 않는다
        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation)
        {
            // 들고 있던 유닛이 있으면 원래 자리에 복귀
            if (heldUnit != null) CancelPlacement();
            return;
        }

        // 호버는 매 프레임 마우스 위치를 추적해야 하므로 Update에서 처리
        HandleHover();
        if (heldUnit != null)
        {
            UpdateDragVisuals();
        }
    }
    /// <summary>
    /// 들고 있는 유닛 마우스 드래그
    /// </summary>
    private void UpdateDragVisuals()
    {
        // 화면의 마우스 위치에서 3D 공간으로 향하는 광선 생성
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.value);

        // 가상의 바닥 평면(y=0)과 광선이 만나는 지점을 계산
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 targetPosition = new Vector3(hitPoint.x, hitPoint.y + dragHeightOffset, hitPoint.z);

            heldUnit.transform.position = Vector3.Lerp(heldUnit.transform.position, targetPosition, Time.deltaTime * dragFollowSpeed);
        }
    }

    /// <summary>
    /// 클릭
    /// </summary>
    public void OnPlaceClick(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation) return;

        HandleLeftClick();
    }

    /// <summary>
    /// PlayerInput (Invoke Unity Events) — 우클릭 또는 ESC 취소 액션에 연결.
    /// Inspector: PlayerInput → Events → [액션명] → OnPlaceCancel
    /// </summary>
    public void OnPlaceCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentPhase != BattleManager.Phase.Preparation) return;

        CancelPlacement();
    }

    // 마우스오버

    private void HandleHover()
    {
        TileScript tile = RaycastTarget<TileScript>();

        if (tile == hoveredTile) return; // 변화 없음

        // 이전 타일 원복
        ClearHover();

        // 새 타일 하이라이트 (PlayerZone & 빈 타일 또는 유닛 있는 타일)
        if (tile != null && IsPlayerZone(tile) && highlightMaterial != null)
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

    // 클릭 처리

    private void HandleLeftClick()
    {
        if (heldUnit == null)
        {
            // 유닛 집어 올리기
            TryPickUp();
        }
        else
        {
            // 유닛 내려놓기
            TryDrop();
        }
    }

    private void TryPickUp()
    {
        TileScript tile = RaycastTarget<TileScript>();
        if (tile == null) return;
        if (!IsPlayerZone(tile)) return;
        if (!tile.IsOccupied) return;

        // 타일에 있는 플레이어 유닛 찾기
        UnitController unit = GetUnitOnTile(tile);
        if (unit == null || unit.CurrentTeam != Team.Player) return;

        heldUnit     = unit;
        originalTile = tile;
    }

    private void TryDrop()
    {
        TileScript targetTile = RaycastTarget<TileScript>();

        if (targetTile == null || !IsPlayerZone(targetTile))
        {
            // 유효하지 않은 위치 → 취소
            CancelPlacement();
            return;
        }

        if (targetTile == originalTile)
        {
            // 제자리 클릭 → 취소
            CancelPlacement();
            return;
        }

        if (targetTile.IsOccupied)
        {
            // 다른 유닛이 있으면 스왑
            UnitController other = GetUnitOnTile(targetTile);
            if (other == null)
            {
                // 유닛 오브젝트를 못 찾은 경우 취소
                CancelPlacement();
                return;
            }

            // 스왑: 두 유닛의 타일 점유를 교환
            // clearCurrentTile=false 로 호출해 IsOccupied 상태를 수동 관리한다
            other.PlaceOnTile(originalTile, clearCurrentTile: false);
            heldUnit.PlaceOnTile(targetTile, clearCurrentTile: false);

            // 원래 타일(originalTile)의 IsOccupied 는 other 가 새로 점유
            // targetTile 의 IsOccupied 는 heldUnit 이 새로 점유
            // — PlaceOnTile 내부에서 newTile.IsOccupied = true 처리됨
        }
        else
        {
            // 빈 타일 → 이동
            heldUnit.PlaceOnTile(targetTile);
        }

        heldUnit     = null;
        originalTile = null;
    }

    private void CancelPlacement()
    {
        if (heldUnit != null && originalTile != null)
            heldUnit.PlaceOnTile(originalTile);

        heldUnit     = null;
        originalTile = null;
    }


    private bool IsPlayerZone(TileScript tile)
        => tile.GridCoordinate.y < playerZoneMaxRow;

    /// <summary>
    /// 마우스 위치에서 Physics.Raycast 를 쏴 컴포넌트를 반환한다.
    /// </summary>
    private T RaycastTarget<T>() where T : Component
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.value);
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.collider.GetComponent<T>();
        return null;
    }

    /// <summary>
    /// UnitManager 플레이어 목록에서 해당 타일에 있는 유닛을 반환한다.
    /// </summary>
    private UnitController GetUnitOnTile(BaseTile tile)
    {
        foreach (UnitController unit in UnitManager.Instance.playerUnits)
        {
            if (unit != null && unit.CurrentTile == tile)
                return unit;
        }
        return null;
    }
}
