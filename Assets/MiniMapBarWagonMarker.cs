using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MiniMapBarWagonMarker : MonoBehaviour
{
    public enum MappingMode
    {
        Segment3D,   // projection sur le segment worldStart -> worldEnd
        WorldY       // simple InverseLerp sur les Y monde
    }

    [Header("Références monde")]
    public Transform worldStart;        // bas
    public Transform worldEnd;          // haut
    public Transform chariot;           // le wagon

    [Header("UI")]
    public RectTransform bar;           // la barre (ce GO)
    public RectTransform marker;        // l’icône/trait à déplacer

    [Header("Options")]
    public MappingMode mode = MappingMode.Segment3D;
    [Tooltip("Coche si tu veux 0% en haut et 100% en bas (ou l’inverse).")]
    public bool invert = false;
    [Range(0f, 0.99f)] public float smooth = 0.2f; // 0 = sans lissage

    [Header("Debug")]
    [Range(0f,1f)] public float t;      // 0=bas → 1=haut
    public bool verbose;

    void Reset()
    {
        bar = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        if (!worldStart || !worldEnd || !chariot || !bar || !marker) return;

        float rawT;
        if (mode == MappingMode.Segment3D)
        {
            Vector3 a = worldStart.position;
            Vector3 b = worldEnd.position;
            Vector3 p = chariot.position;
            Vector3 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom < 1e-6f) return;
            rawT = Mathf.Clamp01(Vector3.Dot(p - a, ab) / denom);
        }
        else // WorldY
        {
            rawT = Mathf.InverseLerp(worldStart.position.y, worldEnd.position.y, chariot.position.y);
        }

        if (invert) rawT = 1f - rawT;
        t = (smooth > 0f) ? Mathf.Lerp(t, rawT, 1f - smooth) : rawT;

        // --- Pivot/anchor-proof mapping ---
        // Get the bar’s local rect bounds
        var r = bar.rect;           // local space rect
        float yMin = r.yMin;        // bottom in local space
        float yMax = r.yMax;        // top in local space

        // Lerp directly in local space regardless of pivot
        float localY = Mathf.Lerp(yMin, yMax, t);

        // Keep current local X
        var ap = marker.anchoredPosition;
        marker.anchoredPosition = new Vector2(ap.x, localY);

        if (verbose) Debug.Log($"[WagonMarker] rawT={rawT:F3}  t={t:F3}  y=[{yMin:F1}->{yMax:F1}] -> {localY:F1}");
    }
}
