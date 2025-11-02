using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class FireOnTouchDown3D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public HeliCanonFloat owner;
    int activePointerId = int.MinValue;
    bool pointerHeld;

    public void OnPointerDown(PointerEventData e)
    {
        pointerHeld = true;
        activePointerId = e.pointerId;
        owner?.RegisterFireButtonPointer(e.pointerId);
        owner?.Fire();             // démarre le laser continu
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!pointerHeld || e.pointerId != activePointerId)
            return;

        pointerHeld = false;
        owner?.UnregisterFireButtonPointer(e.pointerId);
        activePointerId = int.MinValue;
    }

    void OnDisable()
    {
        if (pointerHeld)
        {
            owner?.UnregisterFireButtonPointer(activePointerId);
            pointerHeld = false;
            activePointerId = int.MinValue;
        }
    }
}
