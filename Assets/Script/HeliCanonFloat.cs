using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))] // pour recevoir les touches 3D
public class HeliCanonFloat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Patrouille horizontale (monde)")]
    public float minX = -11f, maxX = 0f, horizontalSpeed = 0.5f;

    [Header("Flottement vertical")]
    public float verticalAmplitude = 0.5f, verticalFrequency = 0.2f, verticalPhase = 0f;

    [Header("Aiming")]
    [Tooltip("Transform pivot qui ne fait que yaw (ex: 'PivotYaw').")]
    public Transform cannonPivot;
    public float aimSmoothing = 15f;
    [Tooltip("Clamp autour de l'orientation de BASE (°).")]
    public float yawLimit = 50f; // -50..+50

    [Header("UI / Tir")]
    public GameObject textExplode;              // objet 3D à toucher pour tirer (avec Collider)
    public Transform muzzle;                    // bout du canon
    public LaserBeamContinuous laserPrefab;     // prefab du visuel laser (LineRenderer + script)
    public float fireRange = 30f;
    public float fireDuration = 5f;

    [Header("Aiming Zone")]
    public AimPad3D aimPad;                     // zone dédiée pour la visée (évite les conflits de touch)

    [Header("Debug")]
    [Tooltip("Active l'affichage de logs détaillés dans la console pour aider au debug.")]
    public bool enableDebugLogs = true;

    const string LogPrefix = "[HeliCanonFloat]";

    [Header("Animation")]
    [Tooltip("Transform visuel du canon qui doit osciller autour de Z lorsqu'il est à l'arrêt.")]
    public Transform laserVisualRoot;
    [Tooltip("Transform de l'hélice/ventilateur à faire tourner autour de Z en continu.")]
    public Transform laserFan;
    [Tooltip("Optionnel: Transform servant d'ancre pour le pivot de visée. Si laissé vide, on cherche Cylinder.005, puis le fan.")]
    public Transform aimPivotMarker;
    public float fanSpinSpeed = 540f;
    public float idleOscillationAmplitude = 10f;
    public float idleOscillationFrequency = 1.2f;
    public float idleOscillationReturnSpeed = 6f;

    [Header("Camera Framing")]
    [Tooltip("Force le canon à rester visible dans la caméra (utile pour les rapports d'aspect étroits).")]
    public bool ensureVisibleOnCamera = true;
    [Tooltip("Bord inférieur de l'écran autorisé (viewport 0..1).")]
    [Range(0f, 0.5f)] public float viewportMinY = 0.08f;
    [Tooltip("Bord supérieur de l'écran autorisé (viewport 0..1).")]
    [Range(0.5f, 1f)] public float viewportMaxY = 0.92f;
    [Tooltip("Décalage supplémentaire pour éviter de coller aux bords (en unités viewport).")]
    [Range(0f, 0.2f)] public float viewportPadding = 0.02f;

    [Header("Wagon Interaction")]
    [Tooltip("Collider du wagon à toucher avec le laser.")]
    public Collider wagonCollider;
    [Tooltip("Comportement déclenché lorsqu'on touche le wagon.")]
    public WagonLaserReaction wagonReaction;
    [Tooltip("Temps minimum entre deux réactions consécutives pendant un même tir.")]
    public float wagonHitCooldown = 0.4f;

    // états runtime
    bool held = false;          // doigt #1 maintient le canon ?
    int heldPointerId = -1;     // id du doigt #1
    float aimYLevel;            // plan horizontal de visée

    public bool IsHeld => held;
    public int CurrentHeldPointerId => heldPointerId;

    Transform cachedLaserVisualRoot;
    Transform cachedLaserFan;
    Transform laserYawPivot;
    Quaternion laserVisualBaseLocalRotation = Quaternion.identity;
    Quaternion laserFanBaseLocalRotation = Quaternion.identity;
    float fanSpinAngle;
    float idleOscillationPhaseOffset;
    float aimYawVelocity;

    // référence centrale (milieu)
    float baseYawWorld;         // Yaw pris à l'Awake = "tout droit"

    // laser en cours
    LaserBeamContinuous activeBeam;

    // déplacement
    Vector3 startPos; float x; int dir;

    float lastWagonHitTime = float.NegativeInfinity;

    void Awake()
    {
        startPos = transform.position;
        if (minX > maxX) { var t = minX; minX = maxX; maxX = t; }
        x = Mathf.Clamp(startPos.x, minX, maxX);
        dir = 1;

        AutoAssignReferences(includeInactive: true);
        if (!cannonPivot) cannonPivot = transform;

        // référence centrale = orientation actuelle du prefab
        baseYawWorld = YawOf(cannonPivot);

        if (textExplode) textExplode.SetActive(false);

        idleOscillationPhaseOffset = UnityEngine.Random.value * Mathf.PI * 2f;
        UpdateAnimationDefaultsIfNeeded(force: true);
        EnsureVisibleOnCamera();

        LogDebug($"Awake -> startPos={startPos}, cannonPivot={NameOrNone(cannonPivot)}, muzzle={NameOrNone(muzzle)}, textExplode={NameOrNone(textExplode)}, aimPad={NameOrNone(aimPad)}, laserVisualRoot={NameOrNone(laserVisualRoot)}, laserFan={NameOrNone(laserFan)}, yawPivot={NameOrNone(laserYawPivot)}");
        if (!cannonPivot) LogWarning("cannonPivot n'est pas assigné.");
        if (!muzzle) LogWarning("muzzle n'est pas assigné.");
        if (!textExplode) LogWarning("textExplode n'est pas assigné.");
        if (!aimPad) LogWarning("aimPad n'est pas assigné.");
        if (!laserVisualRoot) LogWarning("laserVisualRoot n'est pas assigné (oscillation idle désactivée).");
        if (!laserFan) LogWarning("laserFan n'est pas assigné (rotation désactivée).");
    }

    void Start()
    {
        EnsureVisibleOnCamera();
    }

    void Update()
    {
        AnimateVisuals();

        if (held && !Input.GetMouseButton(0) && Input.touchCount == 0)
        {
            LogDebug("Sécurité: pointer libéré automatiquement (aucune entrée active)");
            TryEndHold(heldPointerId, "auto-release");

            fireButtonHeld = false;
            fireButtonPointerId = int.MinValue;

            // 🔧 réactiver le texte de tir pour les futurs clics
            if (textExplode && !textExplode.activeSelf)
                textExplode.SetActive(true);
        }

        if (held)
        {
            if (textExplode && !textExplode.activeSelf) textExplode.SetActive(true);
            UpdateAimFromPad();  // vise via le pad (autre doigt)
            return;              // pas de déplacement pendant le hold
        }

        StopActiveBeam("update-no-hold");
        if (textExplode && textExplode.activeSelf) textExplode.SetActive(false);

        // déplacement va-et-vient
        x += dir * horizontalSpeed * Time.deltaTime;
        if (x >= maxX) { x = maxX; dir = -1; }
        if (x <= minX) { x = minX; dir = +1; }

        // flottement
        float tt = Time.time;
        float y = startPos.y + Mathf.Sin(2f * Mathf.PI * verticalFrequency * tt + verticalPhase) * verticalAmplitude;
        transform.position = new Vector3(x, y, startPos.z);
    }

    // ---------- Maintien sur le canon ----------
    public void OnPointerDown(PointerEventData e)
    {
        if (TryStartHold(e.pointerId, "canon"))
            return;

        LogDebug($"OnPointerDown ignoré -> pointerId={e.pointerId}, heldPointerId={heldPointerId}");
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!held)
        {
            LogDebug($"OnPointerUp ignoré -> aucun hold actif pour pointerId={e.pointerId}");
            return;
        }
        // si ce n'est pas le doigt #1 : ignorer un relâchement fait au-dessus du texte
        if (e.pointerId != heldPointerId)
        {
            if (textExplode && textExplode.activeInHierarchy)
            {
                var cam = Camera.main;
                if (cam)
                {
                    Ray ray = cam.ScreenPointToRay(e.position);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                    {
                        if (hit.collider && (hit.collider.gameObject == textExplode ||
                                             hit.collider.transform.IsChildOf(textExplode.transform)))
                        {
                            return; // un autre doigt relâché sur le texte → on ignore
                        }
                    }
                }
            }
            LogDebug($"OnPointerUp ignoré -> pointerId={e.pointerId}, heldPointerId={heldPointerId}");
            return;
        }

        // Ici : doigt #1 se relâche → on coupe TOUT et on repart
        TryEndHold(e.pointerId, "canon");
    }


    bool fireButtonHeld;
    int fireButtonPointerId = int.MinValue;

    internal void RegisterFireButtonPointer(int pointerId)
    {
        fireButtonHeld = true;
        fireButtonPointerId = pointerId;
    }

    internal void UnregisterFireButtonPointer(int pointerId)
    {
        if (fireButtonHeld && fireButtonPointerId == pointerId)
        {
            fireButtonHeld = false;
            fireButtonPointerId = int.MinValue;
        }
    }


    bool TryStartHold(int pointerId, string source)
    {
        if (held)
        {
            LogDebug($"TryStartHold ignoré ({source}) -> pointerId={pointerId}, déjà tenu par {heldPointerId}");
            return false;
        }

        Transform pivot = cannonPivot ? cannonPivot : transform;
        // IMPORTANT : on NE recentre PAS le pivot. Le clamp reste autour de baseYawWorld enregistré à l'Awake.
        aimYLevel = pivot.position.y;

        held = true;
        heldPointerId = pointerId;

        aimYawVelocity = 0f;

        if (textExplode) textExplode.SetActive(true);
        LogDebug($"Hold démarré via {source} -> pointerId={pointerId}, aimYLevel={aimYLevel:F2}");
        return true;
    }

    bool TryEndHold(int pointerId, string source)
    {
        if (!held)
        {
            LogDebug($"TryEndHold ignoré ({source}) -> pointerId={pointerId}, aucun hold actif.");
            return false;
        }

        if (pointerId != heldPointerId)
        {
            LogDebug($"TryEndHold ignoré ({source}) -> pointerId={pointerId}, tenu par {heldPointerId}");
            return false;
        }

        StopActiveBeam($"TryEndHold:{source}");

        held = false;
        heldPointerId = -1;
        aimYawVelocity = 0f;

        if (textExplode) textExplode.SetActive(false);
        LogDebug($"Hold terminé via {source} -> pointerId={pointerId}");
        return true;
    }

    internal bool TryBeginHoldFromAimPad(AimPad3D pad, int pointerId)
    {
        return TryStartHold(pointerId, $"aimPad:{NameOrNone(pad)}");
    }

    internal void ReleaseHoldFromAimPad(AimPad3D pad, int pointerId)
    {
        TryEndHold(pointerId, $"aimPad:{NameOrNone(pad)}");
    }

    void EnsureVisibleOnCamera()
    {
        if (!ensureVisibleOnCamera) return;
        if (!gameObject.scene.IsValid()) return;

        var cam = Camera.main;
        if (!cam)
        {
            LogDebug("EnsureVisibleOnCamera -> aucune caméra Main trouvée.");
            return;
        }

        float minY = Mathf.Clamp01(viewportMinY);
        float maxY = Mathf.Clamp01(viewportMaxY);
        if (maxY <= minY)
        {
            maxY = Mathf.Clamp01(minY + 0.05f);
        }

        float padding = Mathf.Clamp(viewportPadding, 0f, 0.45f);

        Vector3 baseViewport = cam.WorldToViewportPoint(startPos);
        if (baseViewport.z <= 0f) return;
        float distance = baseViewport.z;

        float ViewportToWorldY(float v)
        {
            var world = cam.ViewportToWorldPoint(new Vector3(baseViewport.x, Mathf.Clamp01(v), distance));
            return world.y;
        }

        float lowestY = startPos.y - verticalAmplitude;
        float highestY = startPos.y + verticalAmplitude;

        Vector3 lowestViewport = cam.WorldToViewportPoint(new Vector3(startPos.x, lowestY, startPos.z));
        if (lowestViewport.z > 0f && lowestViewport.y < minY)
        {
            float targetViewport = Mathf.Clamp(minY + padding, 0f, maxY - 0.01f);
            float desiredLowestY = ViewportToWorldY(targetViewport);
            float delta = desiredLowestY - lowestY;
            startPos.y += delta;
            lowestY += delta;
            highestY += delta;
        }

        Vector3 highestViewport = cam.WorldToViewportPoint(new Vector3(startPos.x, highestY, startPos.z));
        if (highestViewport.z > 0f && highestViewport.y > maxY)
        {
            float targetViewport = Mathf.Clamp(maxY - padding, minY + 0.01f, 1f);
            float desiredHighestY = ViewportToWorldY(targetViewport);
            float delta = desiredHighestY - highestY;
            startPos.y += delta;
        }

        baseViewport = cam.WorldToViewportPoint(startPos);
        distance = baseViewport.z;

        lowestViewport = cam.WorldToViewportPoint(new Vector3(startPos.x, startPos.y - verticalAmplitude, startPos.z));
        if (lowestViewport.z > 0f && lowestViewport.y < minY)
        {
            float targetViewport = Mathf.Clamp(minY + padding, 0f, maxY);
            float allowedY = ViewportToWorldY(targetViewport);
            verticalAmplitude = Mathf.Max(0f, startPos.y - allowedY);
        }

        highestViewport = cam.WorldToViewportPoint(new Vector3(startPos.x, startPos.y + verticalAmplitude, startPos.z));
        if (highestViewport.z > 0f && highestViewport.y > maxY)
        {
            float targetViewport = Mathf.Clamp(maxY - padding, minY, 1f);
            float allowedY = ViewportToWorldY(targetViewport);
            verticalAmplitude = Mathf.Max(0f, allowedY - startPos.y);
        }

        transform.position = new Vector3(x, startPos.y, startPos.z);
    }

    void AnimateVisuals()
    {
        float dt = Time.deltaTime;

        if (laserFan)
        {
            fanSpinAngle = Mathf.Repeat(fanSpinAngle + fanSpinSpeed * dt, 360f);
            laserFan.localRotation = laserFanBaseLocalRotation * Quaternion.AngleAxis(fanSpinAngle, Vector3.forward);
        }

        if (!laserVisualRoot) return;
    }

    // ---------- Visée via la zone dédiée ----------
    void UpdateAimFromPad()
    {
        if (!aimPad || !aimPad.IsActive) return;

        var cam = Camera.main; if (!cam) return;

        Vector2 screenPos = aimPad.ScreenPosition;
        Ray ray = cam.ScreenPointToRay(screenPos);

        var plane = new Plane(Vector3.up, new Vector3(0f, aimYLevel, 0f));
        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 hit = ray.GetPoint(dist);
        Vector3 center = new Vector3(cannonPivot.position.x, aimYLevel, cannonPivot.position.z);

        Vector3 v = hit - center; v.y = 0f;
        if (v.sqrMagnitude < 1e-6f) return;

        // Yaw voulu en monde
        float targetYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;

        // Clamp autour de la référence centrale (baseYawWorld)
        float delta = Mathf.DeltaAngle(baseYawWorld, targetYaw);
        float finalYaw = baseYawWorld + Mathf.Clamp(delta, -yawLimit, +yawLimit); // [-limit, +limit]

        float smoothTime = aimSmoothing <= 0f ? 0f : 1f / aimSmoothing;
        float currentYaw = cannonPivot.eulerAngles.y;
        float smoothedYaw = smoothTime <= 0f
            ? finalYaw
            : Mathf.SmoothDampAngle(currentYaw, finalYaw, ref aimYawVelocity, smoothTime);

        cannonPivot.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
    }

    // ---------- Tir (appelé par le texte via FireOnTouchDown3D) ----------
    public void Fire()
    {
        LogDebug("Fire() appelé.");
        AutoAssignReferences(includeInactive: false);
        EnsureWagonReferences();

        if (!laserPrefab)
        {
            LogWarning("Aucun prefab de laser n'est assigné.");
            return;
        }

        Transform pivot = cannonPivot ? cannonPivot : transform;
        if (!pivot)
        {
            LogWarning("Aucun pivot de canon trouvé.");
            return;
        }

        Transform muzzleTransform = muzzle ? muzzle : pivot;
        if (!muzzle)
        {
            LogWarning("Aucun muzzle assigné. Utilisation du pivot comme origine.");
        }

        if (textExplode) textExplode.SetActive(false);
        lastWagonHitTime = Time.time - Mathf.Max(0f, wagonHitCooldown);

        // si un laser existe déjà, on le redémarre proprement
        StopActiveBeam("fire-restart");

        activeBeam = Instantiate(laserPrefab);
        LogDebug($"Fire() -> laser instancié '{laserPrefab.name}' (pivot={NameOrNone(pivot)}, muzzle={NameOrNone(muzzleTransform)})");
        activeBeam.Begin(
            muzzleTransform,
            pivot,
            fireRange,
            fireDuration,
            shouldStop: () => !held,  // arrête immédiatement si le doigt #1 se lève
            onHit: HandleLaserRayHit
        );
    }

    void HandleLaserRayHit(RaycastHit hit)
    {
        if (!isActiveAndEnabled)
            return;

        if (!IsWagonCollider(hit.collider))
            return;

        float cooldown = Mathf.Max(0f, wagonHitCooldown);
        if (Time.time - lastWagonHitTime < cooldown)
            return;

        lastWagonHitTime = Time.time;
        EnsureWagonReferences();

        if (!wagonReaction)
        {
            LogWarning("HandleLaserRayHit -> aucune WagonLaserReaction assignée.");
            return;
        }

        LogDebug($"HandleLaserRayHit -> wagon touché (collider={NameOrNone(hit.collider)}, point={hit.point})");
        wagonReaction.HandleLaserHit();
        StopLaserAfterImpact(hit);
    }

    void StopLaserAfterImpact(RaycastHit hit)
    {
        LogDebug($"StopLaserAfterImpact -> arrêt du tir (point={hit.point})");

        bool holdReleased = false;
        if (held)
            holdReleased = TryEndHold(heldPointerId, "wagonHit");

        if (!holdReleased)
            StopActiveBeam("wagonHit");
    }

    bool IsWagonCollider(Collider other)
    {
        if (!other)
            return false;

        if (wagonCollider)
        {
            Transform root = wagonCollider.transform;
            Transform hitTransform = other.transform;
            if (other == wagonCollider || hitTransform == root || hitTransform.IsChildOf(root))
                return true;
        }

        if (wagonReaction)
        {
            Transform reactionRoot = wagonReaction.transform;
            Transform hitTransform = other.transform;
            if (hitTransform == reactionRoot || hitTransform.IsChildOf(reactionRoot))
                return true;
        }

        return false;
    }

    void StopActiveBeam(string reason)
    {
        if (!activeBeam)
            return;

        var beam = activeBeam;
        activeBeam = null;
        beam.StopNow();
        LogDebug($"StopActiveBeam -> laser interrompu ({reason})");
    }

    void EnsureWagonReferences(bool includeInactive = false)
    {
        if (!wagonReaction && wagonCollider)
        {
            var candidate = wagonCollider.GetComponentInParent<WagonLaserReaction>();
            if (candidate)
            {
                wagonReaction = candidate;
                LogDebug($"AutoAssign -> wagonReaction assigné ({NameOrNone(wagonReaction)})");
            }
        }

        if (!wagonCollider && wagonReaction)
        {
            var direct = wagonReaction.GetComponent<Collider>();
            if (!direct)
                direct = wagonReaction.GetComponentInChildren<Collider>(includeInactive);
            if (direct)
            {
                wagonCollider = direct;
                LogDebug($"AutoAssign -> wagonCollider assigné ({NameOrNone(wagonCollider)})");
            }
        }
    }

    void Reset()
    {
        AutoAssignReferences(includeInactive: true);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoAssignReferences(includeInactive: true);
            EnsureVisibleOnCamera();
        }
    }
