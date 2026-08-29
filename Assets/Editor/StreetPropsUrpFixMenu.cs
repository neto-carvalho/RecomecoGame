#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class StreetPropsUrpFixMenu
{
    const string HdrpMaterialsRoot = "Assets/Prototype Collection/HDRP/Materials";
    const string UrpPackagePath = "Assets/Prototype Collection/Street Props URP.unitypackage";

    [MenuItem("Recomeco/Street Props/Corrigir materiais rosa (HDRP → URP)")]
    static void ConvertHdrpMaterialsToUrp()
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

        var guids = AssetDatabase.FindAssets("t:Material", new[] { HdrpMaterialsRoot });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Street Props",
                "Nenhum material encontrado em:\n" + HdrpMaterialsRoot +
                "\n\nImporta primeiro o pacote \"Street Props URP.unitypackage\" " +
                "e usa os prefabs da pasta URP.",
                "OK");
            return;
        }

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

        EditorUtility.DisplayDialog(
            "Street Props",
            $"Materiais convertidos para URP: {converted}\nIgnorados (já URP): {skipped}\n\n" +
            "Recomendado: importa também \"Street Props URP.unitypackage\" e usa prefabs da pasta URP.",
            "OK");

        Debug.Log($"[Street Props] URP: {converted} materiais convertidos em {HdrpMaterialsRoot}.");
    }

    [MenuItem("Recomeco/Street Props/Selecionar pacote URP para importar")]
    static void SelectUrpPackage()
    {
        var package = AssetDatabase.LoadAssetAtPath<Object>(UrpPackagePath);
        if (package == null)
        {
            EditorUtility.DisplayDialog(
                "Street Props",
                "Pacote não encontrado em:\n" + UrpPackagePath,
                "OK");
            return;
        }

        Selection.activeObject = package;
        EditorGUIUtility.PingObject(package);

        EditorUtility.DisplayDialog(
            "Street Props",
            "Pacote URP selecionado no Project.\n\n" +
            "1) Duplo clique em \"Street Props URP.unitypackage\"\n" +
            "2) Clica Import / Import All\n" +
            "3) Usa prefabs de Assets/Prototype Collection/URP/\n" +
            "4) NÃO uses a cena HDRP Street Props",
            "OK");
    }

    static bool ConvertMaterial(Material mat, Shader urpLit)
    {
        var shaderName = mat.shader != null ? mat.shader.name : "";

        if (shaderName.StartsWith("Universal Render Pipeline/") ||
            shaderName.StartsWith("Shader Graphs/"))
            return false;

        var baseMap = GetTexture(mat, "_BaseColorMap", "_MainTex", "_BaseMap");
        var baseColor = mat.HasProperty("_BaseColor")
            ? mat.GetColor("_BaseColor")
            : mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        var normalMap = GetTexture(mat, "_NormalMap", "_BumpMap");
        var smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.35f;

        mat.shader = urpLit;

        if (baseMap != null && mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", baseMap);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        if (normalMap != null && mat.HasProperty("_BumpMap"))
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        mat.DisableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);

        return true;
    }

    static Texture GetTexture(Material mat, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (mat.HasProperty(name))
            {
                var tex = mat.GetTexture(name);
                if (tex != null)
                    return tex;
            }
        }

        return null;
    }
}
#endif
