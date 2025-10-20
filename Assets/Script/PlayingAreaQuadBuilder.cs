using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using UITouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhaseNew = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))] // pour capter les touchs (Raycast Target ON)
public class PlayingAreaQuadBuilder : MonoBehaviour
{
    [Header("Polygon style")]
    [Tooltip("Couleur de la forme (alpha inclus).")]
    public Color fillColor = new Color(0f, 0.6f, 1f, 0.35f); // bleu transparent
    [Tooltip("Épaisseur virtuelle (pas utile ici, mais au cas où)")]
    public float dummyThickness = 1f;

    private RectTransform area;
    private Canvas canvas;
    private Camera uiCam;

    // tracking des 4 doigts (touchId EnhancedTouch)
    private readonly List<int> touchIds = new List<int>(4);

    // instance de la forme
    private RectTransform polyRect;
    private FilledPolygonGraphic polyGraphic;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Disable(); // évite que la souris soit vue comme touch
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        DestroyPolygon();
    }

    void Awake()
    {
        area = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        uiCam  = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
               ? canvas.worldCamera : null;

        // s'assurer que la zone reçoit les touchs
        var img = GetComponent<Image>();
        if (img)
        {
            img.raycastTarget = true;
            // invisible mais cliquable si tu veux
            if (img.color.a > 0.001f)
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
        }
    }

    void Update()
    {
        CaptureTouches();
        UpdatePolygon();
        CleanupIfFewerThanFour();
    }

    // 1) Enregistrer jusqu'à 4 doigts, dans l'ordre où ils arrivent
    private void CaptureTouches()
    {
        // Ajoute les nouveaux touches (Began) s'ils sont dans la zone
        foreach (var t in UITouch.activeTouches)
        {
            if (t.phase != TouchPhaseNew.Began) continue;
            if (touchIds.Count >= 4) break;
            if (!IsInArea(t.screenPosition)) continue;
            if (!touchIds.Contains(t.touchId))
                touchIds.Add(t.touchId);
        }
    }

    // 2) Mettre à jour/Créer la forme quand on a 4 doigts
    private void UpdatePolygon()
    {
        if (touchIds.Count < 4) return;

        // Récupère les positions locales des 4 doigts (si l'un a disparu, on sort)
        var pts = new List<Vector2>(4);
        for (int i = 0; i < 4; i++)
        {
            if (!TryGetLocalPos(touchIds[i], out var local))
                return; // si un doigt a disparu, on attend le cleanup
            pts.Add(local);
        }

        // Ordonne les points pour former un quad cohérent (tri par angle autour du centroïde)
        OrderPointsByAngle(ref pts);

        // Crée la forme si besoin
        if (polyGraphic == null)
            CreatePolygonInstance();

        polyGraphic.color = fillColor;
        polyGraphic.SetPoints(pts);
    }

    // 3) Détruit la forme si < 4 doigts
    private void CleanupIfFewerThanFour()
    {
        // supprime les touchId morts
        for (int i = touchIds.Count - 1; i >= 0; i--)
        {
            if (!IsTouchAlive(touchIds[i]))
                touchIds.RemoveAt(i);
        }

        if (touchIds.Count < 4)
        {
            DestroyPolygon();
        }
    }

    // ----- helpers polygon -----
    private void CreatePolygonInstance()
    {
        // Crée un GO enfant qui occupe exactement la zone
        var go = new GameObject("FilledQuad", typeof(RectTransform), typeof(FilledPolygonGraphic));
        go.transform.SetParent(area, false);

        polyRect = go.GetComponent<RectTransform>();
        // on fait un "stretch" complet pour que l'espace local corresponde à la zone
        polyRect.anchorMin = new Vector2(0f, 0f);
        polyRect.anchorMax = new Vector2(1f, 1f);
        polyRect.offsetMin = Vector2.zero;
        polyRect.offsetMax = Vector2.zero;
        polyRect.pivot     = new Vector2(0.5f, 0.5f);
        polyRect.localScale = Vector3.one;

        polyGraphic = go.GetComponent<FilledPolygonGraphic>();
        polyGraphic.color = fillColor;
    }

    private void DestroyPolygon()
    {
        if (polyRect != null)
        {
            Destroy(polyRect.gameObject);
            polyRect = null;
            polyGraphic = null;
        }
    }

    // ----- utils géométrie / input -----
    private bool TryGetLocalPos(int touchId, out Vector2 local)
    {
        foreach (var t in UITouch.activeTouches)
        {
            if (t.touchId == touchId)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, t.screenPosition, uiCam, out local);
                return true;
            }
        }
        local = default;
        return false;
    }

    private bool IsTouchAlive(int touchId)
    {
        foreach (var t in UITouch.activeTouches)
            if (t.touchId == touchId) return true;
        return false;
    }

    private bool IsInArea(Vector2 screenPos) =>
        RectTransformUtility.RectangleContainsScreenPoint(area, screenPos, uiCam);

    private static void OrderPointsByAngle(ref List<Vector2> pts)
    {
        // Centroïde
        Vector2 c = Vector2.zero;
        for (int i = 0; i < pts.Count; i++) c += pts[i];
        c /= pts.Count;

        // Tri par angle autour du centre (anti-horaire)
        pts.Sort((a, b) =>
        {
            float angA = Mathf.Atan2(a.y - c.y, a.x - c.x);
            float angB = Mathf.Atan2(b.y - c.y, b.x - c.x);
            return angA.CompareTo(angB);
        });
    }
}