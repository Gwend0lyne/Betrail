using UnityEngine;

public class WagonController : MonoBehaviour
{
    [Header("Référence du défilement monde")]
    public MoveLeft mover;                     // glisse l’objet “grotte” (MoveLeft)

    [Header("Glace / collisions")]
    [Tooltip("Si la vitesse monde >= seuil → la glace est cassée.")]
    public float breakIceSpeedThreshold = 5.0f;

    [Tooltip("Facteur appliqué quand on casse (0.5 = moitié de vitesse).")]
    [Range(0f,1f)] public float slowdownFactorOnBreak = 0.5f;

    [Tooltip("Durée du ralentissement de casse (s).")]
    public float slowdownDuration = 1.0f;

    public bool isStoppedByIce { get; private set; }

    /// <summary>Vitesse monde réellement appliquée (via MoveLeft).</summary>
    public float CurrentSpeed => mover ? mover.CurrentSpeed : 0f;

    /// <summary>La vitesse actuelle permet-elle de casser la glace ?</summary>
    public bool CanBreakIce()
    {
        return CurrentSpeed >= breakIceSpeedThreshold;
    }

    /// <summary>Ralentissement court lorsqu’on casse l’amas.</summary>
    public void ApplyBreakSlowdown()
    {
        if (!mover) return;
        // multiplicatif (propre pour “moitié de vitesse pendant X s”)
        mover.ApplySlowdownFactor(slowdownFactorOnBreak, slowdownDuration);
    }

    /// <summary>Bloqué par la glace (pas assez vite).</summary>
    public void StopDueToIce()
    {
        if (!mover) return;
        if (isStoppedByIce) return;
        isStoppedByIce = true;

        // stop net + annule les ralentissements en cours
        mover.ClearAllSlowdowns();
        mover.Pause();
    }

    /// <summary>Libéré après fonte de la glace.</summary>
    public void ReleaseFromIce()
    {
        if (!mover) return;
        if (!isStoppedByIce) return;
        isStoppedByIce = false;
        mover.Resume();
    }
}