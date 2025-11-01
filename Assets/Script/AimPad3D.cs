using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class AimPad3D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool IsActive => _active;
    public int ActivePointerId => _activePointerId;
    public Vector2 ScreenPosition => _screenPos;

    [Tooltip("Référence au canon contrôlé par cette plateforme tangible.")]
    public HeliCanonFloat owner;

    [Header("Debug")]
    [Tooltip("Active les logs détaillant les interactions sur la plateforme tangible.")]
    public bool enableDebugLogs = true;

    [Header("Fan Animation")]
    [Tooltip("Transform du ventilateur à faire tourner autour de l'axe Y.")]
    public Transform platformFan;
    public float platformFanSpinSpeed = 540f;

    const string LogPrefix = "[AimPad3D]";

    bool _active = false;
    int _activePointerId = -1;
    Vector2 _screenPos;
    bool _ownsHold = false;
    Transform _cachedPlatformFan;
    Quaternion _platformFanBaseRotation = Quaternion.identity;
    float _platformFanAngle;

    void Awake()
    {
        AutoAssignPlatformParts(includeInactive: true);
        if (!owner)
            owner = GetComponentInParent<HeliCanonFloat>();

        UpdateAnimationDefaults(force: true);

        LogDebug($"Awake -> owner={NameOrNone(owner)}, platformFan={NameOrNone(platformFan)}");
        if (!platformFan) LogWarning("platformFan n'est pas assigné (rotation désactivée).");
    }

    void Update()
    {
        AnimateFan();
    }

    public void OnPointerDown(PointerEventData e)
    {
        // si pas déjà pris, on capture ce doigt pour la visée
        if (_active) return;
        _active = true;
        _activePointerId = e.pointerId;
        _screenPos = e.position;
        _ownsHold = owner && owner.TryBeginHoldFromAimPad(this, e.pointerId);
        if (!_ownsHold && owner && owner.IsHeld && owner.CurrentHeldPointerId == e.pointerId)
        {
            _ownsHold = true;
            LogDebug($"OnPointerDown -> récupération du hold existant (pointerId={e.pointerId}).");
        }
        else if (!_ownsHold && owner)
        {
            LogDebug($"OnPointerDown -> hold non démarré (canon déjà tenu par pointerId={owner.CurrentHeldPointerId}).");
        }
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
        bool released = false;
        if (_ownsHold && owner)
        {
            owner.ReleaseHoldFromAimPad(this, e.pointerId);
            _ownsHold = false;
            released = true;
        }
        else if (owner && owner.IsHeld && owner.CurrentHeldPointerId == e.pointerId)
        {
            owner.ReleaseHoldFromAimPad(this, e.pointerId);
            released = true;
        }
        LogDebug($"OnPointerUp -> pointerId={e.pointerId}, releasedHold={released}");
    }

    void AnimateFan()
    {
        float dt = Time.deltaTime;

        if (platformFan)
        {
            _platformFanAngle = Mathf.Repeat(_platformFanAngle + platformFanSpinSpeed * dt, 360f);
            platformFan.localRotation = _platformFanBaseRotation * Quaternion.Euler(0f, _platformFanAngle, 0f);
        }
    }

    void AutoAssignPlatformParts(bool includeInactive)
    {
        Transform searchRoot = owner ? owner.transform : (transform.parent ? transform.parent : transform);

        if (!platformFan)
        {
            Transform fanSearchRoot = searchRoot;
            Transform candidate = FindChildContaining(fanSearchRoot, "fan", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(fanSearchRoot, "propeller", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(fanSearchRoot, "helice", includeInactive);

            if (candidate)
            {
                platformFan = candidate;
                LogDebug($"AutoAssign -> platformFan assigne a {NameOrNone(platformFan)}");
            }
            else
            {
                LogWarning("AutoAssign -> aucun platformFan trouve.");
            }
        }
        else
        {
            LogDebug($"AutoAssign -> platformFan deja assigne ({NameOrNone(platformFan)})");
        }

        UpdateAnimationDefaults();
    }

    void UpdateAnimationDefaults(bool force = false)
    {
        if (force || platformFan != _cachedPlatformFan)
        {
            _cachedPlatformFan = platformFan;
            _platformFanBaseRotation = platformFan ? platformFan.localRotation : Quaternion.identity;
            _platformFanAngle = 0f;
        }
    }

    void Reset()
    {
        if (!owner)
            owner = GetComponentInParent<HeliCanonFloat>();
        AutoAssignPlatformParts(includeInactive: true);
        UpdateAnimationDefaults(force: true);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (!owner)
                owner = GetComponentInParent<HeliCanonFloat>();
            AutoAssignPlatformParts(includeInactive: true);
            UpdateAnimationDefaults(force: true);
        }
    }
#endif

    void LogDebug(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"{LogPrefix} [{name}] {message}", this);
    }

    void LogWarning(string message)
    {
        if (!enableDebugLogs) return;
        Debug.LogWarning($"{LogPrefix} [{name}] {message}", this);
    }

    static Transform FindChildContaining(Transform root, string token, bool includeInactive)
    {
        if (!root || string.IsNullOrEmpty(token)) return null;
        var comparison = StringComparison.OrdinalIgnoreCase;
        var children = root.GetComponentsInChildren<Transform>(includeInactive);
        foreach (var child in children)
        {
            if (child == root) continue;
            if (child.name.IndexOf(token, comparison) >= 0)
                return child;
        }

        return null;
    }

    static string NameOrNone(UnityEngine.Object obj)
    {
        return obj ? obj.name : "null";
    }
}
