#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Video;

public static class DigitalClockFontMenu
{
    const string MenuRoot = "Recomeco/UI/";
    const string GameplaySettingsPath = "Assets/Resources/RecomecoGameplaySettings.asset";
    const string NightSkyboxPath = "Assets/Flat_Style_Vehicles/Skyboxes/Skybox2/Skybox2.mat";
    const string SleepVideoProjectPath = "Assets/Resources/Video/carneirinhos_pulando.mp4";
    const string SleepVideoDownloadsPath = @"c:\Users\netoc\Downloads\carneirinhos pulando.mp4";

    [MenuItem("Recomeco/Cidade/Configurar vídeo de dormir (barraca)")]
    static void SetupBarracaSleepVideo()
    {
        if (!File.Exists(SleepVideoProjectPath) && File.Exists(SleepVideoDownloadsPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SleepVideoProjectPath) ?? "Assets/Resources/Video");
            File.Copy(SleepVideoDownloadsPath, SleepVideoProjectPath, true);
            AssetDatabase.Refresh();
        }

        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(SleepVideoProjectPath);
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Vídeo dormir",
                "Coloque o MP4 em:\n" + SleepVideoProjectPath + "\n\n" +
                "Ou deixe o arquivo em Downloads com o nome:\n" +
                "carneirinhos pulando.mp4\n\ne rode este menu de novo.",
                "OK");
            return;
        }

        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(GameplaySettingsPath);
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Vídeo dormir", "RecomecoGameplaySettings não encontrado.", "OK");
            return;
        }

        settings.precariousSleepVideoClip = clip;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Vídeo dormir",
            "Vídeo de descanso configurado (" + clip.length.ToString("0.0") + "s).\n\n" +
            "Teste: Play → Cidade → dormir na barraca à noite.",
            "OK");
    }

    [MenuItem("Recomeco/Cidade/Atribuir céu noturno (Skybox2) no Gameplay Settings")]
    static void AssignNightSkyboxToSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(GameplaySettingsPath);
        var sky = AssetDatabase.LoadAssetAtPath<Material>(NightSkyboxPath);
        if (settings == null || sky == null)
        {
            EditorUtility.DisplayDialog("Céu noturno",
                "Não encontrei RecomecoGameplaySettings ou Skybox2.mat.",
                "OK");
            return;
        }

        settings.nightSkyboxMaterial = sky;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Céu noturno",
            "Skybox2 (estrelas) ligado em RecomecoGameplaySettings → nightSkyboxMaterial.",
            "OK");
    }

    const string TtfPath = "Assets/Fonts/DSEG7Classic-Bold.ttf";
    const string SdfPath = "Assets/Resources/Fonts/DSEG7Classic-Bold SDF.asset";

    [MenuItem(MenuRoot + "Gerar fonte TMP relógio digital (DSEG7)")]
    static void CreateDigitalClockFontAsset()
    {
        if (!File.Exists(TtfPath))
        {
            EditorUtility.DisplayDialog("Relógio digital",
                "Coloque o arquivo DSEG7Classic-Bold.ttf em:\n" + TtfPath + "\n\n" +
                "Baixe em: https://github.com/keshikan/DSEG (build ou releases)\n" +
                "Enquanto isso, o relógio usa estilo verde temporário.",
                "OK");
            return;
        }

        var folder = Path.GetDirectoryName(SdfPath);
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder("Assets/Resources/Fonts"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
        }

        var source = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (source == null)
        {
            EditorUtility.DisplayDialog("Relógio digital", "Não foi possível ler o TTF.", "OK");
            return;
        }

        var sdf = TMP_FontAsset.CreateFontAsset(
            source,
            90,
            9,
            GlyphRenderMode.SDFAA,
            512,
            512);

        if (sdf == null)
        {
            EditorUtility.DisplayDialog("Relógio digital", "Falha ao criar TMP Font Asset.", "OK");
            return;
        }

        sdf.name = "DSEG7Classic-Bold SDF";
        AssetDatabase.CreateAsset(sdf, SdfPath);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Relógio digital",
            "Fonte criada em:\n" + SdfPath + "\n\nEntre no Play para ver o relógio estilo 7 segmentos.",
            "OK");
    }
}
#endif
