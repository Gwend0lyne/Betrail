using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeamContinuous : MonoBehaviour
{
    LineRenderer lr;
    bool running;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.enabled = false;
        lr.positionCount = 2;
    }

    /// <summary>
    /// Démarre l'affichage du laser en suivant 'muzzle' (origine) et 'pivot' (direction).
    /// S'arrête au bout de 'duration' secondes OU si 'shouldStop()' retourne true.
    /// </summary>
    public void Begin(
        Transform muzzle,
        Transform pivot,
        float range,
        float duration,
        Func<bool> shouldStop,
        Action<RaycastHit> onHit = null)
    {
        if (running) return;
        StartCoroutine(Run(muzzle, pivot, range, duration, shouldStop, onHit));
    }

    public void StopNow()
    {
        running = false; // le coroutine s'arrêtera au prochain cycle
        lr.enabled = false;
        Destroy(gameObject);
    }

    IEnumerator Run(
        Transform muzzle,
        Transform pivot,
        float range,
        float duration,
        Func<bool> shouldStop,
        Action<RaycastHit> onHit)
    {
        running = true;
        lr.enabled = true;
        float t = 0f;

        while (running && t < duration && (shouldStop == null || !shouldStop()))
        {
            if (!muzzle || !pivot) break;

            Vector3 origin = muzzle.position;
            // yaw uniquement (plan XZ)
            Vector3 dir = pivot.forward; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
            dir.Normalize();

            Vector3 end = origin + dir * range;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
            {
                end = hit.point;
                onHit?.Invoke(hit);
            }

            lr.SetPosition(0, origin);
            lr.SetPosition(1, end);

            t += Time.deltaTime;
            yield return null;
        }

        lr.enabled = false;
        Destroy(gameObject);
    }
}
