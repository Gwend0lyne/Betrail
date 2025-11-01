using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class AimPad3D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool IsActive => _active;
    public int ActivePointerId => _activePointerId;
    public Vector2 ScreenPosition => _screenPos;

    [Header("Debug")]
    [Tooltip("Active les logs détaillant les interactions sur la plateforme tangible.")]
    public bool enableDebugLogs = true;

    const string LogPrefix = "[AimPad3D]";

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
        LogDebug($"OnPointerDown -> pointerId={_activePointerId}, screenPos={_screenPos}");
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_active || e.pointerId != _activePointerId) return;
        _screenPos = e.position;
        LogDebug($"OnDrag -> pointerId={e.pointerId}, screenPos={_screenPos}");
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!_active || e.pointerId != _activePointerId) return;
        _active = false;
        _activePointerId = -1;
        LogDebug($"OnPointerUp -> pointerId={e.pointerId}");
    }

    void LogDebug(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"{LogPrefix} [{name}] {message}", this);
    }
}
