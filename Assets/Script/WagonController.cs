using UnityEngine;

public class WagonController : MonoBehaviour
{
    [Header("Référence du défilement monde")]
    public MoveLeft mover;                  // glisse ici l'objet "grotte" (qui a MoveLeft)

    [Header("Glace / collisions")]
    public float breakIceSpeedThreshold = 3.0f;  // si la vitesse monde >= seuil -> casse la glace
    public float slowdownFactorOnBreak = 0.5f;   // ralentissement temporaire (0..1)
    public float slowdownDuration = 1.0f;

    public bool isStoppedByIce { get; private set; }

    public float CurrentSpeed => mover ? mover.CurrentSpeed : 0f;

    public bool CanBreakIce() => CurrentSpeed >= breakIceSpeedThreshold;

    public void ApplyBreakSlowdown()
    {
        if (!mover) return;
        mover.ApplySlowdown(slowdownFactorOnBreak, slowdownDuration);
    }

    public void StopDueToIce()
    {
        if (!mover) return;
        if (isStoppedByIce) return;
        isStoppedByIce = true;
        mover.Pause();
    }

    public void ReleaseFromIce()
    {
        if (!mover) return;
        if (!isStoppedByIce) return;
        isStoppedByIce = false;
        mover.Resume();
    }
}