#endif

    void AutoAssignReferences(bool includeInactive)
    {
        LogDebug($"AutoAssignReferences(includeInactive={includeInactive}) -> start (cannonPivot={NameOrNone(cannonPivot)}, muzzle={NameOrNone(muzzle)}, textExplode={NameOrNone(textExplode)}, aimPad={NameOrNone(aimPad)}, laserVisualRoot={NameOrNone(laserVisualRoot)}, laserFan={NameOrNone(laserFan)}, wagonCollider={NameOrNone(wagonCollider)}, wagonReaction={NameOrNone(wagonReaction)})");

        Transform tangibleRoot = FindChildContaining(transform, "tangible", includeInactive);
        Transform searchRoot = tangibleRoot ? tangibleRoot : transform;
        LogDebug($"AutoAssign -> tangibleRoot={NameOrNone(tangibleRoot)}, searchRoot={NameOrNone(searchRoot)}");

        if (!cannonPivot || cannonPivot == transform)
        {
            var before = cannonPivot;
            Transform candidate = FindChildContaining(searchRoot, "pivot", includeInactive);
            if (!candidate && searchRoot != transform)
                candidate = FindChildContaining(transform, "pivot", includeInactive);

            if (!candidate)
                candidate = FindChildContaining(searchRoot, "laser", includeInactive);

            if (candidate)
            {
                cannonPivot = candidate;
                if (before != cannonPivot)
                    LogDebug($"AutoAssign -> cannonPivot assigné à {NameOrNone(cannonPivot)}");
            }
            else if (!cannonPivot || cannonPivot == transform)
            {
                LogWarning("AutoAssign -> aucun cannonPivot trouvé.");
            }
        }
        else
        {
            LogDebug($"AutoAssign -> cannonPivot déjà assigné ({NameOrNone(cannonPivot)})");
        }

        if (!muzzle)
        {
            var before = muzzle;
            Transform muzzleSearchRoot = cannonPivot ? cannonPivot : searchRoot;
            muzzle = FindChildContaining(muzzleSearchRoot, "muzzle", includeInactive);
            if (!muzzle)
                muzzle = FindChildContaining(muzzleSearchRoot, "laser", includeInactive);
            if (!muzzle)
                muzzle = FindFirstRendererLeaf(muzzleSearchRoot, includeInactive);
            if (!muzzle && muzzleSearchRoot != transform)
                muzzle = FindFirstRendererLeaf(transform, includeInactive);
            if (muzzle && before != muzzle)
                LogDebug($"AutoAssign -> muzzle assigné à {NameOrNone(muzzle)}");
            if (!muzzle)
                LogWarning("AutoAssign -> aucun muzzle trouvé.");
        }
        else
        {
            LogDebug($"AutoAssign -> muzzle déjà assigné ({NameOrNone(muzzle)})");
        }

        if (!textExplode)
        {
            var fire = searchRoot.GetComponentInChildren<FireOnTouchDown3D>(includeInactive);
            if (!fire)
                fire = GetComponentInChildren<FireOnTouchDown3D>(includeInactive);

            if (fire)
            {
                textExplode = fire.gameObject;
                if (!fire.owner) fire.owner = this;
                LogDebug($"AutoAssign -> textExplode assigné à {textExplode.name} (owner mis à jour: {fire.owner == this})");
            }
            else
            {
                LogWarning("AutoAssign -> aucun FireOnTouchDown3D trouvé pour textExplode.");
            }
        }
        else
        {
            var fire = textExplode.GetComponent<FireOnTouchDown3D>();
            if (fire && !fire.owner)
            {
                fire.owner = this;
                LogDebug("AutoAssign -> owner de textExplode mis à jour.");
            }
        }

        if (!aimPad)
        {
            aimPad = GetComponentInChildren<AimPad3D>(includeInactive);
            if (aimPad)
                LogDebug($"AutoAssign -> aimPad assigné à {NameOrNone(aimPad)}");
            else
                LogWarning("AutoAssign -> aucun AimPad3D trouvé.");
        }
        else
        {
            LogDebug($"AutoAssign -> aimPad déjà assigné ({NameOrNone(aimPad)})");
        }

        if (!laserVisualRoot)
        {
            Transform visualSearchRoot = cannonPivot ? cannonPivot : searchRoot;
            Transform candidate = FindChildContaining(visualSearchRoot, "Laser_weapon", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(visualSearchRoot, "laser weapon", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(visualSearchRoot, "laserweapon", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(visualSearchRoot, "capsule", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(visualSearchRoot, "body", includeInactive);
            if (!candidate)
                candidate = FindFirstRendererLeaf(visualSearchRoot, includeInactive);
            if (!candidate && visualSearchRoot != transform)
                candidate = FindFirstRendererLeaf(transform, includeInactive);
            if (candidate == laserFan)
                candidate = null;

            if (candidate)
            {
                laserVisualRoot = candidate;
                LogDebug($"AutoAssign -> laserVisualRoot assigné à {NameOrNone(laserVisualRoot)}");
            }
            else
            {
                LogWarning("AutoAssign -> aucun laserVisualRoot trouvé.");
            }
        }
        else
        {
            LogDebug($"AutoAssign -> laserVisualRoot déjà assigné ({NameOrNone(laserVisualRoot)})");
        }

        if (!laserFan)
        {
            Transform fanSearchRoot = cannonPivot ? cannonPivot : searchRoot;
            Transform candidate = FindChildContaining(fanSearchRoot, "fan", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(fanSearchRoot, "propeller", includeInactive);
            if (!candidate)
                candidate = FindChildContaining(fanSearchRoot, "cube", includeInactive);
            if (candidate == laserVisualRoot)
                candidate = null;

            if (candidate)
            {
                laserFan = candidate;
                LogDebug($"AutoAssign -> laserFan assigné à {NameOrNone(laserFan)}");
            }
            else
            {
                LogWarning("AutoAssign -> aucun laserFan trouvé.");
            }
        }
        else
        {
            LogDebug($"AutoAssign -> laserFan déjà assigné ({NameOrNone(laserFan)})");
        }

        if (aimPad && aimPad.owner != this)
        {
            aimPad.owner = this;
            LogDebug($"AutoAssign -> aimPad.owner assigné à {NameOrNone(aimPad)}");
        }

        EnsureWagonReferences(includeInactive);

        bool pivotAdjusted = EnsureLaserYawPivot(includeInactive);

        LogDebug($"AutoAssign -> résultat final (cannonPivot={NameOrNone(cannonPivot)}, muzzle={NameOrNone(muzzle)}, textExplode={NameOrNone(textExplode)}, aimPad={NameOrNone(aimPad)}, laserVisualRoot={NameOrNone(laserVisualRoot)}, laserFan={NameOrNone(laserFan)}, yawPivot={NameOrNone(laserYawPivot)}, wagonCollider={NameOrNone(wagonCollider)}, wagonReaction={NameOrNone(wagonReaction)})");
        UpdateAnimationDefaultsIfNeeded(force: pivotAdjusted);
    }

    void UpdateAnimationDefaultsIfNeeded(bool force = false)
    {
        if (force || laserVisualRoot != cachedLaserVisualRoot)
        {
            cachedLaserVisualRoot = laserVisualRoot;
            laserVisualBaseLocalRotation = laserVisualRoot ? laserVisualRoot.localRotation : Quaternion.identity;
        }

        if (force || laserFan != cachedLaserFan)
        {
            cachedLaserFan = laserFan;
            laserFanBaseLocalRotation = laserFan ? laserFan.localRotation : Quaternion.identity;
            fanSpinAngle = 0f;
        }
    }

    bool EnsureLaserYawPivot(bool includeInactive)
    {
        if (!laserVisualRoot)
            return false;

        Transform currentParent = laserVisualRoot.parent;
        var comparison = StringComparison.OrdinalIgnoreCase;

        if (currentParent && currentParent != transform)
        {
            bool parentLooksLikePivot = currentParent == laserYawPivot ||
                                        currentParent.name.IndexOf("pivot", comparison) >= 0;
            if (parentLooksLikePivot)
            {
                laserYawPivot = currentParent;
                if (cannonPivot != laserYawPivot)
                {
                    cannonPivot = laserYawPivot;
                    LogDebug($"AutoAssign -> cannonPivot aligné sur {NameOrNone(laserYawPivot)} (existant).");
                }
                return false;
            }
        }

        if (!Application.isPlaying)
        {
            if (!cannonPivot || cannonPivot == transform)
            {
                Transform cylinder = FindChildContaining(laserVisualRoot, "cylinder.005", includeInactive);
                if (!cylinder)
                    cylinder = FindChildContaining(laserVisualRoot, "cylinder", includeInactive);

                cannonPivot = cylinder ? cylinder : (currentParent ? currentParent : laserVisualRoot);
                LogDebug($"AutoAssign -> cannonPivot assigné (éditeur) à {NameOrNone(cannonPivot)}.");
            }
            return false;
        }

        Transform pivotParent = currentParent ? currentParent : transform;

        if (!laserYawPivot)
        {
            var pivotGO = new GameObject($"{laserVisualRoot.name}_YawPivot");
            pivotGO.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            laserYawPivot = pivotGO.transform;
        }

        Transform pivotMarker = aimPivotMarker ? aimPivotMarker : FindChildContaining(laserVisualRoot, "cylinder.005", includeInactive);
        if (!pivotMarker)
            pivotMarker = FindChildContaining(laserVisualRoot, "cylinder", includeInactive);

        Vector3 pivotPosition = DetermineLaserPivotWorldPosition(includeInactive);
        Quaternion pivotRotation = pivotMarker ? pivotMarker.rotation :
                                   (laserFan ? laserFan.rotation : laserVisualRoot.rotation);

        laserYawPivot.SetParent(pivotParent, worldPositionStays: false);
        laserYawPivot.position = pivotPosition;
        laserYawPivot.rotation = pivotRotation;

        laserYawPivot.localScale = Vector3.one;

        if (laserVisualRoot.parent != laserYawPivot)
            laserVisualRoot.SetParent(laserYawPivot, worldPositionStays: true);

        cannonPivot = laserYawPivot;
        LogDebug($"AutoAssign -> yaw pivot positionné à {pivotPosition}");
        return true;
    }

    Vector3 DetermineLaserPivotWorldPosition(bool includeInactive)
    {
        if (laserFan)
            return laserFan.position;

        Transform cylinder = FindChildContaining(laserVisualRoot, "Cylinder.005", includeInactive);
        if (!cylinder)
            cylinder = FindChildContaining(laserVisualRoot, "cylinder", includeInactive);
        if (cylinder)
            return cylinder.position;

        if (laserVisualRoot)
        {
            var renderers = laserVisualRoot.GetComponentsInChildren<Renderer>(includeInactive);
            if (renderers != null && renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                return bounds.center;
            }
            return laserVisualRoot.position;
        }

        return transform.position;
    }

    Transform FindFirstRendererLeaf(Transform root, bool includeInactive)
    {
        if (!root) return null;

        var comparison = StringComparison.OrdinalIgnoreCase;
        var nodes = root.GetComponentsInChildren<Transform>(includeInactive);
        Transform fallback = null;

        foreach (var node in nodes)
        {
            if (node == root) continue;

            bool hasRenderer = node.GetComponent<MeshRenderer>() || node.GetComponent<SkinnedMeshRenderer>();
            if (!hasRenderer) continue;

            if (node.name.IndexOf("sphere", comparison) >= 0 || node.childCount == 0)
                return node;

            if (fallback == null)
                fallback = node;
        }

        return fallback;
    }

    Transform FindChildContaining(Transform root, string token, bool includeInactive)
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
    // --- util ---
    static float YawOf(Transform t)
    {
        Vector3 f = t.forward; f.y = 0f;
        if (f.sqrMagnitude < 1e-6f) return t.rotation.eulerAngles.y;
        return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }

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

    static string NameOrNone(UnityEngine.Object obj)
    {
        return obj ? obj.name : "null";
    }
}
