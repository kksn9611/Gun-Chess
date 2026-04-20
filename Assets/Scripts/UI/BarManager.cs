using System.Collections.Generic;
using UnityEngine;

public class BarManager : MonoBehaviour
{
    [SerializeField] private HPBar healthBarPrefab;
    [SerializeField] private MPBar mpBarPrefab;
    [SerializeField] private Transform canvasTransform;

    /// <summary>
    /// Create HP bar and MP bar when a unit spawns.
    /// </summary>
    public void CreateBars(UnitController target)
    {
        // Create HP bar
        if (healthBarPrefab != null)
        {
            HPBar hpBar = Instantiate(healthBarPrefab, canvasTransform);
            hpBar.Initialize(target, target.uiAnchor);
        }

        // Create MP bar
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
