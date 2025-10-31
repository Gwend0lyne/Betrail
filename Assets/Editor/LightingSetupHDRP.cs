using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class GenerateLightingSetup
{
    [MenuItem("Tools/Generate Cave Lighting Setup")]
    public static void GenerateSetup()
    {
        // Vérifie et crée le dossier Settings si besoin
        string folderPath = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
            Debug.Log("📁 Created folder: Assets/Settings");
        }

        string path = $"{folderPath}/CaveProfile.asset";

        // ----- Création du VolumeProfile -----
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            Debug.Log("🧱 Created new HDRP VolumeProfile at " + path);
        }

        // ----- Fonction utilitaire locale -----
        T AddOrGet<T>() where T : VolumeComponent
        {
            if (!profile.TryGet(out T comp))
            {
                comp = profile.Add<T>(true);
                AssetDatabase.AddObjectToAsset(comp, profile);
            }
            comp.active = true;
            return comp;
        }

        // BLOOM
        var bloom = AddOrGet<Bloom>();
        bloom.SetAllOverridesTo(true);
        bloom.intensity.value = 0.6f;
        bloom.scatter.value = 0.6f;
        bloom.threshold.value = 1.1f;

        // COLOR ADJUSTMENTS
        var colorAdj = AddOrGet<ColorAdjustments>();
        colorAdj.SetAllOverridesTo(true);
        colorAdj.postExposure.value = 0.2f;
        colorAdj.contrast.value = 25f;
        colorAdj.colorFilter.value = new Color(0.82f, 0.72f, 0.69f);

        // TONEMAPPING
        var tone = AddOrGet<Tonemapping>();
        tone.SetAllOverridesTo(true);
        tone.mode.value = TonemappingMode.ACES;

        // AMBIENT OCCLUSION
        var ao = AddOrGet<ScreenSpaceAmbientOcclusion>();
        ao.SetAllOverridesTo(true);
        ao.intensity.value = 0.5f;
        ao.radius.value = 0.3f;

        // FOG
        var fog = AddOrGet<Fog>();
        fog.SetAllOverridesTo(true);
        fog.enabled.value = true;
        fog.meanFreePath.value = 25f;
        fog.albedo.value = new Color(0.77f, 0.88f, 1f);

        // DEPTH OF FIELD
        var dof = AddOrGet<DepthOfField>();
        dof.SetAllOverridesTo(true);
        dof.focusMode.value = DepthOfFieldMode.Manual;
        dof.nearFocusStart.value = 0f;
        dof.nearFocusEnd.value = 2f;
        dof.farFocusStart.value = 8f;
        dof.farFocusEnd.value = 20f;

        // VIGNETTE
        var vignette = AddOrGet<Vignette>();
        vignette.SetAllOverridesTo(true);
        vignette.intensity.value = 0.25f;
        vignette.smoothness.value = 0.6f;

        // Sauvegarde propre
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ CaveProfile.asset successfully generated with overrides (Unity 2022.3 HDRP)");
    }
}
