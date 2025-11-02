using UnityEngine;

[RequireComponent(typeof(PlayingAreaQuadBuilder))]
public class UIQuadCenterToTrackMapperRelative : MonoBehaviour
{
    [Header("Référence UI")]
    public RectTransform mappingRect;   // ex: Canvas/Walls-area/Panel
    public bool mapAlongX = false;      // barre verticale -> false

    [Header("Références monde")]
    public WagonProgressProvider progress;
    public NetFollowerAlongRail follower;

    [Header("Fenêtre relative autour du wagon")]
    [Range(0.05f, 1f)] public float windowSpanT = 1f; // 1 = toute la map
    public float offsetT = 0f;
    public bool clamp01 = true;

    private PlayingAreaQuadBuilder builder;

    void Awake()
    {
        builder = GetComponent<PlayingAreaQuadBuilder>();
        if (!mappingRect) mappingRect = builder ? builder.GetComponent<RectTransform>() : null;
    }

    void LateUpdate()
    {
        if (!builder || !progress || !follower || !mappingRect) return;
        if (!builder.TryGetCenterLocal(out var centerLocalInArea)) return;

        // local(area) -> local(mappingRect) sans passer par l’écran
        Vector2 centerLocalInMapping = (Vector2)mappingRect.InverseTransformPoint(
            builder.transform.TransformPoint(centerLocalInArea)
        );

        Rect rr = mappingRect.rect;
        float uiT = mapAlongX
            ? Mathf.InverseLerp(rr.xMin, rr.xMax, centerLocalInMapping.x)
            : Mathf.InverseLerp(rr.yMin, rr.yMax, centerLocalInMapping.y);
        uiT = Mathf.Clamp01(uiT);

        float wagonT = progress.GetWagonT();

        float half = windowSpanT * 0.5f;
        float startT = Mathf.Clamp(wagonT - half, 0f, 1f - windowSpanT);
        float t = startT + uiT * windowSpanT + offsetT;
        if (clamp01) t = Mathf.Clamp01(t);

        follower.SetT(t);
        // Debug.Log($"[MapperRelatif] wagonT={wagonT:F3} uiT={uiT:F3} startT={startT:F3} t={t:F3}");
    }
}