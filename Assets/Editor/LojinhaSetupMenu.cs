#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LojinhaSetupMenu
{
    const string MenuRoot = "Recomeco/Loja/";

    static readonly (string assetName, string packLabel, int units, int packCents)[] Catalog =
    {
        ("Pacoca", "Pote de paçoca", 10, 500),
        ("Chiclete", "Pacote de chiclete", 10, 150),
        ("Biscoito", "Pacote de biscoito", 20, 250),
        ("AguaMineral", "Fardo de água", 6, 900),
        ("BalaDeGoma", "Saco de bala de goma", 20, 400),
        ("Cocada", "Bandeja de cocada", 8, 1200),
    };

    [MenuItem(MenuRoot + "Configurar Lojinha (objeto selecionado ou \"Lojinha\")")]
    static void SetupLojinha()
    {
        var target = Selection.activeGameObject;
        if (target == null)
            target = GameObject.Find("Lojinha");

        if (target == null)
        {
            EditorUtility.DisplayDialog(
                "Lojinha",
                "Selecione o objeto da loja na Hierarchy (ou nomeie-o \"Lojinha\") e tente de novo.",
                "OK");
            return;
        }

        var shop = target.GetComponent<ShopZone>();
        if (shop == null)
            shop = Undo.AddComponent<ShopZone>(target);

        var products = new List<ShopZone.ShopProduct>();
        var missing = new List<string>();

        foreach (var (assetName, packLabel, units, packCents) in Catalog)
        {
            var item = FindItemAsset(assetName);
            if (item == null)
            {
                missing.Add(assetName);
                continue;
            }

            products.Add(new ShopZone.ShopProduct
            {
                item = item,
                packLabel = packLabel,
                unitsPerPack = units,
                packPriceCents = packCents,
            });
        }

        Undo.RecordObject(shop, "Configurar Lojinha");
        shop.products = products.ToArray();
        shop.shopTitle = "LOJINHA";
        EditorUtility.SetDirty(shop);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.scene);

        var msg = "ShopZone configurado em \"" + target.name + "\" com " + products.Count + " produtos.\n\n" +
                  "Perto da loja: teclas 1-" + products.Count + " compram os pacotes.\n" +
                  "Salve a cena (Ctrl+S).";
        if (missing.Count > 0)
            msg += "\n\nAVISO — assets não encontrados: " + string.Join(", ", missing) +
                   " (verifique a pasta Assets/Items).";

        EditorUtility.DisplayDialog("Lojinha", msg, "OK");
        Selection.activeGameObject = target;
    }

    static readonly (string assetName, string iconFile)[] ItemIcons =
    {
        ("Pacoca", "icon_pacoca.png"),
        ("Chiclete", "icon_chiclete.png"),
        ("Biscoito", "icon_biscoito.png"),
        ("AguaMineral", "icon_agua.png"),
        ("BalaDeGoma", "icon_bala.png"),
        ("Cocada", "icon_cocada.png"),
    };

    [MenuItem(MenuRoot + "Atribuir ícones dos itens")]
    static void AssignItemIcons()
    {
        var assigned = 0;
        var missing = new List<string>();

        foreach (var (assetName, iconFile) in ItemIcons)
        {
            var iconPath = "Assets/Items/Icons/" + iconFile;
            var sprite = EnsureSpriteImport(iconPath);
            var item = FindItemAsset(assetName);

            if (sprite == null || item == null)
            {
                missing.Add(assetName);
                continue;
            }

            item.icon = sprite;
            EditorUtility.SetDirty(item);
            assigned++;
        }

        AssetDatabase.SaveAssets();

        var msg = assigned + " item(ns) com ícone atribuído.";
        if (missing.Count > 0)
            msg += "\n\nFalhou: " + string.Join(", ", missing) +
                   "\nConfirme os ficheiros em Assets/Items/Icons e os assets em Assets/Items.";
        EditorUtility.DisplayDialog("Ícones dos itens", msg, "OK");
    }

    static Sprite EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return null;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem(MenuRoot + "Criar ponto de venda de rua (StreetSellZone)")]
    static void CreateStreetSellSpot()
    {
        var pivot = Vector3.zero;
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
        {
            var cam = SceneView.lastActiveSceneView.camera.transform;
            pivot = cam.position + cam.forward * 6f;
            if (Physics.Raycast(pivot + Vector3.up * 50f, Vector3.down, out var hit, 200f))
                pivot = hit.point;
        }

        var go = new GameObject("PontoVenda_Rua");
        Undo.RegisterCreatedObjectUndo(go, "Create StreetSellZone");
        go.transform.position = pivot;
        go.AddComponent<StreetSellZone>();

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[Loja] \"PontoVenda_Rua\" criado. Arraste para a calçada onde o jogador vende (E) e salve a cena.");
    }

    static ItemData FindItemAsset(string assetName)
    {
        var direct = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/" + assetName + ".asset");
        if (direct != null)
            return direct;

        foreach (var guid in AssetDatabase.FindAssets("t:ItemData " + assetName))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
                return item;
        }

        return null;
    }
}
#endif
