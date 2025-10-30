using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Header("Source de la vitesse")]
    public PanelDuo source;                 // % piloté par ton UI

    [Header("Mapping vitesse")]
    public float maxWorldSpeed = 5f;
    public AnimationCurve response = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Facteur externe (ralentis/stop)")]
    [Range(0f, 1f)] public float externalFactor = 1f;   // 1 = vitesse normale, 0 = stop

    public float CurrentSpeed { get; private set; }      // vitesse monde instantanée (u/s)

    Coroutine slowdownCo;

    void Update()
    {
        if (!source) return;

        float pct01  = Mathf.Clamp01(source.SpeedPercent / 100f);
        float factor = response.Evaluate(pct01);

        CurrentSpeed = maxWorldSpeed * factor * externalFactor;
        transform.position += Vector3.left * CurrentSpeed * Time.deltaTime;
    }

    // ---- API pilotée par le gameplay ----
    public void Pause() => externalFactor = 0f;

    public void Resume() => externalFactor = 1f;

    public void ApplySlowdown(float factor, float duration)
    {
        if (slowdownCo != null) StopCoroutine(slowdownCo);
        slowdownCo = StartCoroutine(SlowdownRoutine(factor, duration));
    }

    System.Collections.IEnumerator SlowdownRoutine(float factor, float duration)
    {
        // applique un facteur <1, puis revient à 1 au bout de 'duration'
        externalFactor = Mathf.Clamp01(factor);
        yield return new WaitForSeconds(duration);
        externalFactor = 1f;
        slowdownCo = null;
    }
}