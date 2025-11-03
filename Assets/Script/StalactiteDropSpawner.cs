using System.Collections;
using UnityEngine;

public class StalactiteDropSpawner : MonoBehaviour
{
    [Header("Références monde")]
    public Transform worldStart;
    public Transform worldEnd;
    public Transform groundYRef;
    public Transform worldParent;

    [Header("Prefabs")]
    public GameObject stalactiteWorldPrefab;
    public GameObject icePilePrefab;

    [Header("Réfs gameplay (scène)")]
    public Collider chariotCollider;

    [Header("Chute")]
    public float dropDelay = 1.0f;
    public float spawnHeight = 6.0f;
    public float fallDuration = 0.6f;
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sécurité anti-doubles")]
    public float spawnCooldown = 0.2f;   // petit cooldown pour éviter les doubles
    private bool dropInFlight = false;
    private float lastSpawnTime = -999f;
    private Coroutine activeCo;

    [Header("Debug")]
    public bool verbose = true;
    [Range(0f, 1f)] public float lastT;
    public Vector3 lastTargetWorld;

    public void ScheduleDrop(float normalizedAlongRail)
    {
        if (!worldStart || !worldEnd)
        {
            Debug.LogWarning("[DropSpawner] Références manquantes (worldStart/worldEnd).");
            return;
        }

        // anti-spam/doublon
        if (dropInFlight || Time.time - lastSpawnTime < spawnCooldown)
        {
            if (verbose) Debug.Log("[DropSpawner] Drop ignoré (déjà en cours / cooldown).");
            return;
        }

        var spawnPrefab = stalactiteWorldPrefab ? stalactiteWorldPrefab : icePilePrefab;
        if (!spawnPrefab)
        {
            Debug.LogWarning("[DropSpawner] Aucun prefab de chute disponible.");
            return;
        }

        lastT = Mathf.Clamp01(normalizedAlongRail);
        activeCo = StartCoroutine(DropRoutine(spawnPrefab, lastT));
    }

    private IEnumerator DropRoutine(GameObject spawnPrefab, float t)
    {
        dropInFlight = true;
        lastSpawnTime = Time.time;

        Vector3 target = Vector3.Lerp(worldStart.position, worldEnd.position, t);
        float groundY = groundYRef ? groundYRef.position.y : target.y;
        target.y = groundY;
        lastTargetWorld = target;

        if (verbose) Debug.Log($"[DropSpawner] t={t:F2} start={worldStart.position} end={worldEnd.position} target={target}");

        yield return new WaitForSeconds(dropDelay);

        Vector3 spawnPos = target + Vector3.up * spawnHeight;
        Transform parent = worldParent ? worldParent : transform;
        GameObject go = Instantiate(spawnPrefab, spawnPos, Quaternion.identity, parent);

        // Sécurise l’affichage
        if (go.transform.localScale == Vector3.zero) go.transform.localScale = Vector3.one;
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend) rend.enabled = true;

        // IMPORTANT : RB cinématique pour que le Trigger du filet marche proprement
        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var impact = go.GetComponent<StalactiteImpact>() ?? go.AddComponent<StalactiteImpact>();
        impact.chariotCollider = chariotCollider;
        impact.icePilePrefab   = icePilePrefab;
        impact.worldParent     = parent;

        float tElapsed = 0f;
        while (tElapsed < fallDuration)
        {
            // Si détruit au vol (filet), on sort sans rien faire.
            if (go == null || impact == null || impact.DeflectedByNet)
                goto FINISH;

            tElapsed += Time.deltaTime;
            float k = fallCurve.Evaluate(Mathf.Clamp01(tElapsed / fallDuration));
            go.transform.position = Vector3.Lerp(spawnPos, target, k);
            yield return null;
        }

        // Dernière vérif avant impact
        if (go != null && impact != null && !impact.DeflectedByNet)
        {
            impact.ResolveImpact();
            if (verbose) Debug.Log($"[DropSpawner] Impact monde @ {target}");
        }

    FINISH:
        dropInFlight = false;
        activeCo = null;
    }
}
