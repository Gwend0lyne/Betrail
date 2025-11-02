using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class Bomb : MonoBehaviour, IPointerClickHandler
{
    [Header("Explosion")]
    public float explosionRadius = 2.5f;
    public LayerMask affectMask = ~0;
    public bool destroyOnExplode = true;

    [Header("Effets")]
    public float penaltyPercent = 30f;    // baisse instantanée de la jauge (ex: 30)
    public float swayAmplitudeDeg = 6f;   // tangage léger
    public float swayDuration = 3f;       // 3 secondes
    public float swayFrequencyHz = 1f;    // lent

    public void OnPointerClick(PointerEventData e) => Explode();
    void OnMouseDown() => Explode();

    void Explode()
    {
        Debug.Log($"💣 Bombe qui explose à {transform.position}");

        var hits = Physics.OverlapSphere(
            transform.position, explosionRadius, affectMask, QueryTriggerInteraction.Ignore);

        foreach (var h in hits)
        {
            // 1️⃣ Comportement générique : MoveLeft pénalisé
            var mover = h.GetComponent<MoveLeft>() ?? h.GetComponentInParent<MoveLeft>();
            if (mover && mover.source != null)
                mover.source.ApplyInstantPenalty(penaltyPercent);

            // 2️⃣ Comportement spécial : réaction du wagon comme un tir de laser
            var wagonReaction = h.GetComponent<WagonLaserReaction>() ?? h.GetComponentInParent<WagonLaserReaction>();
            if (wagonReaction)
            {
                wagonReaction.HandleLaserHit();
                Debug.Log($"→ Wagon touché par explosion : {wagonReaction.name}");
            }
        }

        if (destroyOnExplode) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}
