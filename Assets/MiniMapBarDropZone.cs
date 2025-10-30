using UnityEngine;
using UnityEngine.EventSystems;

public class MiniMapBarDropZone : MonoBehaviour, IDropHandler
{
    [Header("Spawner monde (sur 'grotte')")]
    public StalactiteDropSpawner dropSpawner;

    [Range(0f,1f)] public float normalizedDropPosition;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || dropSpawner == null) return;

        var barRect = (RectTransform)transform;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                barRect, eventData.position, eventData.pressEventCamera, out var local)) return;

        // vertical, 0% en bas → 100% en haut
        float y = local.y + barRect.rect.height * barRect.pivot.y;
        normalizedDropPosition = Mathf.Clamp01(y / barRect.rect.height);

        // Lance la chute dans le monde réel
        dropSpawner.ScheduleDrop(normalizedDropPosition);
        Debug.Log($"[MiniMapBarDropZone] t envoyé = {normalizedDropPosition:F2}");
    }
}