using UnityEditor;
using UnityEngine;

/// <summary>Converte materiais Built-in do pack Sir_bike para URP Lit (evita skate rosa na Demo e no jogo).</summary>
public static class SirBikeMaterialUpgradeMenu
{
    const string MaterialsFolder = "Assets/Sir_bike/Materials";

    [MenuItem("Recomeco/Assets/Sir_bike — converter materiais para URP")]
    public static void ConvertSirBikeMaterialsToUrp()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Recomeco", "Shader URP Lit não encontrado. O projeto usa URP?", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        var converted = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == urpLit)
                continue;

            var shaderName = mat.shader != null ? mat.shader.name : string.Empty;
            if (shaderName.Contains("Universal Render Pipeline"))
                continue;

            var tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : mat.mainTexture;
            var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

            mat.shader = urpLit;
            if (tex != null)
                mat.mainTexture = tex;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.35f);

            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco",
            $"Materiais convertidos para URP Lit: {converted}\n\nAbra a cena Demo do pack para conferir.",
            "OK");
    }
}