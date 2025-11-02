using UnityEngine;

public class FiletKill : MonoBehaviour
{
    // works with trigger or collision, whichever you choose
    private void OnTriggerEnter(Collider other)  => TryKill(other);
    private void OnCollisionEnter(Collision col) => TryKill(col.collider);

    private void TryKill(Collider other)
    {
        // Is it a falling stalactite? (has StalactiteImpact on the root)
        var impact = other.GetComponentInParent<StalactiteImpact>();
        if (impact == null) return;

        // Kill the stalactite now; DropSpawner's "impact.ResolveImpact()" will be skipped
        Destroy(impact.gameObject);
        // Optional: little feedback here (sound/particles) if you want
        // e.g., Instantiate(popFX, other.transform.position, Quaternion.identity);
    }
}