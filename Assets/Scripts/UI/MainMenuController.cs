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
        GameplayIntroVideo.PlayThenLoadScene(sceneName);
    }

    public void OnOptionsClicked()
    {
        OpenSubPanel(optionsPanel);
    }

    public void OnCreditsClicked()
    {
        OpenSubPanel(creditsPanel);
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
            artLayout.SetLogoVisible(true);
    }

    void OpenSubPanel(GameObject panel)
    {
        if (panel == null)
            return;

        if (mainButtonsPanel != null)
            mainButtonsPanel.SetActive(false);
        if (optionsPanel != null && optionsPanel != panel)
            optionsPanel.SetActive(false);
        if (creditsPanel != null && creditsPanel != panel)
            creditsPanel.SetActive(false);
        if (levelSelectPanel != null && levelSelectPanel != panel)
            levelSelectPanel.SetActive(false);

        if (artLayout != null)
            artLayout.SetLogoVisible(panel == optionsPanel || panel == creditsPanel);

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    void ApplyCreditsText()
    {
        if (creditsPanel == null)
            return;

        var text = creditsPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null)
            return;

        text.text = RecomecoCredits.MenuBody;
        text.fontSize = 18;
        text.lineSpacing = 2f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.margin = new Vector4(28, 28, 28, 88);

        if (text.transform.parent is RectTransform box)
            box.sizeDelta = new Vector2(760, 540);
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
}
