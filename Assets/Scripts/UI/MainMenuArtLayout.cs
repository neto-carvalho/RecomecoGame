using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Layout do menu com fundo limpo, logo e sprites de botão (normal / hover / selecionado).
/// </summary>
[DisallowMultipleComponent]
public class MainMenuArtLayout : MonoBehaviour
{
    [Header("Arte")]
    [SerializeField] bool useArtOverlay = true;
    [SerializeField] bool useSpriteButtons = true;
    [SerializeField] MainMenuButtonSet buttonSet;
    [SerializeField] GameObject logoObject;
    [SerializeField] Image logoImage;
    [SerializeField] Sprite logoSprite;
    [SerializeField] GameObject mainButtonsPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject creditsPanel;
    [SerializeField] Button[] menuButtons;

    [Header("Posição (referência 1920×1080)")]
    [SerializeField] float buttonWidth = 450f;
    [SerializeField] float buttonVerticalGap = 20f;
    [SerializeField] float buttonSlotPadding = 4f;
    [SerializeField] float topButtonAnchorY = 0.39f;
    [SerializeField] bool autoFitButtonStack = true;
    [SerializeField] float bottomMargin = 44f;
    [SerializeField] float gapBelowLogo = 18f;
    [SerializeField] float minButtonWidth = 400f;

    [Header("Logo (referência 1920×1080)")]
    [SerializeField] Vector2 logoAnchor = new(0.5f, 0.72f);
    [SerializeField] float logoWidth = 860f;

    int _selectedIndex;
    int _hoveredIndex = -1;

    public bool UseArtOverlay => useArtOverlay;
    public bool UseSpriteButtons => useSpriteButtons;

    public void Apply()
    {
        if (!useArtOverlay)
            return;

        AutoWireReferences();
        EnsureButtonSetPopulated();
        ConfigureLogo();
        ConfigureButtonsPanel();
        ConfigureButtons();
        SortCanvasLayers();
        RefreshButtonVisuals();
    }

    void Start()
    {
        Apply();
    }

    void EnsureButtonSetPopulated()
    {
        if (buttonSet == null)
            buttonSet = Resources.Load<MainMenuButtonSet>("MainMenuButtonSet");

        if (buttonSet != null && buttonSet.jogar.normal != null)
            return;

        Debug.LogWarning(
            "MainMenuArtLayout: MainMenuButtonSet sem sprites. " +
            "Use Recomeco → Cenas → Aplicar menu profissional.");
    }

    public void SetSelected(int index)
    {
        _selectedIndex = index;
        RefreshButtonVisuals();
    }

    public void SetHovered(int index)
    {
        _hoveredIndex = index;
        RefreshButtonVisuals();
        BringButtonToFront(index);
    }

    public void ClearHover()
    {
        _hoveredIndex = -1;
        RefreshButtonVisuals();
    }

    void AutoWireReferences()
    {
        if (logoObject == null)
            logoObject = transform.Find("Logo")?.gameObject;

        if (logoImage == null && logoObject != null)
            logoImage = logoObject.GetComponent<Image>();

        if (mainButtonsPanel == null)
            mainButtonsPanel = transform.Find("MainButtons")?.gameObject;

        if (buttonSet == null)
            buttonSet = Resources.Load<MainMenuButtonSet>("MainMenuButtonSet");

        if (menuButtons != null && menuButtons.Length > 0 && menuButtons[0] != null)
            return;

        if (mainButtonsPanel == null)
            return;

        var names = new[] { "Btn_JOGAR", "Btn_OPÇÕES", "Btn_CRÉDITOS", "Btn_SAIR" };
        menuButtons = new Button[names.Length];
        for (var i = 0; i < names.Length; i++)
            menuButtons[i] = mainButtonsPanel.transform.Find(names[i])?.GetComponent<Button>();
    }

