using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Cria prefab placeholder de skate e liga em RecomecoGameplaySettings.</summary>
public static class PlayerSkateSetupMenu
{
    const string PrefabPath = "Assets/Prefabs/Player/SkatePlaceholder.prefab";

    const string SkateItemPath = "Assets/Items/Skate.asset";

    [MenuItem("Recomeco/Player/Skate — criar item no inventário")]
    public static void EnsureSkateItemAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ItemData>(SkateItemPath);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Recomeco", "Item já existe: " + SkateItemPath, "OK");
            return;
        }

        var item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = PlayerSkateItems.ItemName;
        item.unitSellPriceCents = 0;
        item.consumable = false;
        item.gearItem = true;
        item.consumableHint = "Equipamento urbano. B na cidade para montar/desmontar.";

        if (!AssetDatabase.IsValidFolder("Assets/Items"))
            AssetDatabase.CreateFolder("Assets", "Items");

        AssetDatabase.CreateAsset(item, SkateItemPath);

        var settings = Resources.Load<RecomecoGameplaySettings>("RecomecoGameplaySettings");
        if (settings != null)
        {
            settings.skateItem = item;
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco",
            "Criado " + SkateItemPath + "\n\nOpcional: arraste um Sprite de ícone no ItemData.",
            "OK");
    }

    [MenuItem("Recomeco/Player/Criar prefab placeholder do skate")]
    public static void CreateSkatePlaceholderPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Player"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Player");

        var root = new GameObject("SkatePlaceholder");
        var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Deck";
        deck.transform.SetParent(root.transform, false);
        deck.transform.localScale = new Vector3(0.55f, 0.06f, 0.18f);
        deck.transform.localPosition = new Vector3(0f, 0.03f, 0.02f);
        Object.DestroyImmediate(deck.GetComponent<Collider>());

        var wheelOffsets = new[]
        {
            new Vector3(-0.2f, 0f, -0.05f),
            new Vector3(0.2f, 0f, -0.05f),
            new Vector3(-0.2f, 0f, 0.12f),
            new Vector3(0.2f, 0f, 0.12f),
        };

        foreach (var offset in wheelOffsets)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(root.transform, false);
            wheel.transform.localScale = new Vector3(0.08f, 0.03f, 0.08f);
            wheel.transform.localPosition = offset;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Object.DestroyImmediate(wheel.GetComponent<Collider>());
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        var settings = Resources.Load<RecomecoGameplaySettings>("RecomecoGameplaySettings");
        if (settings != null)
        {
            settings.skateVisualPrefab = prefab;
            settings.skateVisualLocalPosition = new Vector3(0f, 0.02f, 0.08f);
            settings.skateVisualLocalEuler = new Vector3(0f, 0f, 0f);
            settings.skateVisualLocalScale = Vector3.one;
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco — Skate",
            "Prefab criado em:\n" + PrefabPath +
            "\n\nRecomecoGameplaySettings.skateVisualPrefab foi atualizado.\n" +
            "Substitua depois por um modelo da Asset Store (low poly skate).",
            "OK");
    }

    const string SkateIconPath = "Assets/Items/Icons/icon_skate.png";
    const string SirBikeSkatePrefabPath = "Assets/Sir_bike/Prefabs/Skateboard.prefab";

    [MenuItem("Recomeco/Player/Skate — usar prefab Sir_bike (Bicycle pack)")]
    public static void UseSirBikeSkatePrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SirBikeSkatePrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Recomeco",
                "Prefab não encontrado:\n" + SirBikeSkatePrefabPath,
                "OK");
            return;
        }

        var settings = Resources.Load<RecomecoGameplaySettings>("RecomecoGameplaySettings");
        if (settings != null)
        {
            settings.skateVisualPrefab = prefab;
            settings.skateVisualLocalPosition = new Vector3(0f, 0f, 0.02f);
            settings.skateVisualLocalEuler = Vector3.zero;
            settings.skateVisualLocalScale = Vector3.one;
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco",
            "Skateboard Sir_bike ligado ao RecomecoGameplaySettings.\n\n" +
            "Play → B na cidade. Escala padrão = 1.\n" +
            "Rosa no Editor? Recomeco → Assets → Sir_bike — converter materiais para URP.",
            "OK");
    }

    [MenuItem("Recomeco/Player/Skate — ícone do inventário + prefab visual")]
    public static void SetupSkateIconAndVisualPrefab()
    {
        CreateSkatePlaceholderPrefab();

        var item = AssetDatabase.LoadAssetAtPath<ItemData>(SkateItemPath);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkateIconPath);
        if (item != null && sprite != null)
        {
            item.icon = sprite;
            EditorUtility.SetDirty(item);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco",
            "Ícone (icon_skate.png) ligado ao ItemData.\nPrefab visual no RecomecoGameplaySettings.\n\n" +
            "Se o ícone não aparecer, selecione icon_skate.png e confira Texture Type = Sprite.",
            "OK");
    }

    [MenuItem("Recomeco/Player/Skate — guia Mixamo (ler antes de montar)")]
    public static void ShowMixamoGuide()
    {
        EditorUtility.DisplayDialog(
            "Skate — animações primeiro",
            "1) Mixamo.com (grátis): baixe clips IN PLACE, Humanoid, sem skin:\n" +
            "   • Skateboard Push (remar)\n" +
            "   • Skateboard Idle / Ride Loop\n" +
            "   • Skateboard Jump / Ollie\n\n" +
            "2) Coloque os .fbx em Assets/Animations/Skate/\n" +
            "3) Rig: Humanoid, Avatar Definition = Create From This Model (Mixamo sem skin)\n" +
            "4) Crie Animator Controller com parâmetros:\n" +
            "   Hor, Vert, Speed (float), IsJump (bool), Push (trigger)\n" +
            "5) Blend tree ride por Speed; transição Push ao começar a andar\n" +
            "6) Arraste o controller em RecomecoGameplaySettings → skateMovementController\n" +
            "7) Prefab visual do skate no osso LeftFoot/RightFoot se possível\n\n" +
            "Sem passo 6, o jogador só DESBLOQUEIA o skate; B avisa que faltam animações.",
            "OK");
    }

    [MenuItem("Recomeco/Player/Skate — criar pasta Assets/Animations/Skate")]
    public static void EnsureSkateAnimationFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Skate"))
            AssetDatabase.CreateFolder("Assets/Animations", "Skate");

        EditorUtility.DisplayDialog("Recomeco", "Pasta pronta: Assets/Animations/Skate", "OK");
    }

    const string SkateSoundLibraryPath = "Assets/Audio/SkateSoundLibrary.asset";
    const string SkateSoundResourcesPath = "Assets/Resources/Recomeco/SkateSoundLibrary.asset";
    const string TileEssentialsRoot = "Assets/Footsteps - Essentials/Footsteps_Tile";

    [MenuItem("Recomeco/Player/Skate — configurar sons (rodas + pulo)")]
    public static void SetupSkateSounds()
    {
        var library = GetOrCreateSkateSoundLibrary();
        if (library == null)
        {
            EditorUtility.DisplayDialog(
                "Recomeco",
                "Não foi possível criar a biblioteca de sons.\nVerifique se existe «Footsteps - Essentials».",
                "OK");
            return;
        }

        PopulateSkateSoundsFromEssentials(library);
        EnsureSkateSoundResourcesCopy(library);

        var settings = Resources.Load<RecomecoGameplaySettings>("RecomecoGameplaySettings");
        if (settings != null)
        {
            settings.skateSoundLibrary = library;
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco",
            "Sons de skate configurados (rodas + pulo/aterrizagem).\n\n" +
            "Ligados em RecomecoGameplaySettings → skateSoundLibrary.\n" +
            "Play → B na cidade para ouvir.",
            "OK");
    }

    static SkateSoundLibrary GetOrCreateSkateSoundLibrary()
    {
        var library = AssetDatabase.LoadAssetAtPath<SkateSoundLibrary>(SkateSoundLibraryPath);
        if (library != null)
            return library;

        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");

        library = ScriptableObject.CreateInstance<SkateSoundLibrary>();
        AssetDatabase.CreateAsset(library, SkateSoundLibraryPath);
        return library;
    }

    static void EnsureSkateSoundResourcesCopy(SkateSoundLibrary library)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Recomeco"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Recomeco");
        }

        var existing = AssetDatabase.LoadAssetAtPath<SkateSoundLibrary>(SkateSoundResourcesPath);
        if (existing == library)
            return;

        if (existing != null)
            AssetDatabase.DeleteAsset(SkateSoundResourcesPath);

        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(library), SkateSoundResourcesPath);
    }

    static void PopulateSkateSoundsFromEssentials(SkateSoundLibrary library)
    {
        var runFolder = TileEssentialsRoot + "/Footsteps_Tile_Run";
        var jumpFolder = TileEssentialsRoot + "/Footsteps_Tile_Jump";

        library.rollTicks = LoadAudioClipsInFolder(runFolder);
        library.rollLoop = null;

        library.jumpStart = LoadAudioClipsInFolder(jumpFolder, name => name.Contains("Jump_Start"));
        library.jumpLand = LoadAudioClipsInFolder(jumpFolder, name => name.Contains("Jump_Land"));

        if (library.jumpStart == null || library.jumpStart.Length == 0)
            library.jumpStart = LoadAudioClipsInFolder(jumpFolder);

        library.rollVolume = 0.42f;
        library.rollMinSpeed = 0.35f;
        library.rollMaxSpeed = 9f;
        library.rollTickMinInterval = 0.1f;
        library.rollTickMaxInterval = 0.32f;

        EditorUtility.SetDirty(library);
    }

    static AudioClip PickClip(string folder, string fileNameWithoutExtension)
    {
        var path = folder + "/" + fileNameWithoutExtension + ".wav";
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static AudioClip[] LoadAudioClipsInFolder(string folder, System.Predicate<string> nameFilter = null)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return System.Array.Empty<AudioClip>();

        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folder });
        var list = new List<AudioClip>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var file = Path.GetFileNameWithoutExtension(path);
            if (nameFilter != null && !nameFilter(file))
                continue;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                list.Add(clip);
        }

        return list.ToArray();
    }

    [MenuItem("Recomeco/Player/Adicionar PlayerSkateController no Player da cena")]
    public static void AddSkateComponentToScenePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Recomeco", "Nenhum objeto com tag Player na cena.", "OK");
            return;
        }

        if (player.GetComponent<PlayerSkateController>() == null)
            Undo.AddComponent<PlayerSkateController>(player);

        EditorUtility.DisplayDialog("Recomeco", "PlayerSkateController adicionado ao Player.", "OK");
    }
}