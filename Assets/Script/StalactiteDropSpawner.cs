using System.Collections;
using UnityEngine;

public class StalactiteDropSpawner : MonoBehaviour
{
    [Header("Références monde")]
    public Transform worldStart;        // 0%
    public Transform worldEnd;          // 100%
    public Transform groundYRef;        // Y d’impact (rail)
    public Transform worldParent;       // parent du spawn (ex: "grotte"). Laisser vide = this.

    [Header("Prefabs")]
    public GameObject stalactiteWorldPrefab;  // prefab 3D qui tombe (Transform, pas RectTransform)
    public GameObject icePilePrefab;          // prefab amas de glace (cube temporaire OK)

    [Header("Réfs gameplay (scène)")]
    public Collider chariotCollider;          // BoxCollider du chariot (objet de scène)

    [Header("Chute")]
    public float dropDelay   = 1.0f;
    public float spawnHeight = 6.0f;
    public float fallDuration = 0.6f;
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0,0,1,1);

    [Header("Debug")]
    public bool verbose = true;
    [Range(0f,1f)] public float lastT;
    public Vector3 lastTargetWorld;

    public void ScheduleDrop(float normalizedAlongRail)
    {
        if (!worldStart || !worldEnd)
        {
            Debug.LogWarning("[StalactiteDropSpawner] R�f�rences manquantes (worldStart/worldEnd).");
            return;
        }

        var spawnPrefab = ResolveFallingPrefab();
        if (!spawnPrefab)
        {
            Debug.LogWarning("[StalactiteDropSpawner] Aucun prefab de chute disponible.");
            return;
        }

        lastT = Mathf.Clamp01(normalizedAlongRail);
        StartCoroutine(DropRoutine(spawnPrefab, lastT));
    }


    GameObject ResolveFallingPrefab()
    {
        if (stalactiteWorldPrefab) return stalactiteWorldPrefab;
        if (icePilePrefab) return icePilePrefab;
        return null;
    }

    private IEnumerator DropRoutine(GameObject spawnPrefab, float t)
    {
        // 1) Cible monde
        Vector3 target = Vector3.Lerp(worldStart.position, worldEnd.position, t);
        float groundY = groundYRef ? groundYRef.position.y : target.y;
        target.y = groundY;
        lastTargetWorld = target;

        if (verbose) Debug.Log($"[DropSpawner] t={t:F2}  start={worldStart.position}  end={worldEnd.position}  target={target}");

        // 2) Délai avant l’apparition
        yield return new WaitForSeconds(dropDelay);

        // 3) Apparition au-dessus puis descente animée
        Vector3 spawnPos = target + Vector3.up * spawnHeight;
        Transform parent = worldParent ? worldParent : transform;
        GameObject go = Instantiate(spawnPrefab, spawnPos, Quaternion.identity, parent);

        // Sécurités de visibilité
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend) rend.enabled = true;
        if (go.transform.localScale == Vector3.zero) go.transform.localScale = Vector3.one;

        // Injection des références de scène à l’instance
        var impact = go.GetComponent<StalactiteImpact>();
        if (impact == null)
        {
            impact = go.AddComponent<StalactiteImpact>();
        }

        impact.chariotCollider = chariotCollider;
        impact.icePilePrefab   = icePilePrefab;
        impact.worldParent     = worldParent ? worldParent : transform;

        // 4) Animation de chute
        float tElapsed = 0f;
        while (tElapsed < fallDuration)
        {
            tElapsed += Time.deltaTime;
            float k = fallCurve.Evaluate(Mathf.Clamp01(tElapsed / fallDuration));
            go.transform.position = Vector3.Lerp(spawnPos, target, k);
            yield return null;
        }
        go.transform.position = target;

        // 5) Résolution de l’impact
        if (impact != null) impact.ResolveImpact();

        if (verbose) Debug.Log($"[DropSpawner] Impact monde @ {target}");
    }

    private void OnDrawGizmosSelected()
    {
        if (worldStart && worldEnd)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(worldStart.position, worldEnd.position);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastTargetWorld, 0.2f);
        }
    }
}
