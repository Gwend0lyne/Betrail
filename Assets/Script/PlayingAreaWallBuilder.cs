using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using UITouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhaseNew = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class PlayingAreaWallBuilder : MonoBehaviour
{
    [Header("Wall")]
    public RectTransform wallPrefab;  // Prefab UI du mur
    public float wallThickness = 10f; // Épaisseur du mur

    [Header("World Wall")]
    [Tooltip("Prefab 3D instancié dans la scène pour bloquer physiquement le laser (doit inclure LaserBlocker + Collider).")]
    public GameObject worldWallPrefab;
    [Tooltip("Parent hiérarchique pour la version 3D du mur (facultatif).")]
    public Transform worldWallParent;
    [Tooltip("Hauteur (Y monde) du plan sur lequel on projette les doigts pour positionner le mur 3D.")]
    public float worldWallPlaneY = 0f;
    [Tooltip("Largeur X appliquée à l'objet 3D généré.")]
    public float worldWallThickness = 0.5f;
    [Tooltip("Taille verticale Y appliquée à l'objet 3D généré.")]
    public float worldWallHeight = 3f;
    [Tooltip("Décalage additionnel appliqué à l'objet 3D.")]
    public Vector3 worldWallOffset = Vector3.zero;

    private RectTransform area;
    private Canvas canvas;
    private Camera uiCam;
    private RectTransform currentWall;
    private GameObject currentWorldWall;
    private bool invalidParentWarningLogged;

    // Gestion multitouch séquentielle
    private int firstTouchId = -1;
    private int secondTouchId = -1;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Disable(); // Empêche la souris d'être vue comme un "touch"
    }

    void OnDisable() => EnhancedTouchSupport.Disable();

    void Awake()
    {
        area = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        // S’assurer que la zone capte les touches
        var img = GetComponent<Image>();
        if (img) img.raycastTarget = true;
    }

    void Update()
    {
        HandleTouchesSequential();
        UpdateWallWhileBothTouchesHeld();
    }

    // ----- Logique tactile -----
    private void HandleTouchesSequential()
    {
        // 1) Premier doigt
        if (firstTouchId == -1)
        {
            foreach (var t in UITouch.activeTouches)
            {
                if (t.phase != TouchPhaseNew.Began) continue;
                if (!IsInArea(t.screenPosition)) continue;

                firstTouchId = t.touchId;
                break;
            }
        }

        // 2) Deuxième doigt
        if (firstTouchId != -1 && secondTouchId == -1)
        {
            foreach (var t in UITouch.activeTouches)
            {
                if (t.phase != TouchPhaseNew.Began) continue;
                if (t.touchId == firstTouchId) continue;
                if (!IsInArea(t.screenPosition)) continue;

                secondTouchId = t.touchId;

                Vector2 aLocal = ScreenToLocal(GetTouchScreenPos(firstTouchId));
                Vector2 bLocal = ScreenToLocal(GetTouchScreenPos(secondTouchId));
                CreateOrReplaceWall(aLocal, bLocal);
                break;
            }
        }

        // 3) Supprimer si un des doigts est levé
        if (currentWall != null)
        {
            bool aAlive = IsTouchAlive(firstTouchId);
            bool bAlive = IsTouchAlive(secondTouchId);
            if (!aAlive || !bAlive)
            {
                ClearCurrentWalls();
                firstTouchId = -1;
                secondTouchId = -1;
            }
        }
    }

    private void UpdateWallWhileBothTouchesHeld()
    {
        if (currentWall == null) return;
        if (!IsTouchAlive(firstTouchId) || !IsTouchAlive(secondTouchId)) return;

        Vector2 aLocal = ScreenToLocal(GetTouchScreenPos(firstTouchId));
        Vector2 bLocal = ScreenToLocal(GetTouchScreenPos(secondTouchId));
        ApplyWallGeometry(currentWall, aLocal, bLocal, wallThickness);
        ApplyWorldWallGeometry(aLocal, bLocal);
    }

    // ----- Création / MàJ -----
    private void CreateOrReplaceWall(Vector2 aLocal, Vector2 bLocal)
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("⚠️ Wall Prefab non assigné !");
            return;
        }

        ClearCurrentWalls();

        currentWall = Instantiate(wallPrefab, area);
        NormalizeWallRect(currentWall);
        ApplyWallGeometry(currentWall, aLocal, bLocal, wallThickness);

        var img = currentWall.GetComponent<Image>();
        if (img) img.raycastTarget = true;

        CreateWorldWall();
        ApplyWorldWallGeometry(aLocal, bLocal);
    }

    private void ApplyWallGeometry(RectTransform wall, Vector2 aLocal, Vector2 bLocal, float thickness)
    {
        Vector2 mid = (aLocal + bLocal) * 0.5f;
        Vector2 delta = bLocal - aLocal;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        wall.anchoredPosition = mid;
        wall.localRotation = Quaternion.Euler(0, 0, angle);
        wall.sizeDelta = new Vector2(length, thickness);
    }

    private void CreateWorldWall()
    {
        if (!worldWallPrefab)
            return;

        currentWorldWall = Instantiate(worldWallPrefab);
        Transform parent = GetRuntimeWorldWallParent();
        if (parent)
            currentWorldWall.transform.SetParent(parent, false);
        currentWorldWall.SetActive(true);
    }

    private void ApplyWorldWallGeometry(Vector2 aLocal, Vector2 bLocal)
    {
        if (!currentWorldWall)
            return;

        if (!TryLocalToWorld(aLocal, out var aWorld) ||
            !TryLocalToWorld(bLocal, out var bWorld))
        {
            currentWorldWall.SetActive(false);
            return;
        }

        Vector3 delta = bWorld - aWorld;
        delta.y = 0f;

        float length = delta.magnitude;
        if (length < 0.05f)
        {
            currentWorldWall.SetActive(false);
            return;
        }

        currentWorldWall.SetActive(true);

        Vector3 mid = (aWorld + bWorld) * 0.5f;
        Quaternion rotation = delta.sqrMagnitude > 1e-6f
            ? Quaternion.LookRotation(delta.normalized, Vector3.up)
            : Quaternion.identity;

        Vector3 scale = currentWorldWall.transform.localScale;
        if (worldWallThickness > 0f)
            scale.x = worldWallThickness;
        if (worldWallHeight > 0f)
            scale.y = worldWallHeight;
        scale.z = length;

        currentWorldWall.transform.SetPositionAndRotation(
            mid + worldWallOffset + Vector3.up * (worldWallHeight > 0f ? worldWallHeight * 0.5f : 0f),
            rotation);
        currentWorldWall.transform.localScale = scale;
    }

    private bool TryLocalToWorld(Vector2 local, out Vector3 world)
    {
        world = Vector3.zero;
        if (!area)
            return false;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, area.TransformPoint(local));
        Camera cam = uiCam ? uiCam : Camera.main;
        if (!cam)
            return false;

        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, worldWallPlaneY, 0f));
        if (!plane.Raycast(ray, out float dist))
            return false;

        world = ray.GetPoint(dist);
        world.y = worldWallPlaneY;
        return true;
    }

    private Transform GetRuntimeWorldWallParent()
    {
        if (!worldWallParent)
            return null;

        if (worldWallParent.gameObject.scene.IsValid())
            return worldWallParent;

        if (!invalidParentWarningLogged)
        {
            Debug.LogWarning("[PlayingAreaWallBuilder] worldWallParent doit appartenir à la scène. Parent ignoré.", this);
            invalidParentWarningLogged = true;
        }
        return null;
    }

    private void ClearCurrentWalls()
    {
        if (currentWall)
        {
            Destroy(currentWall.gameObject);
            currentWall = null;
        }

        if (currentWorldWall)
        {
            Destroy(currentWorldWall);
            currentWorldWall = null;
        }
    }

    // ----- Utilitaires -----
    private bool IsInArea(Vector2 screenPos) =>
        RectTransformUtility.RectangleContainsScreenPoint(area, screenPos, uiCam);

    private Vector2 ScreenToLocal(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, uiCam, out var local);
        return local;
    }

    private Vector2 GetTouchScreenPos(int touchId)
    {
        foreach (var t in UITouch.activeTouches)
            if (t.touchId == touchId) return t.screenPosition;
        return Vector2.zero;
    }

    private bool IsTouchAlive(int touchId)
    {
        if (touchId == -1) return false;
        foreach (var t in UITouch.activeTouches)
            if (t.touchId == touchId) return true;
        return false;
    }

    private void NormalizeWallRect(RectTransform wall)
    {
        wall.SetParent(area, false);
        wall.anchorMin = wall.anchorMax = new Vector2(0.5f, 0.5f);
        wall.pivot = new Vector2(0.5f, 0.5f);
        wall.localScale = Vector3.one;
    }
}
