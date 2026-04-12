using UnityEngine;
using UnityEngine.UI;
public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private RawImage tickImage;

    private UnitController targetUnit;
    private Camera mainCam;
    private Transform targetAnchor;

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.OnHpChanged += UpdateHp;
        targetUnit.OnBenchState += BarStateChanged;
        mainCam = Camera.main;
        targetAnchor = uiAnchor;

        // 초기 상태 반영 (벤치에서 스폰된 유닛이면 즉시 숨김)
        gameObject.SetActive(!targetUnit.IsOnBench);
    }

    /// <summary>벤치 ↔ 전장 전환 시 바를 숨기거나 보여준다.</summary>
    private void BarStateChanged(bool isOnBench)
    {
        gameObject.SetActive(!isOnBench);
    }

    public void UpdateHp(float currentHp, float maxHp)
    {
        if (fill == null) return;
        
        
        fill.fillAmount = currentHp / maxHp;
        if (tickImage != null)
        {
            float hpPerTick = 100f; // 체력 100당 눈금 1칸

            // 최대 체력이 500이라면 tickCount는 5가 됩니다.
            float tickCount = maxHp / hpPerTick;

            tickImage.uvRect = new Rect(0, 0, tickCount-1f, 1);
        }
    }

    private void LateUpdate()
    {
        if (targetUnit == null || !targetUnit.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = mainCam.WorldToScreenPoint(targetAnchor.position);
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.OnHpChanged -= UpdateHp;
            targetUnit.OnBenchState -= BarStateChanged;
        }
    }
}

