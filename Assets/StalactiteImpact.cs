using UnityEngine;

public class StalactiteImpact : MonoBehaviour
{
    [Header("Références injectées par le spawner")]
    public Collider chariotCollider;
    public GameObject icePilePrefab;
    public Transform worldParent;     // <<< parent monde (ex: grotte)

    [Header("Réglages impact")]
    public float iceYOffset = 0f;
    public float wagonHitOverlapRadius = 0.6f;

    bool resolved;

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

        // >>> Parent sous le monde qui défile (grotte)
        var parent = worldParent != null ? worldParent : null;
        var ice = Instantiate(icePilePrefab, transform.position + Vector3.up * iceYOffset, Quaternion.identity, parent);

        var pile = ice.GetComponent<IcePile>();
        if (pile != null)
            pile.Initialize(hitWagon ? IcePile.ImpactCase.Wagon : IcePile.ImpactCase.Rails);

        Destroy(gameObject);
    }
}