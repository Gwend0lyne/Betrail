using UnityEngine;
using UnityEngine.Rendering;

public class ForceHDRProfile : MonoBehaviour
{
    public Volume caveVolume;

    void Awake()
    {
        if (caveVolume != null)
        {
            caveVolume.isGlobal = true;
            caveVolume.priority = 10f;
            Debug.Log($"✅ Volume forcé : {caveVolume.profile.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ Aucun volume assigné !");
        }
    }
}