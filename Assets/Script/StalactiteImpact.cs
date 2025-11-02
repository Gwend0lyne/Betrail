using UnityEngine;

public class StalactiteImpact : MonoBehaviour
{
    [Header("Références injectées par le spawner")]
    public Collider chariotCollider;
    public GameObject icePilePrefab;
    public Transform worldParent;

    [Header("Réglages impact")]
    public float iceYOffset = 0f;
    public float wagonHitOverlapRadius = 0.6f;

    [Header("Détection filet")]
    [Tooltip("Tag utilisé par le filet (optionnel). Laisser vide si non utilisé.")]
    public string filetTag = "Filet";
    [Tooltip("Marge autour du collider du stalactite pour l’OverlapBox.")]
    public Vector3 overlapPadding = new Vector3(0.05f, 0.05f, 0.05f);

    bool resolved;
    Collider selfCol;
    Renderer selfRenderer;

    void Awake()
    {
        selfCol = GetComponent<Collider>();
        selfRenderer = GetComponentInChildren<Renderer>();
        if (selfCol == null)
        {
            // Sécurité: si pas de collider, on en met un (cohérent avec le spawner)
            selfCol = gameObject.AddComponent<BoxCollider>();
        }
    }

    void Update()
    {
        if (resolved) return;

        // ► Détection du filet (même si les events PhysX ne se déclenchent pas)
        if (TouchingNetNow())
        {
            // Le filet "renvoie" le stalactite : on le détruit sans créer de glace
            Destroy(gameObject);
            return;
        }
    }

    bool TouchingNetNow()
    {
        if (!selfCol) return false;

        // Demi-extents pour OverlapBox : taille du collider + petite marge
        Vector3 half = (selfCol.bounds.extents + 0.5f * overlapPadding);

        // On scrute tous les colliders qui touchent le volume du stalactite
        var hits = Physics.OverlapBox(selfCol.bounds.center, half, transform.rotation,
                                      ~0, // toutes layers
                                      QueryTriggerInteraction.Collide);

        foreach (var h in hits)
        {
            if (h == selfCol) continue;

            // Détection souple : tag OU composant FiletKill
            bool isNet = ( !string.IsNullOrEmpty(filetTag) && h.CompareTag(filetTag) )
                         || h.GetComponentInParent<FiletKill>() != null;

            if (isNet) return true;
        }
        return false;
    }

    public void ResolveImpact()
    {
        if (resolved) return;
        resolved = true;

        bool hitWagon = false;
        if (chariotCollider != null)
        {
            var p = transform.position;
            var closest = chariotCollider.ClosestPoint(p);
            hitWagon = (closest - p).sqrMagnitude <= wagonHitOverlapRadius * wagonHitOverlapRadius;
        }

        var parent = worldParent ? worldParent : null;
        var ice = Instantiate(icePilePrefab, transform.position + Vector3.up * iceYOffset,
                              Quaternion.identity, parent);

        var pile = ice.GetComponent<IcePile>();
        if (pile == null) pile = ice.AddComponent<IcePile>();
        pile.Initialize(hitWagon ? IcePile.ImpactCase.Wagon : IcePile.ImpactCase.Rails);

        Destroy(gameObject);
    }
}
