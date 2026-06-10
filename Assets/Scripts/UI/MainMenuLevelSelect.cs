using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Painel de escolha entre Ferro Velho e Cidade ao clicar em Jogar.
/// Constrói a UI em runtime se ainda não existir na cena.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuLevelSelect : MonoBehaviour
{
    const string FerroVelhoPreviewPath = "UI/Menu/level_ferro_velho";
    const string CidadePreviewPath = "UI/Menu/level_cidade";

    static readonly Color Gold = new(0.95f, 0.78f, 0.15f, 1f);
    static readonly Color PanelBg = new(0.09f, 0.08f, 0.07f, 0.97f);
    static readonly Color DimColor = new(0f, 0f, 0f, 0.74f);
    static readonly Color CardBg = new(0.14f, 0.12f, 0.1f, 1f);
    static readonly Color CardHoverBg = new(0.2f, 0.17f, 0.12f, 1f);

    [SerializeField] Sprite ferroVelhoPreview;
    [SerializeField] Sprite cidadePreview;

    bool _built;
    MainMenuController _menu;

    public void BuildIfNeeded()
    {
        if (_built)
            return;

        _built = true;
        ferroVelhoPreview ??= LoadPreviewSprite(FerroVelhoPreviewPath);
        cidadePreview ??= LoadPreviewSprite(CidadePreviewPath);

        _menu = GetComponentInParent<MainMenuController>();
        StretchFull(GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>());
        BuildUi();
        gameObject.SetActive(false);
    }

    void BuildUi()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        CreateStretchImage(transform, "Dim", DimColor);

        var box = new GameObject("Box");
        box.transform.SetParent(transform, false);
        StretchCenter(box.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(1580f, 780f));
        var boxImg = box.AddComponent<Image>();
        boxImg.color = PanelBg;

        var title = AddTmp(box.transform, "Title", "ONDE COMEÇAR?", 46, FontStyles.Bold);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -36f);
        titleRect.sizeDelta = new Vector2(1200f, 64f);
        title.color = Gold;

        var subtitle = AddTmp(box.transform, "Subtitle", "Escolha por onde sua jornada começa", 24, FontStyles.Normal);
        var subtitleRect = subtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 1f);
        subtitleRect.anchorMax = new Vector2(0.5f, 1f);
        subtitleRect.pivot = new Vector2(0.5f, 1f);
        subtitleRect.anchoredPosition = new Vector2(0f, -98f);
        subtitleRect.sizeDelta = new Vector2(1200f, 40f);
        subtitle.color = new Color(0.88f, 0.86f, 0.82f, 1f);

        var cards = new GameObject("Cards");
        cards.transform.SetParent(box.transform, false);
        var cardsRect = cards.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRect.pivot = new Vector2(0.5f, 0.5f);
        cardsRect.anchoredPosition = new Vector2(0f, -10f);
        cardsRect.sizeDelta = new Vector2(1420f, 520f);

        var layout = cards.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 48f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateLevelCard(
            cards.transform,
            ferroVelhoPreview,
            "FERRO VELHO",
            "Comece no ferro-velho do deserto",
            RecomecoSceneNames.FerroVelho);

        CreateLevelCard(
            cards.transform,
            cidadePreview,
            "CIDADE",
            "Comece explorando a cidade costeira",
            RecomecoSceneNames.Cidade);

        CreateBackButton(box.transform);
    }

    void CreateLevelCard(
        Transform parent,
        Sprite preview,
        string titleText,
        string description,
        string sceneName)
    {
        var cardGo = new GameObject("Card_" + sceneName);
        cardGo.transform.SetParent(parent, false);

        var cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(640f, 500f);

        var layoutElement = cardGo.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 640f;
        layoutElement.preferredHeight = 500f;

        var cardBg = cardGo.AddComponent<Image>();
        cardBg.color = CardBg;

        var outline = cardGo.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.enabled = false;

        var button = cardGo.AddComponent<Button>();
        button.targetGraphic = cardBg;
        button.onClick.AddListener(() =>
        {
            if (_menu != null)
                _menu.LoadGameplayScene(sceneName);
        });

        var hover = cardGo.AddComponent<LevelSelectCardHover>();
        hover.Configure(cardRect, cardBg, outline, CardBg, CardHoverBg);

        var previewGo = new GameObject("Preview");
        previewGo.transform.SetParent(cardGo.transform, false);
        var previewRect = previewGo.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0f, 0.22f);
        previewRect.anchorMax = new Vector2(1f, 1f);
        previewRect.offsetMin = new Vector2(12f, 0f);
        previewRect.offsetMax = new Vector2(-12f, -12f);
        var previewImg = previewGo.AddComponent<Image>();
        previewImg.sprite = preview;
        previewImg.preserveAspect = true;
        previewImg.color = preview != null ? Color.white : new Color(0.35f, 0.32f, 0.28f, 1f);
        previewImg.raycastTarget = false;

        var shade = CreateStretchImage(cardGo.transform, "Shade", new Color(0f, 0f, 0f, 0.45f));
        var shadeRect = shade.rectTransform;
        shadeRect.anchorMin = new Vector2(0f, 0f);
        shadeRect.anchorMax = new Vector2(1f, 0.42f);
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;
        shade.raycastTarget = false;

        var title = AddTmp(cardGo.transform, "Label", titleText, 34, FontStyles.Bold);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 0f);
        titleRect.pivot = new Vector2(0.5f, 0f);
        titleRect.anchoredPosition = new Vector2(0f, 72f);
        titleRect.sizeDelta = new Vector2(-48f, 48f);
        title.color = Gold;
        title.alignment = TextAlignmentOptions.Center;

        var desc = AddTmp(cardGo.transform, "Description", description, 20, FontStyles.Normal);
        var descRect = desc.rectTransform;
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0f);
        descRect.pivot = new Vector2(0.5f, 0f);
        descRect.anchoredPosition = new Vector2(0f, 28f);
        descRect.sizeDelta = new Vector2(-56f, 40f);
        desc.color = new Color(0.92f, 0.9f, 0.86f, 1f);
        desc.alignment = TextAlignmentOptions.Center;
    }

    static Button CreateBackButton(Transform parent)
    {
        var go = new GameObject("Btn_Voltar");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 28f);
        rect.sizeDelta = new Vector2(240f, 52f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.16f, 0.14f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var label = AddTmp(go.transform, "Text", "VOLTAR", 24, FontStyles.Bold);
        label.color = Gold;
        return btn;
    }

    static Sprite LoadPreviewSprite(string resourcesPath)
    {
        var sprite = Resources.Load<Sprite>(resourcesPath);
        if (sprite != null)
            return sprite;

        var texture = Resources.Load<Texture2D>(resourcesPath);
        if (texture == null)
            return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    static Image CreateStretchImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        StretchFull(go.AddComponent<RectTransform>());
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI AddTmp(Transform parent, string name, string text, float size, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        StretchFull(rect);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
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

    sealed class LevelSelectCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform _rect;
        Image _bg;
        Outline _outline;
        Color _normalColor;
        Color _hoverColor;
        float _targetScale = 1f;

        public void Configure(
            RectTransform rect,
            Image bg,
            Outline outline,
            Color normalColor,
            Color hoverColor)
        {
            _rect = rect;
            _bg = bg;
            _outline = outline;
            _normalColor = normalColor;
            _hoverColor = hoverColor;
        }

        void Update()
        {
            if (_rect == null)
                return;

            var scale = _rect.localScale.x;
            var next = Mathf.Lerp(scale, _targetScale, Time.unscaledDeltaTime * 12f);
            _rect.localScale = Vector3.one * next;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _targetScale = 1.03f;
            if (_bg != null)
                _bg.color = _hoverColor;
            if (_outline != null)
                _outline.enabled = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScale = 1f;
            if (_bg != null)
                _bg.color = _normalColor;
            if (_outline != null)
                _outline.enabled = false;
        }
    }
}
