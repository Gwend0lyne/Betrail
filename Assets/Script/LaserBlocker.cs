using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Marque un collider (ou un parent) comme bloqueur du laser d'hélicoptère.
/// Quand le faisceau le touche, il s'arrête avant d'atteindre le wagon.
/// </summary>
[DisallowMultipleComponent]
public class LaserBlocker : MonoBehaviour
{
    [Tooltip("Affiche un log quand le laser touche ce bloqueur.")]
    public bool enableDebugLogs = true;

    [Tooltip("Couleur du gizmo dessiné quand l'objet est sélectionné.")]
    public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);

    [Tooltip("Evènement déclenché lorsque le laser touche ce bloqueur.")]
    public UnityEvent onLaserBlocked;

    public void NotifyBlocked(RaycastHit hit)
    {
        if (enableDebugLogs)
            Debug.Log($"[LaserBlocker] Laser stoppé sur '{name}' à {hit.point}", this);

        onLaserBlocked?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (!col)
            return;

        Gizmos.color = gizmoColor;
        var bounds = col.bounds;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
