using System.Collections;
using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Header("Source de la vitesse")]
    public PanelDuo source;

    [Header("Mapping vitesse")]
    [Tooltip("Vitesse monde à 100% (unités/s).")]
    public float maxWorldSpeed = 5f;
    [Tooltip("Courbe de réponse: x = pourcentage (0..1), y = facteur (0..1).")]
    public AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // ---- Ralentissements ----
    // 1) multiplicatif temporaire (ex: 0.5 => moitié de vitesse)
    float _factor = 1f;
    Coroutine _factorCo;

    // 2) offset absolu temporaire (enlève N unités/s)
    public float slowOffset = 0f;
    Coroutine _offsetCo;

    // ---- Tangage visuel (inchangé) ----
    public enum TiltAxis { PitchX, YawY, RollZ }
    [Header("Tangage (visuel)")]
    public Transform tiltTarget;
    public TiltAxis tiltAxis = TiltAxis.RollZ;
    public float tiltReturnSpeed = 6f;
    Coroutine swayCo;
    Quaternion tiltInitialLocalRot;

    [Header("Vie (optionnel)")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    bool paused = false;
    public bool IsPaused => paused;

    /// <summary>Vitesse monde réellement appliquée ce frame (unités/s vers la gauche).</summary>
    public float CurrentSpeed { get; private set; }

    /// <summary>Vitesse monde théorique avant effets (en fonction du panel).</summary>
    public float BaseSpeedNow
    {
        get
        {
            if (!source) return 0f;
            float pct01 = Mathf.Clamp01(source.SpeedPercent / 100f);
            return maxWorldSpeed * response.Evaluate(pct01);
        }
    }

    void Awake()
    {
        if (!tiltTarget) tiltTarget = transform;
        tiltInitialLocalRot = tiltTarget.localRotation;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    void Update()
    {
        // vitesse de base mappée par la courbe
        float baseSpeed = BaseSpeedNow;

        // applique multiplicateur + offset
        float v = paused ? 0f : Mathf.Max(0f, baseSpeed * _factor - slowOffset);
        CurrentSpeed = v;

        transform.position += Vector3.left * v * Time.deltaTime;

        // retour du tilt quand pas d’oscillation
        if (swayCo == null && tiltTarget)
        {
            tiltTarget.localRotation = Quaternion.Slerp(
                tiltTarget.localRotation, tiltInitialLocalRot, Time.deltaTime * tiltReturnSpeed);
        }
    }

    // ---------------- API Jeu ----------------

    public void Pause()  { paused = true; }
    public void Resume() { paused = false; }

    public void ApplyDamage(float amount)
    {
        currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
        Debug.Log($"[MoveLeft] -{amount} HP → {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Ralentissement multiplicatif : factor in [0..1]. 1 = aucune perte, 0.5 = moitié.
    /// </summary>
    public void ApplySlowdownFactor(float factor, float duration)
    {
        factor = Mathf.Clamp01(factor);
        if (_factorCo != null) StopCoroutine(_factorCo);
        _factorCo = StartCoroutine(CoFactor(factor, duration));
    }

    IEnumerator CoFactor(float targetFactor, float duration)
    {
        _factor = targetFactor;
        yield return new WaitForSeconds(duration);
        _factor = 1f;           // retour normal
        _factorCo = null;
    }

    /// <summary>
    /// Ralentissement absolu : enlève 'amount' unités/s pendant 'duration'.
    /// </summary>
    public void ApplySlowdown(float amount, float duration)
    {
        amount = Mathf.Max(0f, amount);
        if (_offsetCo != null) StopCoroutine(_offsetCo);
        _offsetCo = StartCoroutine(CoOffset(amount, duration));
    }

    IEnumerator CoOffset(float amount, float duration)
    {
        slowOffset = amount;
        yield return new WaitForSeconds(duration);
        slowOffset = 0f;
        _offsetCo = null;
    }

    /// <summary>Annule tout effet de ralentissement en cours.</summary>
    public void ClearAllSlowdowns()
    {
        if (_factorCo != null) StopCoroutine(_factorCo);
        if (_offsetCo  != null) StopCoroutine(_offsetCo);
        _factorCo = null;
        _offsetCo  = null;
        _factor = 1f;
        slowOffset = 0f;
    }

    public void ApplySway(float amplitudeDeg, float duration, float frequencyHz = 1f)
    {
        if (!tiltTarget || amplitudeDeg <= 0f || duration <= 0f) return;
        if (swayCo != null) StopCoroutine(swayCo);
        swayCo = StartCoroutine(CoSway(amplitudeDeg, duration, frequencyHz));
    }

    IEnumerator CoSway(float ampDeg, float duration, float freqHz)
    {
        float t = 0f;
        Vector3 axis = tiltAxis == TiltAxis.PitchX ? Vector3.right :
                       tiltAxis == TiltAxis.YawY   ? Vector3.up    :
                                                     Vector3.forward;
        while (t < duration)
        {
            t += Time.deltaTime;
            float angle = Mathf.Sin(t * Mathf.PI * 2f * freqHz) * ampDeg;
            tiltTarget.localRotation = tiltInitialLocalRot * Quaternion.AngleAxis(angle, axis);
            yield return null;
        }
        tiltTarget.localRotation = tiltInitialLocalRot;
        swayCo = null;
    }
}
