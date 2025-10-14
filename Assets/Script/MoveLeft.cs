using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Header("Source de la vitesse")]
    public PanelDuo source; // <- glisse ici ton PanelDuo dans l’inspector

    [Header("Mapping vitesse")]
    [Tooltip("Vitesse monde à 100% (unités/s).")]
    public float maxWorldSpeed = 5f;

    [Tooltip("Courbe de réponse: x = pourcentage (0..1), y = facteur (0..1).")]
    public AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    void Update()
    {
        if (!source) return;

        // 0..1 d’après le pourcentage lissé d’aiguille (ultra fluide)
        float pct01 = Mathf.Clamp01(source.SpeedPercent / 100f);

        // Option: courbe non-linéaire (ex: ease-in pour inertie)
        float factor = response.Evaluate(pct01);

        float v = maxWorldSpeed * factor; // unités/s
        transform.position += Vector3.left * v * Time.deltaTime;
    }
}