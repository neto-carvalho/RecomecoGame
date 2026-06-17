#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSetupMenu
{
    const string MenuRoot = "Recomeco/";
    const string MenuScenePath = "Assets/Scenes/MenuInicial.unity";
    const string MenuArtFolder = "Assets/UI/Menu";
    const string BackgroundCleanFileName = "menu_background_clean.png";
    const string BackgroundFileName = "menu_background.png";
    const string LogoFileName = "menu_logo.png";

    static readonly string[] BackgroundSearchPaths =
    {
        MenuArtFolder + "/" + BackgroundCleanFileName,
        MenuArtFolder + "/" + BackgroundFileName,
        "Assets/UI/Menu/tela_inicial_recomeo.png",
    };

    [MenuItem(MenuRoot + "Cenas/Criar tela inicial (MenuInicial)")]
    static void CreateMainMenuScene()
    {
        EnsureMenuArtFolder();

        if (File.Exists(MenuScenePath))
        {
            if (!EditorUtility.DisplayDialog("Recomeco",
                    "Já existe " + MenuScenePath + ".\nRecriar a UI do menu?",
                    "Recriar", "Cancelar"))
            {
                EditorSceneManager.OpenScene(MenuScenePath);
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = RecomecoSceneNames.MenuInicial;

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.12f, 0.1f);
        }

        EnsureEventSystem();
        var root = BuildMenuCanvas();
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        PutMenuFirstInBuildSettings();

        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Recomeco",
            "Cena criada: " + MenuScenePath + "\n\n" +
            "Arte recomendada em " + MenuArtFolder + ":\n" +
            "• " + BackgroundCleanFileName + " — fundo SEM logo e botões\n" +
            "• " + LogoFileName + " — logo RECOMEÇO (PNG transparente)\n" +
            "• Sprites dos botões (opcional, depois)\n\n" +
            "Os cliques usam botões invisíveis alinhados à arte.\n" +
            "Ajuste fino: componente MainMenuArtLayout no Canvas.\n\n" +
            "MenuInicial foi definida como primeira cena em Build Settings.",
            "OK");
    }

    const string ButtonsFolder = MenuArtFolder + "/Buttons";
    const string ButtonSetAssetPath = "Assets/Resources/MainMenuButtonSet.asset";
    const string MenuMusicResourcesPath = "Assets/Resources/Audio/musica_recomeco.mp3";
    const string MenuMusicSourcePath = "Assets/Audio/Menu/musica_recomeco.mp3";

    enum ArtLayoutMode
    {
        FullArtInvisible,
        ProfessionalSprites,
    }

    [MenuItem(MenuRoot + "Cenas/Gerar arte do menu (recortar PNGs)")]
    static void GenerateMenuArtFromPython()
    {
        var script = "Tools/slice_menu_art.py";
        var scriptPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, script);
        if (!File.Exists(scriptPath))
        {
            EditorUtility.DisplayDialog("Recomeco", "Script não encontrado:\n" + scriptPath, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Recomeco",
                "Gerar logo, botões e fundo limpo a partir de menu_background.png?",
                "Gerar", "Cancelar"))
            return;

        var python = "python";
        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = python,
            Arguments = "\"" + scriptPath + "\"",
            WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = System.Diagnostics.Process.Start(start);
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        AssetDatabase.Refresh();

        if (process.ExitCode != 0)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Falha ao gerar arte.\n\n" + error + "\n" + output, "OK");
            return;
        }

        EditorUtility.DisplayDialog("Recomeco",
            "Arte gerada em Assets/UI/Menu/\n\n" + output,
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Aplicar menu — arte completa (botões invisíveis)")]
    static void ApplyArtOverlayToExistingScene()
    {
        ApplyMenuLayoutMode(ArtLayoutMode.FullArtInvisible,
            "Menu com arte completa:\n" +
            "• Fundo menu_background.png\n" +
            "• Botões invisíveis alinhados\n" +
            "• Sem texto/logo duplicado\n\n" +
            "Melhor opção para ficar igual ao mockup (com ícones na arte).");
    }

    [MenuItem(MenuRoot + "Cenas/Fatiar sprites de botões (menu_buttons_sheet)")]
    static void SliceButtonSpritesMenu()
    {
        RunPythonScript("Tools/slice_menu_buttons.py", "Sprites fatiados em Assets/UI/Menu/Buttons/");
    }

    [MenuItem(MenuRoot + "Cenas/Atualizar sprites MainMenuButtonSet")]
    static void RefreshMainMenuButtonSetMenu()
    {
        var buttonSet = CreateOrUpdateButtonSetAsset();
        if (buttonSet == null || buttonSet.jogar.normal == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Sprites dos botões não encontrados.\n" +
                "Execute antes: Aplicar menu profissional ou fatiar sprites.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog("Recomeco",
            "MainMenuButtonSet atualizado com os sprites de Assets/UI/Menu/Buttons/.",
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Aplicar menu profissional (fundo + logo + botões)")]
    static void ApplyProfessionalMenuPackage()
    {
        RunPythonScript("Tools/polish_menu_logo.py", null, silent: true);
        RunPythonScript("Tools/trim_menu_button_sprites.py", null, silent: true);
        AssetDatabase.Refresh();

        var buttonSet = CreateOrUpdateButtonSetAsset();
        if (buttonSet != null && buttonSet.jogar.normal == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Sprites dos botões não foram importados.\n" +
                "Verifique Assets/UI/Menu/Buttons/ e tente de novo.",
                "OK");
            return;
        }
        if (buttonSet == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Não foi possível criar MainMenuButtonSet.\n" +
                "Confira Assets/UI/Menu/Buttons/ e menu_buttons_sheet.png.",
                "OK");
            return;
        }

        ApplyMenuLayoutMode(ArtLayoutMode.ProfessionalSprites,
            "Menu profissional aplicado:\n" +
            "• Fundo limpo\n" +
            "• Logo RECOMEÇO\n" +
            "• Botões com normal / hover / selecionado\n\n" +
            "Dê Play para testar. Ajuste posição em MainMenuArtLayout se precisar.");
    }

    static void ApplyMenuLayoutMode(ArtLayoutMode mode, string successMessage)
    {
        if (!File.Exists(MenuScenePath))
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Crie o menu antes: Recomeco → Cenas → Criar tela inicial.",
                "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(MenuScenePath);
        var menu = Object.FindFirstObjectByType<MainMenuController>();
        if (menu == null)
        {
            EditorUtility.DisplayDialog("Recomeco", "MainMenuController não encontrado na cena.", "OK");
            return;
        }

        var bgSprite = mode == ArtLayoutMode.ProfessionalSprites
            ? LoadMenuSprite(MenuArtFolder + "/" + BackgroundCleanFileName)
            : LoadMenuSprite(MenuArtFolder + "/" + BackgroundFileName);

        if (bgSprite == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Imagem de fundo não encontrada para o modo selecionado.", "OK");
            return;
        }

        ApplyBackgroundToScene(scene, bgSprite);
        ConfigureArtLayoutOnMenu(menu, mode);
        if (menu != null)
        {
            EnsureLevelSelectPanel(menu);
            EnsureMenuMusic(menu.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Recomeco", successMessage, "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Aplicar imagem de fundo do menu (se existir em UI/Menu)")]
    static void ApplyBackgroundFromFolder()
    {
        var sprite = LoadMenuBackgroundSprite();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Coloque um PNG em:\n" + MenuArtFolder + "/" + BackgroundFileName,
                "OK");
            return;
        }

        if (!File.Exists(MenuScenePath))
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Crie o menu antes: Recomeco → Cenas → Criar tela inicial.",
                "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(MenuScenePath);
        ApplyBackgroundToScene(scene, sprite);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Recomeco", "Fundo do menu atualizado.", "OK");
    }

    static void ApplyBackgroundToScene(Scene scene, Sprite spriteOverride = null)
    {
        var bg = GameObject.Find("Background")?.GetComponent<Image>();
        if (bg == null)
            return;

        var sprite = spriteOverride ?? LoadMenuBackgroundSprite();
        if (sprite == null)
            return;

        bg.sprite = sprite;
        bg.color = Color.white;
    }

    [MenuItem(MenuRoot + "Cenas/MenuInicial como primeira cena (Build Settings)")]
    static void MenuFirstBuildSettingsMenu()
    {
        PutMenuFirstInBuildSettings();
        EditorUtility.DisplayDialog("Recomeco", "Build Settings atualizado.", "OK");
    }

    static void EnsureMenuArtFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/UI"))
            AssetDatabase.CreateFolder("Assets", "UI");
        if (!AssetDatabase.IsValidFolder(MenuArtFolder))
            AssetDatabase.CreateFolder("Assets/UI", "Menu");
    }

    static GameObject BuildMenuCanvas()
    {
        var canvasGo = new GameObject("Canvas_Menu");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var menu = canvasGo.AddComponent<MainMenuController>();

        var bg = CreateStretchImage(canvasGo.transform, "Background", new Color(0.2f, 0.18f, 0.16f, 1f));
        var bgSprite = LoadMenuBackgroundSprite();
        if (bgSprite != null)
        {
            bg.sprite = bgSprite;
            bg.color = Color.white;
        }

        var logo = CreateLogoImage(canvasGo.transform);

        var buttonsPanel = new GameObject("MainButtons");
        buttonsPanel.transform.SetParent(canvasGo.transform, false);
        var panelRect = buttonsPanel.AddComponent<RectTransform>();
        StretchFull(panelRect);
        var layout = buttonsPanel.AddComponent<VerticalLayoutGroup>();
        layout.enabled = false;

        var btnPlay = CreateOverlayMenuButton(buttonsPanel.transform, "JOGAR");
        var btnOpt = CreateOverlayMenuButton(buttonsPanel.transform, "OPÇÕES");
        var btnCred = CreateOverlayMenuButton(buttonsPanel.transform, "CRÉDITOS");
        var btnQuit = CreateOverlayMenuButton(buttonsPanel.transform, "SAIR");

        var optionsPanel = CreateSubPanel(canvasGo.transform, "Panel_Opcoes",
            "OPÇÕES\n\nEm breve: volume, qualidade gráfica e controles.");
        var creditsPanel = CreateSubPanel(canvasGo.transform, "Panel_Creditos", RecomecoCredits.MenuBody, 540f);

        EnsureLevelSelectPanel(menu);
        WireMenu(menu, btnPlay, btnOpt, btnCred, btnQuit, buttonsPanel, optionsPanel, creditsPanel);
        ConfigureArtLayoutOnMenu(menu, ArtLayoutMode.FullArtInvisible, logo.gameObject, buttonsPanel,
            btnPlay, btnOpt, btnCred, btnQuit);
        EnsureMenuMusic(canvasGo);

        return canvasGo;
    }

    static Image CreateLogoImage(Transform parent)
    {
        var logo = CreateStretchImage(parent, "Logo", Color.white);
        var logoRect = logo.rectTransform;
        logoRect.anchorMin = new Vector2(0.5f, 0.58f);
        logoRect.anchorMax = new Vector2(0.5f, 0.58f);
        logoRect.pivot = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = new Vector2(920, 240);
        logo.preserveAspect = true;
        logo.raycastTarget = false;

        var sprite = LoadMenuSprite(MenuArtFolder + "/" + LogoFileName);
        if (sprite != null)
        {
            logo.sprite = sprite;
            logo.gameObject.SetActive(true);
        }
        else
        {
            logo.gameObject.SetActive(false);
        }

        return logo;
    }

    static void ConfigureArtLayoutOnMenu(
        MainMenuController menu,
        ArtLayoutMode mode = ArtLayoutMode.FullArtInvisible,
        GameObject logoObject = null,
        GameObject buttonsPanel = null,
        Button btnPlay = null,
        Button btnOpt = null,
        Button btnCred = null,
        Button btnQuit = null)
    {
        var art = menu.GetComponent<MainMenuArtLayout>();
        if (art == null)
            art = menu.gameObject.AddComponent<MainMenuArtLayout>();

        logoObject ??= menu.transform.Find("Logo")?.gameObject;
        buttonsPanel ??= menu.transform.Find("MainButtons")?.gameObject;

        if (btnPlay == null && buttonsPanel != null)
        {
            btnPlay = buttonsPanel.transform.Find("Btn_JOGAR")?.GetComponent<Button>();
            btnOpt = buttonsPanel.transform.Find("Btn_OPÇÕES")?.GetComponent<Button>()
                ?? buttonsPanel.transform.Find("Btn_OPCOES")?.GetComponent<Button>();
            btnCred = buttonsPanel.transform.Find("Btn_CRÉDITOS")?.GetComponent<Button>()
                ?? buttonsPanel.transform.Find("Btn_CREDITOS")?.GetComponent<Button>();
            btnQuit = buttonsPanel.transform.Find("Btn_SAIR")?.GetComponent<Button>();
        }

        var useSprites = mode == ArtLayoutMode.ProfessionalSprites;
        var logoSprite = useSprites ? LoadMenuSprite(MenuArtFolder + "/" + LogoFileName) : null;
        var buttonSet = useSprites ? CreateOrUpdateButtonSetAsset() : null;

        var so = new SerializedObject(art);
        so.FindProperty("useArtOverlay").boolValue = true;
        so.FindProperty("useSpriteButtons").boolValue = useSprites;
        so.FindProperty("buttonSet").objectReferenceValue = buttonSet;
        so.FindProperty("logoObject").objectReferenceValue = logoObject;
        so.FindProperty("logoImage").objectReferenceValue = logoObject != null
            ? logoObject.GetComponent<Image>()
            : null;
        so.FindProperty("logoSprite").objectReferenceValue = logoSprite;
        so.FindProperty("mainButtonsPanel").objectReferenceValue = buttonsPanel;
        so.FindProperty("optionsPanel").objectReferenceValue =
            menu.transform.Find("Panel_Opcoes")?.gameObject;
        so.FindProperty("creditsPanel").objectReferenceValue =
            menu.transform.Find("Panel_Creditos")?.gameObject;
        so.FindProperty("levelSelectPanel").objectReferenceValue =
            menu.transform.Find("Panel_EscolherCena")?.gameObject;
        so.FindProperty("menuButtons").arraySize = 4;
        so.FindProperty("menuButtons").GetArrayElementAtIndex(0).objectReferenceValue = btnPlay;
        so.FindProperty("menuButtons").GetArrayElementAtIndex(1).objectReferenceValue = btnOpt;
        so.FindProperty("menuButtons").GetArrayElementAtIndex(2).objectReferenceValue = btnCred;
        so.FindProperty("menuButtons").GetArrayElementAtIndex(3).objectReferenceValue = btnQuit;
        so.ApplyModifiedPropertiesWithoutUndo();

        var menuSo = new SerializedObject(menu);
        menuSo.FindProperty("artLayout").objectReferenceValue = art;
        menuSo.ApplyModifiedPropertiesWithoutUndo();

        if (useSprites)
            HideButtonLabels(btnPlay, btnOpt, btnCred, btnQuit);
        else
        {
            MakeButtonOverlay(btnPlay);
            MakeButtonOverlay(btnOpt);
            MakeButtonOverlay(btnCred);
            MakeButtonOverlay(btnQuit);
        }

        HideLogoPlaceholderText(logoObject);
        if (logoObject != null)
            logoObject.SetActive(useSprites && logoSprite != null);

        if (!Application.isPlaying)
            art.Apply();
    }

    static void HideButtonLabels(
        Button btnPlay,
        Button btnOpt,
        Button btnCred,
        Button btnQuit)
    {
        MakeButtonOverlay(btnPlay);
        MakeButtonOverlay(btnOpt);
        MakeButtonOverlay(btnCred);
        MakeButtonOverlay(btnQuit);

        foreach (var button in new[] { btnPlay, btnOpt, btnCred, btnQuit })
        {
            if (button == null)
                continue;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = Color.white;
            button.transition = Selectable.Transition.None;
        }
    }

    static MainMenuButtonSet CreateOrUpdateButtonSetAsset()
    {
        AssetDatabase.Refresh();
        EnsureSpriteImport(ButtonsFolder + "/jogar_normal.png");
        EnsureSpriteImport(ButtonsFolder + "/jogar_selected.png");
        EnsureSpriteImport(ButtonsFolder + "/opcoes_normal.png");
        EnsureSpriteImport(ButtonsFolder + "/opcoes_hover.png");
        EnsureSpriteImport(ButtonsFolder + "/creditos_normal.png");
        EnsureSpriteImport(ButtonsFolder + "/creditos_hover.png");
        EnsureSpriteImport(ButtonsFolder + "/sair_normal.png");
        EnsureSpriteImport(ButtonsFolder + "/sair_hover.png");
        EnsureSpriteImport(MenuArtFolder + "/" + LogoFileName);
        EnsureSpriteImport(MenuArtFolder + "/" + BackgroundCleanFileName);

        var asset = AssetDatabase.LoadAssetAtPath<MainMenuButtonSet>(ButtonSetAssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<MainMenuButtonSet>();
            AssetDatabase.CreateAsset(asset, ButtonSetAssetPath);
        }

        var so = new SerializedObject(asset);
        AssignButtonEntry(so, "jogar", "jogar_normal", "jogar_selected", "jogar_selected");
        AssignButtonEntry(so, "opcoes", "opcoes_normal", "opcoes_hover", null);
        AssignButtonEntry(so, "creditos", "creditos_normal", "creditos_hover", null);
        AssignButtonEntry(so, "sair", "sair_normal", "sair_hover", null);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    static void AssignButtonEntry(
        SerializedObject so,
        string entryName,
        string normalFile,
        string hoverFile,
        string selectedFile)
    {
        var entry = so.FindProperty(entryName);
        AssignSpriteIfFound(entry.FindPropertyRelative("normal"), ButtonsFolder + "/" + normalFile);
        AssignSpriteIfFound(entry.FindPropertyRelative("hover"), ButtonsFolder + "/" + hoverFile);
        if (string.IsNullOrEmpty(selectedFile))
            entry.FindPropertyRelative("selected").objectReferenceValue = null;
        else
            AssignSpriteIfFound(entry.FindPropertyRelative("selected"), ButtonsFolder + "/" + selectedFile);
    }

    static void AssignSpriteIfFound(SerializedProperty property, string assetPath)
    {
        var sprite = LoadMenuSprite(assetPath);
        if (sprite != null)
            property.objectReferenceValue = sprite;
    }

    static void EnsureSpriteImport(string assetPath)
    {
        if (!File.Exists(assetPath))
            return;
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    static void RunPythonScript(string relativeScript, string successMessage, bool silent = false)
    {
        var scriptPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativeScript);
        if (!File.Exists(scriptPath))
        {
            if (!silent)
                EditorUtility.DisplayDialog("Recomeco", "Script não encontrado:\n" + scriptPath, "OK");
            return;
        }

        var start = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python",
            Arguments = "\"" + scriptPath + "\"",
            WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = System.Diagnostics.Process.Start(start);
        process.WaitForExit();
        if (!silent && process.ExitCode != 0)
        {
            EditorUtility.DisplayDialog("Recomeco",
                process.StandardError.ReadToEnd() + process.StandardOutput.ReadToEnd(), "OK");
            return;
        }

        if (!string.IsNullOrEmpty(successMessage) && !silent)
            EditorUtility.DisplayDialog("Recomeco", successMessage, "OK");
    }

    static void HideLogoPlaceholderText(GameObject logoObject)
    {
        if (logoObject == null)
            return;

        foreach (var label in logoObject.GetComponentsInChildren<TextMeshProUGUI>(true))
            label.gameObject.SetActive(false);
    }

    static void MakeButtonOverlay(Button button)
    {
        if (button == null)
            return;

        foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            label.gameObject.SetActive(false);

        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = new Color(1f, 1f, 1f, 0f);
    }

    static void WireMenu(
        MainMenuController menu,
        Button btnPlay,
        Button btnOpt,
        Button btnCred,
        Button btnQuit,
        GameObject buttonsPanel,
        GameObject optionsPanel,
        GameObject creditsPanel)
    {
        var so = new SerializedObject(menu);
        so.FindProperty("mainButtonsPanel").objectReferenceValue = buttonsPanel;
        so.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
        so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        so.FindProperty("levelSelectPanel").objectReferenceValue =
            menu.transform.Find("Panel_EscolherCena")?.gameObject;
        so.FindProperty("mainMenuButtons").arraySize = 4;
        so.FindProperty("mainMenuButtons").GetArrayElementAtIndex(0).objectReferenceValue = btnPlay;
        so.FindProperty("mainMenuButtons").GetArrayElementAtIndex(1).objectReferenceValue = btnOpt;
        so.FindProperty("mainMenuButtons").GetArrayElementAtIndex(2).objectReferenceValue = btnCred;
        so.FindProperty("mainMenuButtons").GetArrayElementAtIndex(3).objectReferenceValue = btnQuit;
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(btnPlay.onClick, menu.OnPlayClicked);
        UnityEventTools.AddPersistentListener(btnOpt.onClick, menu.OnOptionsClicked);
        UnityEventTools.AddPersistentListener(btnCred.onClick, menu.OnCreditsClicked);
        UnityEventTools.AddPersistentListener(btnQuit.onClick, menu.OnQuitClicked);

        var closeOpt = FindBackButton(optionsPanel);
        var closeCred = FindBackButton(creditsPanel);
        var closeLevel = FindBackButton(menu.transform.Find("Panel_EscolherCena")?.gameObject);
        if (closeOpt != null)
            UnityEventTools.AddPersistentListener(closeOpt.onClick, menu.OnCloseSubPanelClicked);
        if (closeCred != null)
            UnityEventTools.AddPersistentListener(closeCred.onClick, menu.OnCloseSubPanelClicked);
        if (closeLevel != null)
            UnityEventTools.AddPersistentListener(closeLevel.onClick, menu.OnCloseSubPanelClicked);

        AddHighlight(btnPlay, menu, 0);
        AddHighlight(btnOpt, menu, 1);
        AddHighlight(btnCred, menu, 2);
        AddHighlight(btnQuit, menu, 3);
    }

    static Button FindBackButton(GameObject panel)
    {
        foreach (var tr in panel.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name != "Btn_Voltar")
                continue;
            return tr.GetComponent<Button>();
        }

        return null;
    }

    static void AddHighlight(Button btn, MainMenuController menu, int index)
    {
        var h = btn.gameObject.AddComponent<MainMenuButtonHighlight>();
        var so = new SerializedObject(h);
        so.FindProperty("menu").objectReferenceValue = menu;
        so.FindProperty("buttonIndex").intValue = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Button CreateOverlayMenuButton(Transform parent, string label)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(660, 64);
        return btn;
    }

    static Button CreateMenuButton(Transform parent, string label, int layoutHeight)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = layoutHeight == 0 ? 72 : 64;

        var img = go.AddComponent<Image>();
        img.color = layoutHeight == 0
            ? new Color(0.95f, 0.78f, 0.15f, 1f)
            : new Color(0.12f, 0.12f, 0.12f, 0.92f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(480, le.preferredHeight);

        var textRect = new GameObject("Text").AddComponent<RectTransform>();
        textRect.SetParent(go.transform, false);
        StretchFull(textRect);
        AddTmp(textRect, label, 28, FontStyles.Bold);

        return btn;
    }

    static GameObject CreateSubPanel(Transform parent, string name, string body, float boxHeight = 400f)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rect = panel.AddComponent<RectTransform>();
        StretchFull(rect);

        var dim = CreateStretchImage(panel.transform, "Dim", new Color(0, 0, 0, 0.65f));
        StretchFull(dim.rectTransform);

        var box = new GameObject("Box");
        box.transform.SetParent(panel.transform, false);
        var boxRect = box.AddComponent<RectTransform>();
        StretchCenter(boxRect, new Vector2(0.5f, 0.5f), new Vector2(760, boxHeight));
        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var textRect = new GameObject("Text").AddComponent<RectTransform>();
        textRect.SetParent(box.transform, false);
        StretchFull(textRect);
        var isCredits = name == "Panel_Creditos";
        var tmp = AddTmp(textRect, body, isCredits ? 18 : 26, FontStyles.Normal);
        tmp.alignment = isCredits ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Center;
        tmp.lineSpacing = isCredits ? 2f : 0f;
        tmp.margin = isCredits ? new Vector4(28, 28, 28, 88) : new Vector4(24, 24, 24, 80);

        var back = CreateMenuButton(box.transform, "VOLTAR", 1);
        back.name = "Btn_Voltar";
        var backRect = back.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0, 20);
        backRect.sizeDelta = new Vector2(220, 48);
        var backLe = back.gameObject.GetComponent<LayoutElement>();
        if (backLe != null)
            Object.DestroyImmediate(backLe);

        panel.SetActive(false);
        return panel;
    }

    static Image CreateStretchImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        StretchFull(rect);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI AddTmpChild(RectTransform parent, string text, float size, FontStyles style)
    {
        var tr = new GameObject("Text").AddComponent<RectTransform>();
        tr.SetParent(parent, false);
        StretchFull(tr);
        return AddTmp(tr, text, size, style);
    }

    static TextMeshProUGUI AddTmp(RectTransform parent, string text, float size, FontStyles style)
    {
        var tmp = parent.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        return tmp;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void StretchCenter(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static Sprite LoadMenuBackgroundSprite()
    {
        foreach (var path in BackgroundSearchPaths)
        {
            var sprite = LoadMenuSprite(path);
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    static Sprite LoadMenuSprite(string path)
    {
        path = NormalizeAssetPath(path);
        if (!File.Exists(path))
            return null;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static string NormalizeAssetPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            path += ".png";

        return path.Replace('\\', '/');
    }

    static void EnsureLevelSelectPanel(MainMenuController menu)
    {
        if (menu == null)
            return;

        var existing = menu.transform.Find("Panel_EscolherCena");
        GameObject panelGo;
        MainMenuLevelSelect levelSelect;

        if (existing != null)
        {
            panelGo = existing.gameObject;
            levelSelect = panelGo.GetComponent<MainMenuLevelSelect>()
                ?? panelGo.AddComponent<MainMenuLevelSelect>();
        }
        else
        {
            panelGo = new GameObject("Panel_EscolherCena");
            panelGo.transform.SetParent(menu.transform, false);
            var rect = panelGo.AddComponent<RectTransform>();
            StretchFull(rect);
            levelSelect = panelGo.AddComponent<MainMenuLevelSelect>();
        }

        levelSelect.BuildIfNeeded();
        panelGo.SetActive(false);

        var so = new SerializedObject(menu);
        so.FindProperty("levelSelectPanel").objectReferenceValue = panelGo;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureMenuMusic(GameObject canvas)
    {
        if (canvas == null)
            return;

        EnsureMenuMusicImport(MenuMusicResourcesPath);
        EnsureMenuMusicImport(MenuMusicSourcePath);

        var music = canvas.GetComponent<MainMenuMusic>();
        if (music == null)
            music = canvas.AddComponent<MainMenuMusic>();

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicResourcesPath)
            ?? AssetDatabase.LoadAssetAtPath<AudioClip>(MenuMusicSourcePath);

        var so = new SerializedObject(music);
        if (clip != null)
            so.FindProperty("menuMusic").objectReferenceValue = clip;
        so.FindProperty("volume").floatValue = 0.65f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureMenuMusicImport(string assetPath)
    {
        if (!File.Exists(assetPath))
            return;

        var importer = AssetImporter.GetAtPath(assetPath) as UnityEditor.AudioImporter;
        if (importer == null)
            return;

        var settings = importer.defaultSampleSettings;
        settings.loadType = UnityEngine.AudioClipLoadType.Streaming;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.loadInBackground = true;
        importer.SaveAndReimport();
    }

    public static void PutMenuFirstInBuildSettings()
    {
        var paths = new System.Collections.Generic.List<string>();

        void AddUnique(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;
            paths.Remove(path);
            paths.Insert(0, path);
        }

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path) && !paths.Contains(s.path))
                paths.Add(s.path);
        }

        AddUnique(MenuScenePath);
        AddUnique("Assets/Scenes/Cidade.unity");
        AddUnique("Assets/Scenes/FerroVelho.unity");

        paths.RemoveAll(p => p != null && (
            p.Replace('\\', '/').EndsWith("/Flat_Style_Vehicles/Demo/Demo.unity", System.StringComparison.OrdinalIgnoreCase) ||
            p.Replace('\\', '/').EndsWith("/Demo/Demo.unity", System.StringComparison.OrdinalIgnoreCase)));

        var scenes = new EditorBuildSettingsScene[paths.Count];
        for (var i = 0; i < paths.Count; i++)
            scenes[i] = new EditorBuildSettingsScene(paths[i], true);

        EditorBuildSettings.scenes = scenes;
    }
}
#endif
