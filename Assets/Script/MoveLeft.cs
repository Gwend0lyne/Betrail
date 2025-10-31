using System.Collections;
using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Header("Source de la vitesse")]
    public PanelDuo source;                        // glisse ici ton PanelDuo

    [Header("Mapping vitesse")]
    [Tooltip("Vitesse monde à 100% (unités/s).")]
    public float maxWorldSpeed = 5f;
    [Tooltip("Courbe de réponse: x = pourcentage (0..1), y = facteur (0..1).")]
    public AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Ralentissement ABSOLU (ex: -30 unités/s)")]
    [Tooltip("Offset soustrait à la vitesse (unités/s). Revient à 0 après la durée.")]
    public float slowOffset = 0f;                  // >0 = enlève des unités/s
    Coroutine slowCo;

    // ----- Tangage -----
    public enum TiltAxis { PitchX, YawY, RollZ }   // choisis l’axe qui donne “gauche↔droite”
    [Header("Tangage (visuel)")]
    public Transform tiltTarget;                   // null => transform
    public TiltAxis tiltAxis = TiltAxis.RollZ;     // par défaut: Roll Z = gauche/droite classique
    public float tiltReturnSpeed = 6f;             // retour vers la rotation d’origine
    Coroutine swayCo;
    Quaternion tiltInitialLocalRot;                // rotation locale de référence

    [Header("Vie (optionnel)")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    // Pause/Resume
    bool paused = false;
    public bool IsPaused => paused;

    /// <summary>Vitesse monde appliquée ce frame (unités/s vers la gauche).</summary>
    public float CurrentSpeed { get; private set; }

    void Awake()
    {
        if (!tiltTarget) tiltTarget = transform;
        tiltInitialLocalRot = tiltTarget.localRotation;

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    void Update()
    {
        if (!source)
        {
            CurrentSpeed = 0f;
            return;
        }

        // 0..1 depuis l’aiguille lissée
        float pct01 = Mathf.Clamp01(source.SpeedPercent / 100f);
        float baseFactor = response.Evaluate(pct01);          // 0..1
        float baseSpeed  = maxWorldSpeed * baseFactor;        // unités/s

        float v = paused ? 0f : Mathf.Max(0f, baseSpeed - slowOffset); // soustraction ABSOLUE
        CurrentSpeed = v;

        transform.position += Vector3.left * v * Time.deltaTime;

        // retour du tilt vers la rotation d’origine si pas d’oscillation en cours
        if (swayCo == null && tiltTarget)
        {
            tiltTarget.localRotation = Quaternion.Slerp(
                tiltTarget.localRotation, tiltInitialLocalRot, Time.deltaTime * tiltReturnSpeed);
        }
    }

    // ---------- API jeu ----------
    public void Pause()  => paused = true;
    public void Resume() => paused = false;

    public void ApplyDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        Debug.Log($"[MoveLeft] -{amount} HP → {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Soustrait 'amount' unités/s à la vitesse pendant 'duration' secondes.
    /// Ex: amount=30 → 40 devient 10 (sans passer sous 0).
    /// </summary>
    public void ApplySlowdown(float amount, float duration)
    {
        amount = Mathf.Max(0f, amount);
        if (slowCo != null) StopCoroutine(slowCo);
        slowCo = StartCoroutine(CoSlowdown(amount, duration));
    }

    IEnumerator CoSlowdown(float amount, float duration)
    {
        slowOffset = amount;
        yield return new WaitForSeconds(duration);
        slowOffset = 0f;
        slowCo = null;
    }

    /// <summary>Oscille doucement pendant 'duration' autour de l’axe choisi.</summary>
    public void ApplySway(float amplitudeDeg, float duration, float frequencyHz = 1.0f)
    {
        if (!tiltTarget || amplitudeDeg <= 0f || duration <= 0f) return;
        if (swayCo != null) StopCoroutine(swayCo);
        swayCo = StartCoroutine(CoSway(amplitudeDeg, duration, frequencyHz));
    }

    IEnumerator CoSway(float ampDeg, float duration, float freqHz)
    {
        float t = 0f;

        // vecteur d’axe local selon le choix
        Vector3 axis =
            tiltAxis == TiltAxis.PitchX ? Vector3.right :
            tiltAxis == TiltAxis.YawY   ? Vector3.up    :
                                          Vector3.forward;  // RollZ par défaut

        while (t < duration)
        {
            t += Time.deltaTime;
            float angle = Mathf.Sin(t * Mathf.PI * 2f * freqHz) * ampDeg;
            // rotation = rotation d’origine * rotation d’angle autour de l’axe local choisi
            tiltTarget.localRotation = tiltInitialLocalRot * Quaternion.AngleAxis(angle, axis);
            yield return null;
        }

        // fin: revient proprement à la rotation initiale
        tiltTarget.localRotation = tiltInitialLocalRot;
        swayCo = null;
    }
}
