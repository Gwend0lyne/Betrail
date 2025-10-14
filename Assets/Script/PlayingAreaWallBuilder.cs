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

    private RectTransform area;
    private Canvas canvas;
    private Camera uiCam;
    private RectTransform currentWall;

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
                Destroy(currentWall.gameObject);
                currentWall = null;
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
    }

    // ----- Création / MàJ -----
    private void CreateOrReplaceWall(Vector2 aLocal, Vector2 bLocal)
    {
        if (wallPrefab == null)
        {
            Debug.LogWarning("⚠️ Wall Prefab non assigné !");
            return;
        }

        if (currentWall != null)
            Destroy(currentWall.gameObject);

        currentWall = Instantiate(wallPrefab, area);
        NormalizeWallRect(currentWall);
        ApplyWallGeometry(currentWall, aLocal, bLocal, wallThickness);

        var img = currentWall.GetComponent<Image>();
        if (img) img.raycastTarget = true;
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
