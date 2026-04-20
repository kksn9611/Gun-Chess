using UnityEngine;
using System.Collections;


[RequireComponent(typeof(UnitController))]
public class UnitVisuals : MonoBehaviour
{
    [Header("Fire Point")]
    [SerializeField] private Transform firePoint;

    [Header("Effect")]
    [SerializeField] private TrailRenderer bulletTrailPrefab;
    [SerializeField] private float bulletReachTime = 0.1f;

    public void FireWeaponEffect(UnitController target)
    {
        if (target == null) return;
        Vector3 targetCenterPos = target.transform.position + Vector3.up * 1f;

        if (bulletTrailPrefab != null)
        {
            //Start from firepoint
            TrailRenderer trail = Instantiate(bulletTrailPrefab, firePoint.position, Quaternion.identity);

            //toward target logic
            StartCoroutine(SpawnTrail(trail, targetCenterPos));
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            time += Time.deltaTime / bulletReachTime;
            yield return null;
        }
        trail.transform.position = hitPoint;
        Destroy(trail.gameObject, trail.time);
    }
}


