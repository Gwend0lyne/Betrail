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
        Debug.Log("bombe qui explose");

        var hits = Physics.OverlapSphere(
            transform.position, explosionRadius, affectMask, QueryTriggerInteraction.Ignore);

        foreach (var h in hits)
        {
            // Récupère le MoveLeft touché
            var mover = h.GetComponent<MoveLeft>() ?? h.GetComponentInParent<MoveLeft>();
            if (mover == null) continue;

            // 1) Baisse instantanée de la jauge/vitesse
            if (mover.source != null)
                mover.source.ApplyInstantPenalty(penaltyPercent);

            // 2) Tangage 3 s (le mouvement NE s'arrête pas)
            mover.ApplySway(swayAmplitudeDeg, swayDuration, swayFrequencyHz);
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