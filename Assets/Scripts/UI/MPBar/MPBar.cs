using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPBar와 동일한 구조의 MP 바.
/// UnitController.OnMpChanged 이벤트를 구독하여 Fill을 갱신한다.
/// </summary>
public class MPBar : MonoBehaviour
{
    [SerializeField] private Image fill;

    [SerializeField] private Vector3 screenOffset = new Vector3(0f, -9f, 0f); // HP바 아래 오프셋 (Screen 픽셀)

    private UnitController targetUnit;
    private Camera mainCam;
    private Transform targetAnchor;

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.OnMpChanged += UpdateMp;
        targetUnit.OnHpChanged += OnHpChanged;
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

    /// <summary>MP 변경 콜백. Fill의 fillAmount를 갱신한다.</summary>
    public void UpdateMp(float currentMp, float maxMp)
    {
        if (fill == null) return;
        fill.fillAmount = maxMp > 0f ? currentMp / maxMp : 0f;
    }

    /// <summary>HP 변경 시 사망 여부를 확인하여 바를 숨긴다.</summary>
    private void OnHpChanged(float currentHp, float maxHp)
    {
        if (currentHp <= 0f)
            gameObject.SetActive(false);
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

        transform.position = mainCam.WorldToScreenPoint(targetAnchor.position) + screenOffset;
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.OnMpChanged -= UpdateMp;
            targetUnit.OnHpChanged -= OnHpChanged;
            targetUnit.OnBenchState -= BarStateChanged;
        }
    }
}
