using System.Collections.Generic;
using UnityEngine;

public class BarManager : MonoBehaviour
{
    [SerializeField] private HPBar healthBarPrefab;
    [SerializeField] private MPBar mpBarPrefab;
    [SerializeField] private Transform canvasTransform;

    /// <summary>
    /// 유닛 스폰 시 HP 바와 MP 바를 함께 생성한다.
    /// </summary>
    public void CreateBars(UnitController target)
    {
        // HP 바 생성
        if (healthBarPrefab != null)
        {
            HPBar hpBar = Instantiate(healthBarPrefab, canvasTransform);
            hpBar.Initialize(target, target.uiAnchor);
        }

        // MP 바 생성
        if (mpBarPrefab != null)
        {
            MPBar mpBar = Instantiate(mpBarPrefab, canvasTransform);
            mpBar.Initialize(target, target.uiAnchor);
        }
    }

    private void OnEnable()
    {
        UnitController.OnUnitSpawned += CreateBars;
    }

    private void OnDisable()
    {
        UnitController.OnUnitSpawned -= CreateBars;
    }
}
