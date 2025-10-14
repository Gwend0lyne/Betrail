using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Graphic UI qui remplit un polygone défini par des points locaux (dans l'espace du RectTransform).
/// Pour un quad, passe 4 points ordonnés (sens horaire/anti-horaire).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class FilledPolygonGraphic : Graphic
{
    [SerializeField] private List<Vector2> _points = new List<Vector2>(4);

    /// <summary>Définit les points et redessine.</summary>
    public void SetPoints(IList<Vector2> pts)
    {
        _points.Clear();
        if (pts != null) _points.AddRange(pts);
        SetVerticesDirty();
        SetMaterialDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_points == null || _points.Count < 3)
            return;

        // Triangulation "fan" (convexe) : v0, (i, i+1)
        // On suppose que les points sont déjà dans l'ordre (sens horaire/anti-horaire)
        int n = _points.Count;

        // Ajoute les vertices
        for (int i = 0; i < n; i++)
        {
            UIVertex v = UIVertex.simpleVert;
            v.color = color;
            v.position = _points[i];
            vh.AddVert(v);
        }

        // Triangles : (0, i, i+1)
        for (int i = 1; i < n - 1; i++)
        {
            vh.AddTriangle(0, i, i + 1);
        }
    }
}