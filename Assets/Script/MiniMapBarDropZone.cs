using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MiniMapBarDropZone : MonoBehaviour, IDropHandler
{
    [Header("Spawner monde (sur 'grotte')")]
    public StalactiteDropSpawner dropSpawner;

    [Header("Prefab de l'icône (dans Canvas MiniMap)")]
    public GameObject stalactiteIconPrefab;

    [Header("Parent UI pour les icônes")]
    public RectTransform iconContainer;

    [Range(0f,1f)] public float normalizedDropPosition;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || dropSpawner == null) return;

        var barRect = (RectTransform)transform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                barRect, eventData.position, eventData.pressEventCamera, out var local)) return;

        // Vertical : 0% en bas → 100% en haut
        float y = local.y + barRect.rect.height * barRect.pivot.y;
        normalizedDropPosition = Mathf.Clamp01(y / barRect.rect.height);

        // 1️⃣ Appel logique (monde)
        dropSpawner.ScheduleDrop(normalizedDropPosition);
        Debug.Log($"[MiniMapBarDropZone] t envoyé = {normalizedDropPosition:F2}");

        // 2️⃣ Création visuelle (mini-map)
        if (stalactiteIconPrefab != null && iconContainer != null)
        {
            // Instancie l'icône
            GameObject icon = Instantiate(stalactiteIconPrefab, iconContainer);

            // Positionne l'icône selon la position du drop
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0f);
            iconRect.anchorMax = new Vector2(0.5f, 0f);
            iconRect.anchoredPosition = new Vector2(0f, normalizedDropPosition * barRect.rect.height);
        }
    }
}
