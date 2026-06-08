using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela inicial: Jogar, Opções, Créditos, Sair.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Cena ao clicar em Jogar")]
    [SerializeField] string gameplaySceneName = RecomecoSceneNames.Cidade;

    [Header("Painéis opcionais (filhos da UI)")]
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject creditsPanel;
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

        if (artLayout == null)
            artLayout = GetComponent<MainMenuArtLayout>();
        if (artLayout == null)
            artLayout = gameObject.AddComponent<MainMenuArtLayout>();

        artLayout.Apply();

        CacheButtonImages();
        WireCloseButtons();
        ApplyCreditsText();
        ShowMainButtons();
        HighlightButton(0);
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
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("MainMenuController: defina o nome da cena de jogo (ex.: Cidade).");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError(
                "MainMenuController: cena '" + gameplaySceneName +
                "' não está em File → Build Settings.");
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
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

        panel.SetActive(true);
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
