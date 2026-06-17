#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightingBalanceMenu
{
    const string FillLightName = "Fill Light (Ambient)";

    [MenuItem("Recomeco/Iluminação/Equilibrar sol e sombras (cena ativa)")]
    public static void BalanceActiveSceneLighting()
    {
        ApplyAmbientFill();
        ConfigureMainSun();
        EnsureFillLight();

        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog(
            "Iluminação",
            "Sol mais alto (menos contraste lateral), luz ambiente reforçada e \"" + FillLightName +
            "\" sem sombras.\n\nGuarda a cena (Ctrl+S).",
            "OK");
        Debug.Log("[Recomeco] Iluminação equilibrada na cena: " + scene.name);
    }

    public static void ApplyAmbientFill()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.84f, 0.96f);
        RenderSettings.ambientEquatorColor = new Color(0.64f, 0.68f, 0.74f);
        RenderSettings.ambientGroundColor = new Color(0.44f, 0.42f, 0.4f);
        RenderSettings.ambientIntensity = 1.9f;
        RenderSettings.subtractiveShadowColor = new Color(0.66f, 0.72f, 0.82f);
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.fog = false;
    }

    public static void ConfigureMainSun()
    {
        var sun = FindMainDirectionalLight();
        if (sun == null)
            return;

        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        sun.intensity = 1.1f;
        sun.shadowStrength = 0.62f;
        sun.color = new Color(1f, 0.94f, 0.82f);
        sun.shadows = LightShadows.Soft;
        EditorUtility.SetDirty(sun);
    }

    public static void EnsureFillLight()
    {
        var fillGo = GameObject.Find(FillLightName);
        if (fillGo == null)
        {
            fillGo = new GameObject(FillLightName);
            Undo.RegisterCreatedObjectUndo(fillGo, "Create Fill Light");
        }

        var fill = fillGo.GetComponent<Light>();
        if (fill == null)
            fill = Undo.AddComponent<Light>(fillGo);

        fill.type = LightType.Directional;
        fill.intensity = 0.5f;
        fill.color = new Color(0.72f, 0.82f, 0.96f);
        fill.shadows = LightShadows.None;
        fill.renderMode = LightRenderMode.ForcePixel;

        fillGo.transform.rotation = Quaternion.Euler(50f, 145f, 0f);
        EditorUtility.SetDirty(fillGo);
    }

    static Light FindMainDirectionalLight()
    {
        Light named = null;
        Light any = null;
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional || !light.enabled)
                continue;
            if (light.gameObject.name == "Directional Light")
                named = light;
            if (any == null)
                any = light;
        }
        return named != null ? named : any;
    }
}
#endif