    void RefreshButtonVisuals()
    {
        if (!useSpriteButtons || buttonSet == null || menuButtons == null)
            return;

        for (var i = 0; i < menuButtons.Length; i++)
        {
            var button = menuButtons[i];
            if (button == null)
                continue;

            var image = button.GetComponent<Image>();
            if (image == null)
                continue;

            var entry = buttonSet.Get(i);
            image.sprite = ResolveSprite(entry, i);
            image.color = Color.white;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                var slotHeight = ButtonSlotHeight(entry, buttonWidth);
                rect.sizeDelta = new Vector2(buttonWidth, slotHeight);
            }
        }
    }

    void BringButtonToFront(int index)
    {
        if (mainButtonsPanel == null || menuButtons == null)
            return;
        if (index < 0 || index >= menuButtons.Length || menuButtons[index] == null)
            return;

        menuButtons[index].transform.SetAsLastSibling();
    }

    float SpriteHeight(Sprite sprite, float width)
    {
        if (sprite == null || sprite.rect.width <= 0f)
            return width * 0.14f;

        return width * (sprite.rect.height / sprite.rect.width);
    }

    float ButtonSlotHeight(MainMenuButtonSet.Entry entry, float width)
    {
        var height = 0f;
        if (entry.normal != null)
            height = Mathf.Max(height, SpriteHeight(entry.normal, width));
        if (entry.hover != null)
            height = Mathf.Max(height, SpriteHeight(entry.hover, width));
        if (entry.selected != null)
            height = Mathf.Max(height, SpriteHeight(entry.selected, width));

        return (height > 0f ? height : width * 0.14f) + buttonSlotPadding;
    }

    float GetLogoBottomY()
    {
        const float refHeight = 1080f;
        if (logoObject == null || !logoObject.activeSelf)
            return refHeight * 0.68f;

        var sprite = logoSprite != null ? logoSprite : logoImage != null ? logoImage.sprite : null;
        var logoHeight = sprite != null
            ? logoWidth * (sprite.rect.height / sprite.rect.width)
            : logoWidth * 0.35f;

        return logoAnchor.y * refHeight - logoHeight * 0.5f;
    }

    void AutoFitButtonStack()
    {
        if (menuButtons == null || buttonSet == null)
            return;

        const float refHeight = 1080f;
        var width = buttonWidth;

        while (width >= minButtonWidth)
        {
            if (TryGetFirstButtonCenterY(width, out var firstCenterY))
            {
                buttonWidth = width;
                topButtonAnchorY = firstCenterY / refHeight;
                return;
            }

            width -= 10f;
        }

        if (TryGetFirstButtonCenterY(minButtonWidth, out var fallbackCenterY))
        {
            buttonWidth = minButtonWidth;
            topButtonAnchorY = fallbackCenterY / refHeight;
        }
    }

    bool TryGetFirstButtonCenterY(float width, out float firstCenterY)
    {
        firstCenterY = 0f;
        if (menuButtons == null || buttonSet == null)
            return false;

        var heights = new float[menuButtons.Length];
        for (var i = 0; i < menuButtons.Length; i++)
            heights[i] = ButtonSlotHeight(buttonSet.Get(i), width);

        var span = 0f;
        for (var i = 0; i < heights.Length - 1; i++)
            span += heights[i] + buttonVerticalGap;

        var logoBottomY = GetLogoBottomY();
        var maxFirstCenter = logoBottomY - gapBelowLogo - heights[0] * 0.5f;
        var minFirstCenter = bottomMargin + heights[^1] * 0.5f + span;

        if (minFirstCenter > maxFirstCenter)
            return false;

        firstCenterY = maxFirstCenter;
        return true;
    }

    Sprite ResolveSprite(MainMenuButtonSet.Entry entry, int index)
    {
        if (_hoveredIndex >= 0)
        {
            if (_hoveredIndex == index && entry.hover != null)
                return entry.hover;

            return entry.normal != null ? entry.normal : entry.hover;
        }

        if (_selectedIndex == index && entry.selected != null)
            return entry.selected;

        if (entry.normal != null)
            return entry.normal;

        return entry.hover != null ? entry.hover : entry.selected;
    }

    void ConfigureLogo()
    {
        if (logoObject == null)
            logoObject = transform.Find("Logo")?.gameObject;

        if (logoObject == null)
            return;

        if (logoImage == null)
            logoImage = logoObject.GetComponent<Image>();

        foreach (var label in logoObject.GetComponentsInChildren<TextMeshProUGUI>(true))
            label.gameObject.SetActive(false);

        if (logoImage != null)
        {
            var rect = logoImage.rectTransform;
            rect.anchorMin = logoAnchor;
            rect.anchorMax = logoAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            if (logoSprite != null)
            {
                var aspect = logoSprite.rect.height / logoSprite.rect.width;
                rect.sizeDelta = new Vector2(logoWidth, logoWidth * aspect);
            }
            else
                rect.sizeDelta = new Vector2(logoWidth, logoWidth * 0.35f);
        }

        if (logoSprite != null && logoImage != null)
        {
            logoImage.sprite = logoSprite;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            logoObject.SetActive(true);
            return;
        }

        if (logoImage != null)
            logoImage.raycastTarget = false;

        logoObject.SetActive(false);
    }

    void ConfigureButtonsPanel()
    {
        if (mainButtonsPanel == null)
            return;

        var panelRect = mainButtonsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
        }

        var layout = mainButtonsPanel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            layout.enabled = false;
    }

    void ConfigureButtons()
    {
        if (menuButtons == null || menuButtons.Length == 0)
            return;

        if (autoFitButtonStack && useSpriteButtons && buttonSet != null)
            AutoFitButtonStack();

        var anchorY = topButtonAnchorY;
        const float refHeight = 1080f;

        for (var i = 0; i < menuButtons.Length; i++)
        {
            var button = menuButtons[i];
            if (button == null)
                continue;

            CleanupButtonChildren(button.transform);

            var image = button.GetComponent<Image>();
            var height = buttonWidth * 0.14f;
            if (useSpriteButtons && buttonSet != null)
                height = ButtonSlotHeight(buttonSet.Get(i), buttonWidth);

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, anchorY);
                rect.anchorMax = new Vector2(0.5f, anchorY);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(buttonWidth, height);
                rect.anchoredPosition = Vector2.zero;
            }

            if (image != null)
            {
                image.raycastTarget = true;
                if (!useSpriteButtons)
                    image.color = new Color(1f, 1f, 1f, 0f);
            }

            button.transition = Selectable.Transition.None;
            anchorY -= (height + buttonVerticalGap) / refHeight;
        }
    }

    static void CleanupButtonChildren(Transform button)
    {
        foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            Destroy(label.gameObject);

        var highlight = button.Find("Highlight");
        if (highlight != null)
            Destroy(highlight.gameObject);

        foreach (Transform child in button)
        {
            if (child.name == "Text")
                Destroy(child.gameObject);
        }
    }

    void SortCanvasLayers()
    {
        var bg = transform.Find("Background");
        if (bg != null)
            bg.SetAsFirstSibling();

        if (mainButtonsPanel != null)
            mainButtonsPanel.transform.SetSiblingIndex(1);

        if (logoObject != null)
            logoObject.transform.SetSiblingIndex(2);

        if (optionsPanel == null)
            optionsPanel = transform.Find("Panel_Opcoes")?.gameObject;
        if (creditsPanel == null)
            creditsPanel = transform.Find("Panel_Creditos")?.gameObject;

        if (optionsPanel != null)
            optionsPanel.transform.SetAsLastSibling();
        if (creditsPanel != null)
            creditsPanel.transform.SetAsLastSibling();
    }
}
