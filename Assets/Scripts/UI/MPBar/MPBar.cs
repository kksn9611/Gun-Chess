using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HPBar와 동일한 구조의 MP 바.
/// UnitController.OnMpChanged 이벤트를 구독하여 Fill을 갱신한다.
/// </summary>
public class MPBar : MonoBehaviour
{
    [SerializeField] private Image fill;

    [SerializeField] private Vector3 screenOffset = new Vector3(0f, -10f, 0f); // HP바 아래 오프셋 (Screen 픽셀)

    private UnitController targetUnit;
    private Camera mainCam;
    private Transform targetAnchor;

    public void Initialize(UnitController target, Transform uiAnchor)
    {
        targetUnit = target;
        targetUnit.OnMpChanged += UpdateMp;
        targetUnit.OnBenchState += HandleBenchStateChanged;
        mainCam = Camera.main;
        targetAnchor = uiAnchor;

        // 초기 상태 반영 (벤치에서 스폰된 유닛이면 즉시 숨김)
        gameObject.SetActive(!targetUnit.IsOnBench);
    }

    /// <summary>벤치 ↔ 전장 전환 시 바를 숨기거나 보여준다.</summary>
    private void HandleBenchStateChanged(bool isOnBench)
    {
        gameObject.SetActive(!isOnBench);
    }

    /// <summary>MP 변경 콜백. Fill의 fillAmount를 갱신한다.</summary>
    public void UpdateMp(float currentMp, float maxMp)
    {
        if (fill == null) return;
        fill.fillAmount = maxMp > 0f ? currentMp / maxMp : 0f;
    }

    private void LateUpdate()
    {
        if (targetUnit == null || !targetUnit.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = mainCam.WorldToScreenPoint(targetAnchor.position) + screenOffset;
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.OnMpChanged -= UpdateMp;
            targetUnit.OnBenchState -= HandleBenchStateChanged;
        }
    }
}
