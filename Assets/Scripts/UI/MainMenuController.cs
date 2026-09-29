using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Painéis opcionais (filhos da UI)")]
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] GameObject levelSelectPanel;
    [SerializeField] GameObject mainButtonsPanel;

    [Header("Destaque do botão selecionado")]
    [SerializeField] Color normalButtonColor = new(0.12f, 0.12f, 0.12f, 0.92f);
    [SerializeField] Color highlightedButtonColor = new(0.95f, 0.78f, 0.15f, 1f);

    [SerializeField] Button[] mainMenuButtons;
    [SerializeField] MainMenuArtLayout artLayout;

    Image[] _buttonBackgrounds;
    int _selectedIndex;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        EnsureCanvasScaler();

        if (artLayout == null)
            artLayout = GetComponent<MainMenuArtLayout>();
        if (artLayout == null)
            artLayout = gameObject.AddComponent<MainMenuArtLayout>();

        EnsureMenuMusic();
        EnsureLevelSelectPanel();

        artLayout.Apply();

        CacheButtonImages();
        WireCloseButtons();
        ApplyCreditsText();
        ShowMainButtons();
        HighlightButton(0);
    }

    void EnsureCanvasScaler()
    {
        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        scaler.matchWidthOrHeight = 0.5f;
    }

    void EnsureMenuMusic()
    {
        if (GetComponent<MainMenuMusic>() == null)
            gameObject.AddComponent<MainMenuMusic>();
    }

    void EnsureLevelSelectPanel()
    {
        MainMenuLevelSelect levelSelect = null;

        if (levelSelectPanel != null)
            levelSelect = levelSelectPanel.GetComponent<MainMenuLevelSelect>();

        if (levelSelect == null)
            levelSelect = GetComponentInChildren<MainMenuLevelSelect>(true);

        if (levelSelect == null)
        {
            var panelGo = new GameObject("Panel_EscolherCena");
            panelGo.transform.SetParent(transform, false);
            var rect = panelGo.AddComponent<RectTransform>();
            StretchPanel(rect);
            levelSelect = panelGo.AddComponent<MainMenuLevelSelect>();
        }

        levelSelect.BuildIfNeeded();
        levelSelectPanel = levelSelect.gameObject;
    }

    static void StretchPanel(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void CacheButtonImages()
    {
        if (mainMenuButtons == null || mainMenuButtons.Length == 0)
            return;

        _buttonBackgrounds = new Image[mainMenuButtons.Length];
        for (var i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] == null)
                continue;
            _buttonBackgrounds[i] = mainMenuButtons[i].GetComponent<Image>();
        }
    }

    public void HighlightButton(int index) => SelectButton(index);

    public void OnMenuButtonHoverEnter(int index)
    {
        if (artLayout != null && artLayout.UseSpriteButtons)
            artLayout.SetHovered(index);
        else
            SelectButton(index);
    }

    public void OnMenuButtonHoverExit()
    {
        if (artLayout != null)
            artLayout.ClearHover();
    }

    public void OnPlayClicked()
    {
        if (levelSelectPanel != null && levelSelectPanel.activeSelf)
            return;

        OpenSubPanel(levelSelectPanel);
    }

    public void LoadGameplayScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("MainMenuController: nome da cena vazio.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "MainMenuController: cena '" + sceneName +
                "' não está em File → Build Settings.");
            return;
        }

        MainMenuMusic.StopIfPlaying();
        GameplayReturnToMenu.ResetPersistentGameplayState();
        PlayerScenePersistence.ResetForMenuGameplayStart();
        MissionProgress.BeginNewGame(sceneName);

        if (sceneName == RecomecoSceneNames.Cidade)
            SceneTransitionState.SetNextSpawn(RecomecoSceneNames.MoradiaInicial);
        else if (sceneName == RecomecoSceneNames.FerroVelho)
            SceneTransitionState.SetNextSpawn(RecomecoSceneNames.EntradaFerroVelho);

        GameplayIntroVideo.PlayThenLoadScene(sceneName);
    }

    public void LoadFromSave()
    {
        var data = SaveGameManager.LoadFromDisk();
        if (data == null)
        {
            Debug.LogWarning("MainMenuController: nenhum save encontrado.");
            return;
        }

        if (string.IsNullOrEmpty(data.lastScene) ||
            !Application.CanStreamedLevelBeLoaded(data.lastScene))
        {
            Debug.LogError("MainMenuController: cena do save inválida ou fora do Build Settings.");
            return;
        }

        MainMenuMusic.StopIfPlaying();
        GameplayScreenFade.ForceClear();
        GameplayReturnToMenu.ResetPersistentGameplayState();
        PlayerScenePersistence.ResetForMenuGameplayStart();

        NormalizeLegacyInteriorSave(data);
        SaveGameManager.StageForLoad(data);
        if (!string.IsNullOrEmpty(data.lastSpawnId))
            SceneTransitionState.SetNextSpawn(data.lastSpawnId);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(false);
        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);

        Time.timeScale = 1f;
        SceneManager.LoadScene(data.lastScene);
    }

    static void NormalizeLegacyInteriorSave(SaveGameData data)
    {
        if (data == null)
            return;

        if (data.lastScene != RecomecoSceneNames.InteriorCasaElegante)
            return;

        data.lastScene = RecomecoSceneNames.Cidade;
        data.lastSpawnId = RecomecoSceneNames.SaidaCasaElegante;
    }

    public void OnOptionsClicked()
    {
        OpenSubPanel(optionsPanel);
    }

    public void OnCreditsClicked()
    {
        OpenSubPanel(creditsPanel);
        ResetCreditsScroll();
    }

    void ResetCreditsScroll()
    {
        if (creditsPanel == null)
            return;

        var scroll = creditsPanel.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnCloseSubPanelClicked()
    {
        ShowMainButtons();
    }

    public void OnHighlightPlay() => SelectButton(0);
    public void OnHighlightOptions() => SelectButton(1);
    public void OnHighlightCredits() => SelectButton(2);
    public void OnHighlightQuit() => SelectButton(3);

    void SelectButton(int index)
    {
        _selectedIndex = index;

        if (artLayout != null && artLayout.UseArtOverlay && artLayout.UseSpriteButtons)
        {
            artLayout.SetSelected(index);
            return;
        }

        if (_buttonBackgrounds == null)
            return;

        for (var i = 0; i < _buttonBackgrounds.Length; i++)
        {
            if (_buttonBackgrounds[i] == null)
                continue;
            _buttonBackgrounds[i].color = i == _selectedIndex ? highlightedButtonColor : normalButtonColor;
        }
    }

    void ShowMainButtons()
    {
        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(true);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(false);

        if (artLayout != null)
        {
            artLayout.SetMainButtonsVisible(true);
            artLayout.SetLogoVisible(true);
        }
    }

    void OpenSubPanel(GameObject panel)
    {
        if (panel == null)
            return;

        SetMainMenuButtonsVisible(false);
        if (optionsPanel != null && optionsPanel != panel)
            optionsPanel.SetActive(false);
        if (creditsPanel != null && creditsPanel != panel)
            creditsPanel.SetActive(false);
        if (levelSelectPanel != null && levelSelectPanel != panel)
            levelSelectPanel.SetActive(false);

        if (artLayout != null)
        {
            var showLogo = panel == optionsPanel || panel == creditsPanel;
            artLayout.SetLogoVisible(showLogo);
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    void SetMainMenuButtonsVisible(bool visible)
    {
        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(visible);

        if (artLayout != null)
            artLayout.SetMainButtonsVisible(visible);

        if (mainMenuButtons == null)
            return;

        foreach (var button in mainMenuButtons)
        {
            if (button == null)
                continue;

            button.interactable = visible;
        }
    }

    void ApplyCreditsText()
    {
        if (creditsPanel == null)
            return;

        var box = creditsPanel.transform.Find("Box") as RectTransform;
        if (box == null)
            return;

        box.sizeDelta = new Vector2(780, 620);

        var text = ResolveCreditsText(box);
        if (text == null)
            return;

        text.text = RecomecoCredits.MenuBody;
        text.fontSize = 15;
        text.lineSpacing = -2f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = new Vector4(8, 4, 8, 8);

        EnsureCreditsScrollArea(box, text);
        EnsureCreditsTextFitsScroll(text);
    }

    static TextMeshProUGUI ResolveCreditsText(RectTransform box)
    {
        var content = box.Find("CreditsScroll/Viewport/Content");
        if (content != null)
            return content.GetComponentInChildren<TextMeshProUGUI>(true);

        var legacy = box.Find("Text");
        return legacy != null ? legacy.GetComponent<TextMeshProUGUI>() : null;
    }

    static void EnsureCreditsScrollArea(RectTransform box, TextMeshProUGUI text)
    {
        const float buttonReserve = 64f;

        var scrollRoot = box.Find("CreditsScroll") as RectTransform;
        if (scrollRoot == null)
        {
            var scrollGo = new GameObject("CreditsScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollRoot = scrollGo.GetComponent<RectTransform>();
            scrollRoot.SetParent(box, false);
            scrollRoot.SetSiblingIndex(0);

            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
            scrollImage.raycastTarget = true;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewport = viewportGo.GetComponent<RectTransform>();
            viewport.SetParent(scrollRoot, false);
            StretchFull(viewport);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            var content = contentGo.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            text.transform.SetParent(content, false);
            var textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(-8f, 0f);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            scroll.content = content;
        }

        scrollRoot.anchorMin = Vector2.zero;
        scrollRoot.anchorMax = Vector2.one;
        scrollRoot.offsetMin = new Vector2(12f, buttonReserve);
        scrollRoot.offsetMax = new Vector2(-12f, -12f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
    }

    static void EnsureCreditsTextFitsScroll(TextMeshProUGUI text)
    {
        var fitter = text.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = text.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text.rectTransform.anchorMin = new Vector2(0f, 1f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);
        text.rectTransform.sizeDelta = new Vector2(0f, 0f);

        text.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
        var content = text.transform.parent as RectTransform;
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    void WireCloseButtons()
    {
        WireCloseButtonInPanel(optionsPanel);
        WireCloseButtonInPanel(creditsPanel);
        WireCloseButtonInPanel(levelSelectPanel);
    }

    void WireCloseButtonInPanel(GameObject panel)
    {
        if (panel == null)
            return;

        foreach (var tr in panel.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name != "Btn_Voltar")
                continue;

            var button = tr.GetComponent<Button>();
            if (button == null)
                continue;

            button.onClick.AddListener(OnCloseSubPanelClicked);
            return;
        }
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
