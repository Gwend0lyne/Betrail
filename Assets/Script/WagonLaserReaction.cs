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

    [Header("Slowdown")]
    [Tooltip("Absolute speed offset applied on hit. Set high to almost stop the wagon.")]
    public float slowAmount = 999f;
    [Tooltip("How long the slowdown effect lasts (seconds).")]
    public float slowDuration = 1.5f;
    [Tooltip("Extra padding added on top of the current speed before clamping.")]
    public float slowPadding = 0.5f;

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
        if (!moveComponent)
            return;

        float current = Mathf.Max(0f, moveComponent.CurrentSpeed);
        float requested = Mathf.Max(0f, slowAmount);
        float effective = Mathf.Max(requested, current + slowPadding);
        moveComponent.ApplySlowdown(effective, slowDuration);
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
}
