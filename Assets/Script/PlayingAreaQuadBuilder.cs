using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using UITouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhaseNew = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class PlayingAreaQuadBuilder : MonoBehaviour
{
    public RectTransform area;
    public Color fillColor = new Color(0f, 0.6f, 1f, 0.35f);

    private Canvas canvas; private Camera uiCam;
    private readonly List<int> touchIds = new List<int>(4);
    private RectTransform polyRect; private FilledPolygonGraphic polyGraphic;

    private Vector2 lastCenterLocal; private bool hasValidQuad = false;

    void Awake()
    {
        if (!area) area = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        var img = GetComponent<Image>();
        if (img) { img.raycastTarget = true; if (img.color.a > 0.001f) img.color = new Color(img.color.r, img.color.g, img.color.b, 0f); }
    }

    void OnEnable(){ EnhancedTouchSupport.Enable(); TouchSimulation.Disable(); }
    void OnDisable(){ EnhancedTouchSupport.Disable(); DestroyPolygonUI(); hasValidQuad = false; }

    void Update(){ CaptureTouches(); UpdatePolygonUI(); CleanupIfFewerThanFour(); }

    public bool TryGetCenterLocal(out Vector2 centerLocal){ centerLocal = lastCenterLocal; return hasValidQuad; }

    private void CaptureTouches()
    {
        foreach (var t in UITouch.activeTouches)
        {
            if (t.phase != TouchPhaseNew.Began) continue;
            if (touchIds.Count >= 4) break;
            if (!RectTransformUtility.RectangleContainsScreenPoint(area, t.screenPosition, uiCam)) continue;
            if (!touchIds.Contains(t.touchId)) touchIds.Add(t.touchId);
        }
    }

    private bool TryGetLocalPos(int touchId, out Vector2 local)
    {
        foreach (var t in UITouch.activeTouches)
            if (t.touchId == touchId)
            { RectTransformUtility.ScreenPointToLocalPointInRectangle(area, t.screenPosition, uiCam, out local); return true; }
        local = default; return false;
    }

    private void UpdatePolygonUI()
    {
        if (touchIds.Count < 4) return;
        var pts = new List<Vector2>(4);
        for (int i = 0; i < 4; i++){ if (!TryGetLocalPos(touchIds[i], out var l)) return; pts.Add(l); }

        OrderPointsByAngle(ref pts, out _);
        lastCenterLocal = QuadCenterByDiagonals(pts);
        hasValidQuad = true;

        if (polyGraphic == null) CreatePolygonUIInstance();
        polyGraphic.color = fillColor; polyGraphic.SetPoints(pts);
    }

    private void CleanupIfFewerThanFour()
    {
        for (int i = touchIds.Count - 1; i >= 0; i--)
        {
            bool alive = false; foreach (var t in UITouch.activeTouches) if (t.touchId == touchIds[i]) { alive = true; break; }
            if (!alive) touchIds.RemoveAt(i);
        }
        if (touchIds.Count < 4){ DestroyPolygonUI(); hasValidQuad = false; }
    }

    private void CreatePolygonUIInstance()
    {
        var go = new GameObject("FilledQuad", typeof(RectTransform), typeof(FilledPolygonGraphic));
        go.transform.SetParent(area, false);
        polyRect = go.GetComponent<RectTransform>();
        polyRect.anchorMin = Vector2.zero; polyRect.anchorMax = Vector2.one;
        polyRect.offsetMin = Vector2.zero; polyRect.offsetMax = Vector2.zero; polyRect.pivot = new Vector2(0.5f, 0.5f);
        polyGraphic = go.GetComponent<FilledPolygonGraphic>(); polyGraphic.color = fillColor;
    }

    private void DestroyPolygonUI(){ if (polyRect){ Destroy(polyRect.gameObject); polyRect=null; polyGraphic=null; } }

    private static void OrderPointsByAngle(ref List<Vector2> pts, out Vector2 centroid)
    {
        centroid = Vector2.zero; 
        for (int i = 0; i < pts.Count; i++) centroid += pts[i]; 
        centroid /= Mathf.Max(pts.Count, 1);
        var centroidx = centroid.x;
        var centroidy = centroid.y;
        pts.Sort((a, b) =>
        {
            float angA = Mathf.Atan2(a.y - centroidy, a.x - centroidx); 
            float angB = Mathf.Atan2(b.y - centroidy, b.x - centroidx); return angA.CompareTo(angB);
        });
    }

    private static bool LineIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 p)
    {
        Vector2 r = b - a, s = d - c; float rxs = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(rxs) < 1e-6f){ p = 0.25f * (a + b + c + d); return false; }
        Vector2 cma = c - a; float t = (cma.x * s.y - cma.y * s.x) / rxs; p = a + t * r; return true;
    }

    private static Vector2 QuadCenterByDiagonals(IList<Vector2> pts)
    {
        if (pts == null || pts.Count < 4) return Vector2.zero;
        return LineIntersection(pts[0], pts[2], pts[1], pts[3], out var p) ? p : 0.25f * (pts[0] + pts[1] + pts[2] + pts[3]);
    }
    
    public bool HasValidQuad()
    {
        return touchIds.Count >= 4; // vrai tant que les 4 doigts sont posés
    }
}
