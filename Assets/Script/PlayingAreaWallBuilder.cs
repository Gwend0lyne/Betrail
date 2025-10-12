using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// New Input System
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UITouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhaseNew = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(RectTransform))]
public class PlayingAreaWallBuilder : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Wall")]
    public RectTransform wallPrefab;
    public float wallThickness = 10f;

    [Header("Cheat (souris)")]
    public bool cheatArmed = false;             // capture 2 clics
    public KeyCode toggleCheatKey = KeyCode.C;  // touche pour armer/désarmer

    private RectTransform area;
    private Canvas canvas;
    private Camera uiCam;

    // Mur unique
    private RectTransform currentWall;

    // ---- Souris (cheat) ----
    private int mouseOwnerPointerId = int.MinValue; // pointerId du 2e clic
    private bool mouseDragActive = false;
    private readonly List<Vector2> cheatClicks = new List<Vector2>(2);

    // ---- Tactile séquentiel ----
    private int firstTouchId  = -1;   // -1 = aucun
    private int secondTouchId = -1;
    private bool wallFromTouch = false;

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Awake()
    {
        area   = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        uiCam  = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
               ? canvas.worldCamera : null;
    }

    void Update()
    {
        // Toggle cheat
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            cheatArmed = !cheatArmed;
            cheatClicks.Clear();
            Debug.Log(cheatArmed ? "Cheat ARMÉ : cliquez 2 fois dans la zone." : "Cheat DÉSARMÉ.");
        }

        HandleTouchesSequential();
        UpdateWallWhileBothTouchesHeld();
    }

    // ================== TACTILE : séquentiel ==================
    private void HandleTouchesSequential()
    {
        // 1) Enregistrer un premier doigt s'il commence dans la zone (si pas déjà pris)
        foreach (var t in UITouch.activeTouches)
        {
            if (t.phase == TouchPhaseNew.Began && firstTouchId == -1)
            {
                if (IsInArea(t.screenPosition))
                {
                    firstTouchId = t.touchId;
                    // On attend le second plus tard
                    break;
                }
            }
        }

        // 2) Enregistrer un second doigt quand il arrive (si on a déjà le premier)
        if (firstTouchId != -1 && secondTouchId == -1)
        {
            foreach (var t in UITouch.activeTouches)
            {
                if (t.phase == TouchPhaseNew.Began && t.touchId != firstTouchId)
                {
                    if (IsInArea(t.screenPosition))
                    {
                        secondTouchId = t.touchId;

                        // Créer le mur entre les deux positions actuelles
                        var aLocal = ScreenToLocal(GetTouchScreenPos(firstTouchId));
                        var bLocal = ScreenToLocal(GetTouchScreenPos(secondTouchId));
                        CreateOrReplaceWall(aLocal, bLocal);

                        wallFromTouch = true;
                        // le drag tactile se fait en bougeant les doigts (voir UpdateWallWhileBothTouchesHeld)
                        break;
                    }
                }
            }
        }

        // 3) Si le mur tactile existe, supprimer dès qu’un des deux doigts est levé
        if (wallFromTouch && currentWall != null)
        {
            bool aAlive = IsTouchAlive(firstTouchId);
            bool bAlive = IsTouchAlive(secondTouchId);
            if (!aAlive || !bAlive)
            {
                Destroy(currentWall.gameObject);
                currentWall = null;
                ResetTouchState();
                Debug.Log("Mur supprimé (un doigt levé).");
            }
        }
    }

    // Pendant que les deux doigts sont posés, le mur suit leurs positions (drag “naturel”)
    private void UpdateWallWhileBothTouchesHeld()
    {
        if (!wallFromTouch || currentWall == null) return;
        if (!IsTouchAlive(firstTouchId) || !IsTouchAlive(secondTouchId)) return;

        Vector2 aLocal = ScreenToLocal(GetTouchScreenPos(firstTouchId));
        Vector2 bLocal = ScreenToLocal(GetTouchScreenPos(secondTouchId));

        Vector2 mid = (aLocal + bLocal) * 0.5f;
        Vector2 delta = bLocal - aLocal;
        float length = delta.magnitude;
        float angle  = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        currentWall.anchoredPosition = mid;
        currentWall.sizeDelta = new Vector2(length, wallThickness);
        currentWall.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // ================== CHEAT : 2 clics souris ==================
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!cheatArmed) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var local))
            return;

        cheatClicks.Add(local);

        if (cheatClicks.Count >= 2)
        {
            mouseOwnerPointerId = eventData.pointerId; // 2e clic = propriétaire
            CreateOrReplaceWall(cheatClicks[0], cheatClicks[1]);

            cheatClicks.Clear();
            cheatArmed = false;
            wallFromTouch = false; // on est en mode souris
            Debug.Log("Cheat consommé : 2e clic maintenu = drag ; relâche = suppression.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentWall == null) return;
        if (eventData.pointerId == mouseOwnerPointerId && mouseOwnerPointerId != int.MinValue)
            mouseDragActive = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!mouseDragActive || currentWall == null) return;
        float k = (canvas != null && canvas.scaleFactor != 0f) ? canvas.scaleFactor : 1f;
        currentWall.anchoredPosition += eventData.delta / k;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentWall == null) return;
        if (mouseDragActive && eventData.pointerId == mouseOwnerPointerId && mouseOwnerPointerId != int.MinValue)
        {
            Destroy(currentWall.gameObject);
            currentWall = null;
            mouseDragActive = false;
            mouseOwnerPointerId = int.MinValue;
            Debug.Log("Mur supprimé (relâche du 2e clic souris).");
        }
    }

    // ================== Utilitaires ==================
    private void CreateOrReplaceWall(Vector2 aLocal, Vector2 bLocal)
    {
        if (wallPrefab == null) { Debug.LogWarning("Wall Prefab non assigné !"); return; }
        if (currentWall != null) Destroy(currentWall.gameObject);

        var wall = Instantiate(wallPrefab, area);
        currentWall = wall;

        Vector2 mid   = (aLocal + bLocal) * 0.5f;
        Vector2 delta = bLocal - aLocal;
        float length  = delta.magnitude;
        float angle   = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        wall.anchoredPosition = mid;
        wall.sizeDelta = new Vector2(length, wallThickness);
        wall.localRotation = Quaternion.Euler(0, 0, angle);
        wall.SetAsLastSibling();

        // Pour que le mur puisse recevoir des raycasts (utile si tu veux aussi le drag tactile directement)
        var img = wall.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        var cg = wall.GetComponent<CanvasGroup>();
        if (cg == null) cg = wall.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
    }

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

    private void ResetTouchState()
    {
        firstTouchId  = -1;
        secondTouchId = -1;
        wallFromTouch = false;
    }
}
