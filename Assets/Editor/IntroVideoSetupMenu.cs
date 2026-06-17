#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public static class IntroVideoSetupMenu
{
    const string MenuRoot = "Recomeco/Menu/";
    const string VideoPath = "Assets/Resources/Video/recomeco_intro.mp4";
    const string SettingsPath = "Assets/Resources/RecomecoGameplaySettings.asset";

    [MenuItem(MenuRoot + "Configurar vídeo intro")]
    static void ConfigureIntroVideo()
    {
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        if (clip == null)
        {
            EditorUtility.DisplayDialog(
                "Recomeco — vídeo intro",
                "Não encontrei o arquivo:\n" + VideoPath + "\n\n" +
                "Copie o MP4 para essa pasta no Project e aguarde o Unity importar.",
                "OK");
            return;
        }

        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(SettingsPath);
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Recomeco — vídeo intro",
                "Não encontrei " + SettingsPath, "OK");
            return;
        }

        Undo.RecordObject(settings, "Configurar vídeo intro");
        settings.introVideoClip = clip;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Recomeco — vídeo intro",
            "Vídeo intro configurado em RecomecoGameplaySettings.\n\n" +
            "Clip: " + clip.name + " (" + clip.length.ToString("0.0") + "s)\n\n" +
            "Teste: Play na cena MenuInicial → Jogar → escolha uma cena.",
            "OK");
    }
}
#endif
