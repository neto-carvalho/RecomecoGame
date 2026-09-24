#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Food4USetupMenu
{
    const string MenuRoot = "Recomeco/Loja/";
    const string FoodFolder = "Assets/Items/Food";

    /// assetName, display label, price cents, hunger, health, reputation, hint override (optional)
    static readonly (string assetName, string label, int priceCents, float hunger, float health, float rep, string hint)[] Menu =
    {
        ("LancheClassico", "Lanche clássico", 450, 42f, 5f, 0f, "+42 fome, +5 vida"),
        ("XSalada", "X-Salada", 620, 48f, 8f, 2f, "+48 fome, +8 vida, +2 rep."),
        ("HotDog", "Hot dog", 380, 32f, 0f, 0f, "+32 fome"),
        ("Refrigerante", "Refrigerante gelado", 250, 12f, 0f, 5f, "+12 fome, +5 rep."),
        ("MilkShake", "Milk-shake", 520, 35f, -5f, 0f, "+35 fome, -5 vida"),
        ("LancheDuplicado", "Lanche do dia (?) ", 290, 28f, -12f, -3f, "+28 fome, -12 vida, -3 rep."),
    };

    [MenuItem(MenuRoot + "Criar itens de lanchonete (Assets/Items/Food)")]
    static void CreateFoodItemAssets()
    {
        FoodMealIconGenerator.GenerateAllIconsInternal();
        EnsureFoodFolder();
        var created = 0;

        foreach (var entry in Menu)
        {
            var path = FoodFolder + "/" + entry.assetName + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
                created++;
            }

            item.itemName = entry.label.Trim();
            item.unitSellPriceCents = 0;
            item.consumable = true;
            item.hungerRestore = entry.hunger;
            item.healthRestore = entry.health;
            item.reputationRestore = entry.rep;
            item.consumableHint = string.IsNullOrEmpty(entry.hint)
                ? ItemData.BuildEffectHint(entry.hunger, entry.health, entry.rep)
                : entry.hint;

            var icon = FoodMealIconGenerator.LoadFoodIcon(FindIconFile(entry.assetName));
            if (icon != null)
                item.icon = icon;
            else
                Debug.LogWarning("[FOOD4U] Ícone não encontrado para " + entry.assetName +
                                 ". Rode \"Gerar ícones dos lanches FOOD4U\".");

            EditorUtility.SetDirty(item);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("FOOD4U",
            "Itens de lanchonete atualizados em " + FoodFolder + ".\n" +
            (created > 0 ? created + " asset(s) novos criados.\n" : "") +
            "Estes itens não aparecem na venda de rua (preço unitário 0).",
            "OK");
    }

    [MenuItem(MenuRoot + "Lojinha: pacotes só para revenda (desativa consumo)")]
    static void MarkResellItemsNotConsumable()
    {
        var names = new[] { "Pacoca", "Chiclete", "Biscoito", "AguaMineral", "BalaDeGoma", "Cocada" };
        var count = 0;
        foreach (var name in names)
        {
            var item = LojinhaFindItem(name);
            if (item == null)
                continue;

            item.consumable = false;
            item.hungerRestore = 0f;
            item.healthRestore = 0f;
            item.reputationRestore = 0f;
            item.consumableHint = string.Empty;
            EditorUtility.SetDirty(item);
            count++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Lojinha", count + " itens marcados como não consumíveis (só revenda).", "OK");
    }

    [MenuItem(MenuRoot + "Configurar FOOD4U (objeto selecionado ou \"FOOD4U\")")]
    static void SetupFood4USelection()
    {
        CreateFoodItemAssets();

        var target = Selection.activeGameObject;
        if (target == null)
            target = FindFirstFood4URoot();

        if (target == null)
        {
            EditorUtility.DisplayDialog(
                "FOOD4U",
                "Selecione a lanchonete FOOD4U na Hierarchy (ou nome contendo FOOD4U) e tente de novo.",
                "OK");
            return;
        }

        if (ConfigureFood4UShop(target))
        {
            EditorUtility.DisplayDialog("FOOD4U",
                "Lanchonete configurada em \"" + target.name + "\" com cardápio exclusivo.\n\n" +
                "Aperte E perto do local para abrir a loja.\nSalve a cena (Ctrl+S).",
                "OK");
            Selection.activeGameObject = target;
        }
    }

    [MenuItem(MenuRoot + "Configurar TODOS os objetos FOOD4U na cena ativa")]
    static void SetupAllFood4UInScene()
    {
        CreateFoodItemAssets();

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("FOOD4U", "Abra a cena da cidade antes de usar este menu.", "OK");
            return;
        }

        var targets = FindAllFood4UGameObjects();
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "FOOD4U",
                "Nenhum objeto com \"FOOD4U\" no nome foi encontrado na cena \"" + scene.name + "\".\n\n" +
                "Renomeie a lanchonete (ex.: FOOD4U_01) ou selecione um objeto e use o menu de seleção.",
                "OK");
            return;
        }

        var ok = 0;
        foreach (var go in targets)
        {
            if (ConfigureFood4UShop(go))
                ok++;
        }

        EditorUtility.DisplayDialog("FOOD4U",
            ok + " de " + targets.Count + " lanchonete(s) configurada(s) na cena \"" + scene.name + "\".\nSalve a cena (Ctrl+S).",
            "OK");
    }

    static void EnsureFoodFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Items/Food"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Items"))
                AssetDatabase.CreateFolder("Assets", "Items");
            AssetDatabase.CreateFolder("Assets/Items", "Food");
        }
    }

    static bool ConfigureFood4UShop(GameObject target)
    {
        if (target == null)
            return false;

        var shop = target.GetComponent<ShopZone>();
        if (shop == null)
            shop = Undo.AddComponent<ShopZone>(target);

        var products = new List<ShopZone.ShopProduct>();
        foreach (var entry in Menu)
        {
            var item = FindFoodItem(entry.assetName);
            if (item == null)
                continue;

            products.Add(new ShopZone.ShopProduct
            {
                item = item,
                packLabel = entry.label.Trim(),
                unitsPerPack = 1,
                packPriceCents = entry.priceCents,
            });
        }

        Undo.RecordObject(shop, "Configure FOOD4U");
        shop.products = products.ToArray();
        shop.shopTitle = "FOOD4U";
        shop.shopKind = ShopZone.ShopKind.FastFood;
        shop.interactDistance = 5f;
        shop.useHorizontalInteractRange = true;
        shop.useInteractTrigger = true;
        EditorUtility.SetDirty(shop);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(target.scene);
        return products.Count > 0;
    }

    static string FindIconFile(string foodAssetName)
    {
        foreach (var pair in FoodMealIconGenerator.FoodIconMap)
        {
            if (pair.assetName == foodAssetName)
                return pair.iconFile;
        }

        return null;
    }

    static List<GameObject> FindAllFood4UGameObjects()
    {
        var list = new List<GameObject>();
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
            CollectFood4URecursive(root.transform, list);

        return list;
    }

    static void CollectFood4URecursive(Transform t, List<GameObject> list)
    {
        if (t.name.IndexOf("FOOD4U", System.StringComparison.OrdinalIgnoreCase) >= 0)
            list.Add(t.gameObject);

        for (var i = 0; i < t.childCount; i++)
            CollectFood4URecursive(t.GetChild(i), list);
    }

    static GameObject FindFirstFood4URoot()
    {
        var all = FindAllFood4UGameObjects();
        return all.Count > 0 ? all[0] : null;
    }

    static ItemData FindFoodItem(string assetName)
    {
        var foodPath = FoodFolder + "/" + assetName + ".asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(foodPath);
        if (item != null)
            return item;

        return LojinhaFindItem(assetName);
    }

    static ItemData LojinhaFindItem(string assetName)
    {
        var direct = AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Items/" + assetName + ".asset");
        if (direct != null)
            return direct;

        foreach (var guid in AssetDatabase.FindAssets("t:ItemData " + assetName))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Food/"))
                continue;

            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
                return item;
        }

        return null;
    }
}
#endif
