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

        // HP가 0이 되면 즉시 숨김
        if (currentHp <= 0f)
            gameObject.SetActive(false);

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
        // 유닛이 완전히 파괴된 경우에만 바를 파괴
        if (targetUnit == null)
        {
            Destroy(gameObject);
            return;
        }

        // 유닛이 비활성화(사망 등)되면 바를 숨기고, 재활성화되면 다시 표시
        if (!targetUnit.gameObject.activeInHierarchy)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }
        else if (!gameObject.activeSelf && !targetUnit.IsOnBench)
        {
            gameObject.SetActive(true);
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

