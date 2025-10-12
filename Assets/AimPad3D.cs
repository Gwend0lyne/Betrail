using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class AimPad3D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool IsActive => _active;
    public int ActivePointerId => _activePointerId;
    public Vector2 ScreenPosition => _screenPos;

    bool _active = false;
    int _activePointerId = -1;
    Vector2 _screenPos;

    public void OnPointerDown(PointerEventData e)
    {
        // si pas déjà pris, on capture ce doigt pour la visée
        if (_active) return;
        _active = true;
        _activePointerId = e.pointerId;
        _screenPos = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_active || e.pointerId != _activePointerId) return;
        _screenPos = e.position;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_active || e.pointerId != _activePointerId) return;
        _active = false;
        _activePointerId = -1;
    }
}