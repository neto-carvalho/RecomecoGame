#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FoodMealIconGenerator
{
    const int Size = 128;
    const string IconFolder = "Assets/Items/Icons/Food";

    static readonly (string fileName, System.Action<Color32[]> draw)[] Recipes =
    {
        ("icon_food_lanche.png", DrawLancheClassico),
        ("icon_food_xsalada.png", DrawXSalada),
        ("icon_food_hotdog.png", DrawHotDog),
        ("icon_food_refrigerante.png", DrawRefrigerante),
        ("icon_food_milkshake.png", DrawMilkShake),
        ("icon_food_duplicado.png", DrawLancheDuplicado),
    };

    public static readonly (string assetName, string iconFile)[] FoodIconMap =
    {
        ("LancheClassico", "icon_food_lanche.png"),
        ("XSalada", "icon_food_xsalada.png"),
        ("HotDog", "icon_food_hotdog.png"),
        ("Refrigerante", "icon_food_refrigerante.png"),
        ("MilkShake", "icon_food_milkshake.png"),
        ("LancheDuplicado", "icon_food_duplicado.png"),
    };

    [MenuItem("Recomeco/Loja/Gerar ícones dos lanches FOOD4U")]
    public static void GenerateAllIcons()
    {
        GenerateAllIconsInternal();
        EditorUtility.DisplayDialog("FOOD4U", "Ícones gerados em " + IconFolder + ".\n\n" +
            "Use \"Criar itens de lanchonete\" para aplicá-los aos assets.", "OK");
    }

    public static void GenerateAllIconsInternal()
    {
        EnsureFolder();
        foreach (var (fileName, draw) in Recipes)
        {
            var pixels = NewBuffer();
            draw(pixels);
            WritePng(Path.Combine(IconFolder, fileName), pixels);
        }

        AssetDatabase.Refresh();
        foreach (var (fileName, _) in Recipes)
            EnsureSpriteImport(IconFolder + "/" + fileName);

        AssetDatabase.SaveAssets();
    }

    public static Sprite LoadFoodIcon(string iconFile)
    {
        if (string.IsNullOrEmpty(iconFile))
            return null;

        var path = IconFolder + "/" + iconFile;
        EnsureSpriteImport(path);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (sub is Sprite s)
                return s;
        }

        return null;
    }

    [MenuItem("Recomeco/Loja/Aplicar ícones dos lanches nos ItemData")]
    public static void ApplyIconsToFoodItems()
    {
        var updated = 0;
        foreach (var pair in FoodIconMap)
        {
            var itemPath = "Assets/Items/Food/" + pair.assetName + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
            if (item == null)
                continue;

            var icon = LoadFoodIcon(pair.iconFile);
            if (icon == null)
                continue;

            item.icon = icon;
            EditorUtility.SetDirty(item);
            updated++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("FOOD4U", updated + " item(ns) com ícone de lanchonete aplicado.", "OK");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Items/Icons"))
            AssetDatabase.CreateFolder("Assets/Items", "Icons");
        if (!AssetDatabase.IsValidFolder(IconFolder))
            AssetDatabase.CreateFolder("Assets/Items/Icons", "Food");
    }

    static Color32[] NewBuffer()
    {
        var buf = new Color32[Size * Size];
        for (var i = 0; i < buf.Length; i++)
            buf[i] = new Color32(0, 0, 0, 0);
        return buf;
    }

    static void WritePng(string assetPath, Color32[] pixels)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    static void FillRect(Color32[] px, int x, int y, int w, int h, Color32 c)
    {
        for (var j = y; j < y + h; j++)
        {
            if (j < 0 || j >= Size)
                continue;
            for (var i = x; i < x + w; i++)
            {
                if (i < 0 || i >= Size)
                    continue;
                px[j * Size + i] = c;
            }
        }
    }

    static void DrawLancheClassico(Color32[] px)
    {
        var bun = new Color32(214, 168, 84, 255);
        var meat = new Color32(120, 72, 48, 255);
        var cheese = new Color32(255, 210, 60, 255);
        FillRect(px, 28, 78, 72, 18, bun);
        FillRect(px, 32, 58, 64, 22, meat);
        FillRect(px, 30, 52, 68, 8, cheese);
        FillRect(px, 26, 32, 76, 22, bun);
        FillRect(px, 34, 36, 60, 8, new Color32(235, 190, 100, 255));
    }

    static void DrawXSalada(Color32[] px)
    {
        var bun = new Color32(200, 150, 70, 255);
        var meat = new Color32(100, 55, 35, 255);
        var lettuce = new Color32(70, 180, 70, 255);
        var tomato = new Color32(220, 60, 50, 255);
        FillRect(px, 24, 80, 80, 16, bun);
        FillRect(px, 28, 62, 72, 20, meat);
        FillRect(px, 26, 52, 76, 10, lettuce);
        FillRect(px, 38, 44, 22, 10, tomato);
        FillRect(px, 68, 44, 22, 10, tomato);
        FillRect(px, 22, 28, 84, 18, bun);
        FillRect(px, 44, 30, 8, 12, new Color32(255, 255, 255, 255));
    }

    static void DrawHotDog(Color32[] px)
    {
        var bun = new Color32(240, 200, 90, 255);
        var sausage = new Color32(200, 70, 55, 255);
        var mustard = new Color32(255, 220, 40, 255);
        FillRect(px, 18, 52, 92, 28, bun);
        FillRect(px, 22, 58, 84, 16, sausage);
        FillRect(px, 24, 60, 80, 4, mustard);
        FillRect(px, 16, 48, 12, 36, bun);
        FillRect(px, 100, 48, 12, 36, bun);
    }

    static void DrawRefrigerante(Color32[] px)
    {
        var cup = new Color32(200, 40, 45, 255);
        var lid = new Color32(240, 240, 240, 255);
        var straw = new Color32(255, 255, 255, 255);
        var ice = new Color32(180, 220, 255, 200);
        FillRect(px, 40, 36, 48, 72, cup);
        FillRect(px, 36, 28, 56, 12, lid);
        FillRect(px, 72, 8, 6, 28, straw);
        FillRect(px, 44, 48, 20, 16, ice);
        FillRect(px, 66, 56, 14, 12, ice);
        FillRect(px, 48, 100, 32, 8, new Color32(160, 30, 35, 255));
    }

    static void DrawMilkShake(Color32[] px)
    {
        var cup = new Color32(255, 150, 190, 255);
        var cream = new Color32(255, 245, 250, 255);
        var straw = new Color32(255, 100, 140, 255);
        var cherry = new Color32(220, 30, 50, 255);
        FillRect(px, 38, 40, 52, 70, cup);
        FillRect(px, 34, 32, 60, 14, cream);
        FillRect(px, 30, 24, 68, 12, cream);
        FillRect(px, 78, 12, 5, 36, straw);
        FillRect(px, 58, 18, 10, 10, cherry);
        FillRect(px, 42, 102, 44, 6, new Color32(200, 120, 160, 255));
    }

    static void DrawLancheDuplicado(Color32[] px)
    {
        var bun = new Color32(140, 130, 110, 255);
        var meat = new Color32(80, 100, 70, 255);
        var mold = new Color32(60, 140, 60, 255);
        FillRect(px, 28, 76, 72, 18, bun);
        FillRect(px, 32, 56, 64, 22, meat);
        FillRect(px, 36, 60, 18, 14, mold);
        FillRect(px, 70, 62, 16, 12, mold);
        FillRect(px, 26, 34, 76, 20, bun);
        FillRect(px, 52, 44, 24, 24, new Color32(255, 255, 255, 255));
        FillRect(px, 58, 50, 4, 14, new Color32(40, 40, 40, 255));
        FillRect(px, 64, 54, 8, 4, new Color32(40, 40, 40, 255));
    }
}
#endif
