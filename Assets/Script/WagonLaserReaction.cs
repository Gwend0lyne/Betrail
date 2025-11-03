using System.Collections;
using UnityEngine;

/// <summary>
/// Handles the wagon reaction when the helicopter laser hits it:
/// applies a strong slowdown and triggers a local shake on the X axis.
/// </summary>
public class WagonLaserReaction : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Movement component that converts slowdown requests into reduced speed.")]
    public MoveLeft moveComponent;
    [Tooltip("Transform to shake on laser impact. Defaults to this transform.")]
    public Transform shakeTarget;
    [Tooltip("Controller du wagon (permet d'accéder au MoveLeft de la grotte).")]
    public WagonController wagonController;

    [Header("Slowdown")]
    [Tooltip("Absolute speed offset applied on hit. Set high to almost stop the wagon.")]
    public float slowAmount = 999f;
    [Tooltip("How long the slowdown effect lasts (seconds).")]
    public float slowDuration = 1.5f;
    [Tooltip("Extra padding added on top of the current speed before clamping.")]
    public float slowPadding = 0.5f;
    [Tooltip("Use a smooth multiplicative slowdown instead of an instant offset.")]
    public bool useSmoothSlowdown = true;
    [Range(0f, 1f), Tooltip("Target speed factor when the slowdown is fully applied (0 = stop).")]
    public float smoothSlowFactor = 0.05f;
    [Tooltip("Duration of the fade-in towards the smooth slowdown target (seconds).")]
    public float smoothFadeIn = 0.25f;
    [Tooltip("Additional duration to fade out of the smooth slowdown after hold (seconds).")]
    public float smoothFadeOut = 0.4f;

    [Header("Shake")]
    [Tooltip("Shake amplitude in degrees around the local X axis.")]
    public float shakeAmplitudeDeg = 9f;
    [Tooltip("Shake duration (seconds).")]
    public float shakeDuration = 1f;
    [Tooltip("Shake frequency in Hz.")]
    public float shakeFrequencyHz = 5f;

    Coroutine shakeRoutine;
    Quaternion shakeBaseRotation;

    void Awake()
    {
        if (!moveComponent)
            moveComponent = GetComponentInParent<MoveLeft>();

        if (!wagonController)
            wagonController = GetComponentInParent<WagonController>();

        if (!shakeTarget)
            shakeTarget = transform;

        if (shakeTarget)
            shakeBaseRotation = shakeTarget.localRotation;
    }

    void OnDisable()
    {
        ResetShake();
    }

    public void HandleLaserHit()
    {
        ApplySlowdown();
        TriggerShake();
    }

    void ApplySlowdown()
    {
        MoveLeft mover = ResolveMover();
        if (!mover)
        {
            Debug.LogWarning("[WagonLaserReaction] Aucun MoveLeft trouvé pour appliquer le slowdown.", this);
            return;
        }

        if (useSmoothSlowdown)
        {
            mover.ApplySmoothSlowdownFactor(
                smoothSlowFactor,
                smoothFadeIn,
                Mathf.Max(0f, slowDuration),
                smoothFadeOut);
        }
        else
        {
            float current = Mathf.Max(0f, mover.CurrentSpeed);
            float requested = Mathf.Max(0f, slowAmount);
            float effective = Mathf.Max(requested, current + slowPadding);
            mover.ApplySlowdown(effective, slowDuration);
        }
    }

    void TriggerShake()
    {
        if (!shakeTarget)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(CoShake());
    }

    IEnumerator CoShake()
    {
        if (!shakeTarget)
            yield break;

        shakeBaseRotation = shakeTarget.localRotation;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * Mathf.PI * 2f * shakeFrequencyHz) * shakeAmplitudeDeg;
            shakeTarget.localRotation = shakeBaseRotation * Quaternion.AngleAxis(angle, Vector3.right);
            yield return null;
        }

        ResetShake();
        shakeRoutine = null;
    }

    void ResetShake()
    {
        if (shakeTarget)
            shakeTarget.localRotation = shakeBaseRotation;
    }

    MoveLeft ResolveMover()
    {
        if (moveComponent)
            return moveComponent;

        if (wagonController && wagonController.mover)
            return wagonController.mover;

        return null;
    }
}
