#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LowPolyHousesFreePackUrpFixMenu
{
    const string PackRoot = "Assets/Casas/Low Poly Houses Free Pack";
    const string MainMaterialPath = PackRoot + "/Materials/mat main.mat";
    const string MainTexturePath = PackRoot + "/Textures/texture_main.png";

    [MenuItem("Recomeco/Casas/Corrigir Low Poly Houses Free Pack (rosa → URP)")]
    static void ConvertPackMaterialsToUrp()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "URP",
                "Shader \"Universal Render Pipeline/Lit\" não encontrado.",
                "OK");
            return;
        }

        var mainTex = AssetDatabase.LoadAssetAtPath<Texture>(MainTexturePath);
        var converted = 0;

        var guids = AssetDatabase.FindAssets("t:Material", new[] { PackRoot + "/Materials" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                continue;

            if (ConvertMaterial(mat, urpLit, mainTex))
            {
                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Low Poly Houses Free Pack",
            $"Materiais convertidos: {converted}\n\n" +
            "Todos os prefabs usam \"mat main\" — devem aparecer coloridos agora.\n" +
            "Escala ~0,2 ao colocar na Cidade.",
            "OK");

        var mainMaterial = AssetDatabase.LoadAssetAtPath<Material>(MainMaterialPath);
        if (mainMaterial != null)
            Selection.activeObject = mainMaterial;

        Debug.Log($"[Low Poly Houses Free Pack] {converted} material(is) URP em {PackRoot}.");
    }

    static bool ConvertMaterial(Material mat, Shader urpLit, Texture fallbackTex)
    {
        var shaderName = mat.shader != null ? mat.shader.name : "";
        if (shaderName.StartsWith("Universal Render Pipeline/"))
            return false;

        var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        if (mainTex == null && mat.HasProperty("_BaseMap"))
            mainTex = mat.GetTexture("_BaseMap");
        if (mainTex == null)
            mainTex = fallbackTex;

        var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

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
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.15f);

        return true;
    }
}
#endif
