using UnityEngine;

/// Donne la progression 0..1 du wagon le long des rails.
public class WagonProgressProvider : MonoBehaviour
{
    public Transform wagon;
    public Transform railStart;
    public Transform railEnd;

    public float GetWagonT()
    {
        if (!wagon || !railStart || !railEnd) return 0f;
        Vector3 a = railStart.position, b = railEnd.position, v = b - a;
        if (v.sqrMagnitude < 1e-6f) return 0f;
        float t = Vector3.Dot(wagon.position - a, v) / Vector3.Dot(v, v);
        return Mathf.Clamp01(t);
    }
}