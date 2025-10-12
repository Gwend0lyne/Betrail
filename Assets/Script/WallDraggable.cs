using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class WallDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Canvas uiCanvas;
    private RectTransform rt;
    private Vector2 startPos;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (uiCanvas == null) uiCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startPos = rt.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float k = (uiCanvas != null && uiCanvas.scaleFactor != 0f) ? uiCanvas.scaleFactor : 1f;
        rt.anchoredPosition += eventData.delta / k;
    }

    public void OnEndDrag(PointerEventData eventData) { }
}