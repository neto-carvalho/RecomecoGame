#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FaintAnimationSetupMenu
{
    const string FbxPath = "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Mesh/Aminset_Basic.fbx";
    const string SettingsPath = "Assets/Resources/RecomecoGameplaySettings.asset";
    const string PreferredClipName = "Death_Forward";

    [MenuItem("Recomeco/Player/Configurar animação de desmaio")]
    static void AssignFaintClip()
    {
        var clip = FindClip(PreferredClipName) ?? FindClip("Death_Backward") ?? FindClip("Fall");
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Desmaio",
                "Não encontrei Death_Forward no FBX:\n" + FbxPath,
                "OK");
            return;
        }

        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(SettingsPath);
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Desmaio", "Settings não encontrado:\n" + SettingsPath, "OK");
            return;
        }

        settings.faintAnimationClip = clip;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Desmaio",
            "Clip atribuído: " + clip.name + "\n\nSalvo em RecomecoGameplaySettings.",
            "OK");
    }

    static AnimationClip FindClip(string clipName)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FbxPath))
        {
            if (obj is AnimationClip clip && clip.name == clipName && !clip.name.StartsWith("__"))
                return clip;
        }

        return null;
    }
}
#endif
