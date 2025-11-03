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
    bool _pendingToggleOff;
    int _holdPointerId = -1;
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
        EnsureOwnerHoldSync();
    }

    public void OnPointerDown(PointerEventData e)
    {
        _screenPos = e.position;

        if (!_active)
        {
            EngageAim(e);
            return;
        }

        _activePointerId = e.pointerId;
        _pendingToggleOff = true;
        LogDebug($"OnPointerDown -> pointerId={_activePointerId}, screenPos={_screenPos}");
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_active || e.pointerId != _activePointerId) return;
        _screenPos = e.position;
        _pendingToggleOff = false;
        LogDebug($"OnDrag -> pointerId={e.pointerId}, screenPos={_screenPos}");
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != _activePointerId) return;

        bool toggleOff = _pendingToggleOff;
        _activePointerId = -1;
        _pendingToggleOff = false;

        if (toggleOff)
            DisengageAim("tap-toggle", e.pointerId);

        LogDebug($"OnPointerUp -> pointerId={e.pointerId}, toggleOff={toggleOff}");
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

    void EngageAim(PointerEventData e)
    {
        if (!owner)
        {
            LogWarning("EngageAim -> owner manquant, visée impossible.");
            ResetState();
            return;
        }

        _activePointerId = e.pointerId;
        _pendingToggleOff = false;
        _ownsHold = owner.TryBeginHoldFromAimPad(this, e.pointerId);
        if (!_ownsHold && owner.IsHeld && owner.CurrentHeldPointerId == e.pointerId)
        {
            _ownsHold = true;
            LogDebug($"EngageAim -> récupération du hold existant (pointerId={e.pointerId}).");
        }
        else if (!_ownsHold && owner.IsHeld)
        {
            LogDebug($"EngageAim -> hold déjà détenu par pointerId={owner.CurrentHeldPointerId}.");
        }
        else if (!_ownsHold)
        {
            LogDebug("EngageAim -> échec du démarrage du hold.");
        }

        bool holdActive = owner.IsHeld && owner.CurrentHeldPointerId == e.pointerId;
        if (!holdActive)
        {
            ResetState();
            return;
        }

        _holdPointerId = owner.CurrentHeldPointerId;
        _active = true;
        LogDebug($"EngageAim -> hold acquis (pointerId={_holdPointerId}).");
    }

    void DisengageAim(string reason, int triggeringPointerId)
    {
        if (!_active)
        {
            LogDebug($"DisengageAim -> ignoré ({reason}), aucune visée active.");
            return;
        }

        bool released = false;
        if (owner)
        {
            int pointerIdToRelease = _holdPointerId;
            if (pointerIdToRelease == -1 && owner.IsHeld)
                pointerIdToRelease = owner.CurrentHeldPointerId;

            if (pointerIdToRelease != -1)
            {
                owner.ReleaseHoldFromAimPad(this, pointerIdToRelease);
                released = true;
            }
        }

        ResetState();
        LogDebug($"DisengageAim -> reason={reason}, triggerPointer={triggeringPointerId}, releaseRequested={released}");
    }

    void EnsureOwnerHoldSync()
    {
        if (!_active)
            return;

        if (!owner || !owner.IsHeld)
        {
            ResetState();
            LogDebug("EnsureOwnerHoldSync -> hold perdu, visée réinitialisée.");
        }
    }

    void ResetState()
    {
        _active = false;
        _activePointerId = -1;
        _ownsHold = false;
        _pendingToggleOff = false;
        _holdPointerId = -1;
    }
}
