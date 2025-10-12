using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class WallDraggableMultiTouch : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Canvas uiCanvas;
    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (uiCanvas == null) uiCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData) { /* rien de spécial */ }

    public void OnDrag(PointerEventData eventData)
    {
        // Déplacement propre en UI
        float k = (uiCanvas != null && uiCanvas.scaleFactor != 0f) ? uiCanvas.scaleFactor : 1f;
        rt.anchoredPosition += eventData.delta / k;
    }

    public void OnEndDrag(PointerEventData eventData) { /* ne détruit pas le mur en tactile */ }
}