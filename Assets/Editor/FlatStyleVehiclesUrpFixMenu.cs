#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FlatStyleVehiclesUrpFixMenu
{
    const string PackRoot = "Assets/Flat_Style_Vehicles";
    const string CidadeScenePath = "Assets/Scenes/Cidade.unity";

    [MenuItem("Recomeco/Flat Style Vehicles/Corrigir cena Cidade cinza (iluminação + materiais)")]
    static void FixDemoGrayLook()
    {
        ConvertPackMaterialsToUrp(silent: true);
        var scenePath = CidadeScenePath;
        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Cidade", "Cena não encontrada em:\n" + CidadeScenePath, "OK");
            return;
        }

        if (SceneManager.GetActiveScene().path != scenePath)
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var sky = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/AllSkyFree/Epic_BlueSunset/Epic_BlueSunset.mat");
        if (sky != null)
            RenderSettings.skybox = sky;

        DisableBakedGiForActiveScene();
        LightingBalanceMenu.ApplyAmbientFill();
        LightingBalanceMenu.ConfigureMainSun();
        LightingBalanceMenu.EnsureFillLight();

        EditorUtility.DisplayDialog(
            "Cidade",
            "Skybox, luz ambiente, sol mais alto, luz de preenchimento e GI assado desligado.\n\n" +
            "Guarda a cena Cidade (Ctrl+S). Na Scene View, desliga o botão Fog se ainda estiver ativo.",
            "OK");
        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("[Flat Style Vehicles] Cidade: iluminação e materiais ajustados.");
    }

    static void DisableBakedGiForActiveScene()
    {
        LightingSettings settings = null;

        if (Lightmapping.TryGetLightingSettings(out var existing))
            settings = existing;

        const string assetPath = PackRoot + "/Demo/Demo_LightingSettings.lighting";
        if (settings == null)
            settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(assetPath);

        if (settings == null)
        {
            Debug.LogWarning(
                "[Flat Style Vehicles] Lighting Settings não encontrado em " + assetPath +
                ". Abre a cena Cidade e guarda (Ctrl+S); o asset deve ser importado pelo Unity.");
            return;
        }

        settings.bakedGI = false;
        settings.realtimeGI = false;
        EditorUtility.SetDirty(settings);

        var scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            Lightmapping.SetLightingSettingsForScene(scene, settings);
    }

    [MenuItem("Recomeco/Flat Style Vehicles/Corrigir materiais rosa (URP)")]
    static void ConvertPackMaterialsToUrpMenu() => ConvertPackMaterialsToUrp(silent: false);

    static void ConvertPackMaterialsToUrp(bool silent)
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "URP",
                "Shader \"Universal Render Pipeline/Lit\" não encontrado. Confirma que o projeto usa URP.",
                "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { PackRoot });
        var converted = 0;
        var skipped = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                continue;

            if (ConvertMaterial(mat, urpLit))
            {
                EditorUtility.SetDirty(mat);
                converted++;
            }
            else
                skipped++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            EditorUtility.DisplayDialog(
                "Flat Style Vehicles",
                $"Materiais convertidos para URP: {converted}\nIgnorados (já URP ou skybox): {skipped}\n\n" +
                "Fecha e reabre a cena Cidade (ou volta à tua cena).",
                "OK");
        }

        Debug.Log($"[Flat Style Vehicles] URP: {converted} materiais convertidos em {PackRoot}.");
    }

    static bool ConvertMaterial(Material mat, Shader urpLit)
    {
        var shaderName = mat.shader != null ? mat.shader.name : "";

        if (shaderName.StartsWith("Universal Render Pipeline/") ||
            shaderName.StartsWith("Shader Graphs/"))
            return false;

        if (shaderName.Contains("Skybox"))
        {
            var sky = Shader.Find("Skybox/Procedural");
            if (sky != null)
                mat.shader = sky;
            return true;
        }

        var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        var smooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.2f;
        smooth = Mathf.Clamp(smooth * 0.35f, 0.05f, 0.2f);

        mat.shader = urpLit;

        if (mainTex != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", mainTex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", mainTex);
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smooth);

        mat.DisableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);

        return true;
    }
}
#endif
