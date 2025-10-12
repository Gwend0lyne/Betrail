using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    public float range = 30f;
    public float visibleTime = 0.08f;

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.enabled = false;
    }

    public void Fire(Vector3 origin, Vector3 dir)
    {
        Vector3 end = origin + dir.normalized * range;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
            end = hit.point;

        StopAllCoroutines();
        StartCoroutine(ShowOnce(origin, end));
    }

    IEnumerator ShowOnce(Vector3 a, Vector3 b)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.enabled = true;
        yield return new WaitForSeconds(visibleTime);
        lr.enabled = false;
        Destroy(gameObject); // c'est un effet one-shot
    }
}