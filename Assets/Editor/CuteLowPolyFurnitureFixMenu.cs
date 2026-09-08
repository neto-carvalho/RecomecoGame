#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CuteLowPolyFurnitureFixMenu
{
    const string PackRoot = "Assets/CuteMagic_Free/Cute Low Poly Furniture Pack Free";
    [MenuItem("Recomeco/Casas/Corrigir Cute Low Poly Furniture (rosa → URP/shaders)")]
    static void FixFurnitureMaterials()
    {
        var celShader = Shader.Find("CuteMagic/Mobile_CelShader");
        var pbrShader = Shader.Find("CuteMagic/Mobile_HighPerformance_PBR");
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("URP", "Shader URP Lit não encontrado.", "OK");
            return;
        }

        var fixedCel = 0;
        var fixedPbr = 0;
        var fixedUrp = 0;
        var skipped = 0;

        var guids = AssetDatabase.FindAssets("t:Material", new[] { PackRoot });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            if (!NeedsFix(material))
            {
                skipped++;
                continue;
            }

            if (ShouldUsePbrShader(material) && pbrShader != null &&
                ApplyShader(material, pbrShader))
            {
                fixedPbr++;
            }
            else if (celShader != null && ApplyShader(material, celShader))
            {
                fixedCel++;
            }
            else if (ConvertToUrpLit(material, urpLit))
            {
                fixedUrp++;
            }

            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Cute Low Poly Furniture",
            $"Correção concluída.\n\n" +
            $"• Cel shader: {fixedCel}\n" +
            $"• PBR shader: {fixedPbr}\n" +
            $"• URP Lit (fallback): {fixedUrp}\n" +
            $"• Já OK: {skipped}\n\n" +
            "Reabra a cena Demo_Room. Se algo ainda estiver rosa, use o fallback URP Lit abaixo.",
            "OK");

        Debug.Log(
            $"[Cute Furniture] cel={fixedCel}, pbr={fixedPbr}, urp={fixedUrp}, skipped={skipped}");
    }

    [MenuItem("Recomeco/Casas/Corrigir Cute Furniture (texturas brancas)")]
    static void FixMissingTextures()
    {
        var palette1a = AssetDatabase.LoadAssetAtPath<Texture2D>(
            $"{PackRoot}/ShaderTexture/1a.png");
        var paletteColors = AssetDatabase.LoadAssetAtPath<Texture2D>(
            $"{PackRoot}/ShaderTexture/Base_color_1.png");
        var wallAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
            $"{PackRoot}/ShaderTexture/2.png");
        var masterAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
            $"{PackRoot}/ShaderTexture/Master_Texture.png");

        var fixedCount = 0;
        var guids = AssetDatabase.FindAssets("t:Material", new[] { PackRoot });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            var changed = false;
            changed |= AssignIfMissing(material, "_AlbedoMap", ResolveAlbedo(path, palette1a, paletteColors, wallAtlas, masterAtlas));
            changed |= AssignIfMissing(material, "_BaseMap", ResolveAlbedo(path, palette1a, paletteColors, wallAtlas, masterAtlas));
            changed |= AssignIfMissing(material, "_MainTex", ResolveAlbedo(path, palette1a, paletteColors, wallAtlas, masterAtlas));

            if (!changed)
                continue;

            EditorUtility.SetDirty(material);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Cute Low Poly Furniture",
            $"{fixedCount} materiais tiveram texturas corrigidas.\nReabra Demo_Room.",
            "OK");
    }

    static Texture ResolveAlbedo(string materialPath, Texture palette1a, Texture paletteColors,
        Texture wallAtlas, Texture masterAtlas)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(materialPath).ToLowerInvariant();

        if (name.StartsWith("texture_wall") || name == "2")
            return wallAtlas;

        if (name.StartsWith("base_color"))
            return paletteColors;

        if (name.Contains("shadertexture_02"))
            return masterAtlas;

        return palette1a;
    }

    static bool AssignIfMissing(Material material, string property, Texture texture)
    {
        if (texture == null || !material.HasProperty(property))
            return false;

        if (material.GetTexture(property) != null)
            return false;

        material.SetTexture(property, texture);
        return true;
    }

    [MenuItem("Recomeco/Casas/Corrigir Cute Furniture (forçar tudo URP Lit)")]
    static void ForceAllUrpLit()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("URP", "Shader URP Lit não encontrado.", "OK");
            return;
        }

        var converted = 0;
        var guids = AssetDatabase.FindAssets("t:Material", new[] { PackRoot });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || !ConvertToUrpLit(material, urpLit))
                continue;

            EditorUtility.SetDirty(material);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Cute Low Poly Furniture",
            $"{converted} materiais convertidos para URP Lit.\nReabra Demo_Room.",
            "OK");
    }

    static bool NeedsFix(Material material)
    {
        if (material.shader == null)
            return true;

        return material.shader.name == "Hidden/InternalErrorShader";
    }

    static bool ShouldUsePbrShader(Material material)
    {
        return material.HasProperty("_NormalInflow") &&
               material.HasProperty("_PBRComposite") &&
               !material.HasProperty("_CombinedMask");
    }

    static bool ApplyShader(Material material, Shader shader)
    {
        if (shader == null)
            return false;

        material.shader = shader;
        return true;
    }

    static bool ConvertToUrpLit(Material material, Shader urpLit)
    {
        if (urpLit == null)
            return false;

        var shaderName = material.shader != null ? material.shader.name : "";
        if (shaderName.StartsWith("Universal Render Pipeline/"))
            return false;

        var albedo = GetAlbedoTexture(material);
        var color = material.HasProperty("_BaseColorTint")
            ? material.GetColor("_BaseColorTint")
            : material.HasProperty("_Tint")
                ? material.GetColor("_Tint")
                : material.HasProperty("_Color")
                    ? material.GetColor("_Color")
                    : Color.white;

        material.shader = urpLit;

        if (albedo != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", albedo);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.25f);

        return true;
    }

    static Texture GetAlbedoTexture(Material material)
    {
        string[] names =
        {
            "_AlbedoMap", "_BaseMap", "_MainTex"
        };

        foreach (var name in names)
        {
            if (!material.HasProperty(name))
                continue;

            var tex = material.GetTexture(name);
            if (tex != null)
                return tex;
        }

        return null;
    }
}
#endif
