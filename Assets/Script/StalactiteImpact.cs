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

    public bool DeflectedByNet { get; private set; }
    bool resolved;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Filet"))
        {
            DeflectedByNet = true;
            Destroy(gameObject); // ne pas laisser l’anim continuer
        }
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

        var parent = worldParent != null ? worldParent : null;
        var ice = Instantiate(icePilePrefab, transform.position + Vector3.up * iceYOffset, Quaternion.identity, parent);

        var pile = ice.GetComponent<IcePile>() ?? ice.AddComponent<IcePile>();
        pile.Initialize(hitWagon ? IcePile.ImpactCase.Wagon : IcePile.ImpactCase.Rails);

        Destroy(gameObject);
    }
}