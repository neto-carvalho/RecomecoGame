using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sensibilidade do mouse — menu Opções (sub-overlay) e pausa (inline).
/// </summary>
public static class LookSensitivityOptionsUI
{
    const string RowName = "LookSensitivityRow";
    const string LayoutRootName = "OptionsMenuLayout";
    const string SensitivityOverlayName = "SensitivityOptionsOverlay";

    public static void EnsureOnMainMenuOptions(GameObject optionsPanel)
    {
        if (optionsPanel == null)
            return;

        var box = optionsPanel.transform.Find("Box") as RectTransform;
        if (box == null)
            return;

        var heading = box.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (heading != null)
        {
            heading.text = "OPÇÕES";
            heading.fontSize = 30;
            heading.alignment = TextAlignmentOptions.Top;
            heading.margin = new Vector4(24, 20, 24, 8);
        }

        if (box.Find(LayoutRootName) != null)
            return;

        RemoveLegacyMainMenuOptionsWidgets(box);

        var layoutGo = new GameObject(LayoutRootName, typeof(RectTransform));
        layoutGo.transform.SetParent(box, false);
        var layoutRect = layoutGo.GetComponent<RectTransform>();
        layoutRect.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRect.pivot = new Vector2(0.5f, 0.5f);
        layoutRect.anchoredPosition = new Vector2(0f, -12f);
        layoutRect.sizeDelta = new Vector2(360f, 132f);

        var layout = layoutGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        CreateMainMenuOptionButton(
            layoutGo.transform,
            "CONTROLES",
            new Color(0.35f, 0.28f, 0.08f, 1f),
            new Color(0.98f, 0.86f, 0.22f, 1f),
            () => ControlsGuideOverlayUI.Open(optionsPanel.transform));

        CreateMainMenuOptionButton(
            layoutGo.transform,
            "SENSIBILIDADE",
            new Color(0.16f, 0.32f, 0.22f, 1f),
            Color.white,
            () => OpenMainMenuSensitivityOverlay(optionsPanel.transform));
    }

    static void RemoveLegacyMainMenuOptionsWidgets(Transform box)
    {
        var legacyRow = box.Find(RowName);
        if (legacyRow != null)
            UnityEngine.Object.Destroy(legacyRow.gameObject);

        var legacyBtn = box.Find("Btn_Controles");
        if (legacyBtn != null)
            UnityEngine.Object.Destroy(legacyBtn.gameObject);
    }

    static void CreateMainMenuOptionButton(
        Transform parent,
        string label,
        Color bg,
        Color textColor,
        Action onClick)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 52f;
        le.preferredHeight = 52f;

        var img = go.AddComponent<Image>();
        img.color = bg;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => onClick?.Invoke());

        var tmp = CreateTmp(go.transform, label, 22, TextAlignmentOptions.Center);
        StretchFull(tmp.rectTransform);
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
    }

    static void OpenMainMenuSensitivityOverlay(Transform optionsPanel)
    {
        if (optionsPanel == null)
            return;

        if (optionsPanel.Find(SensitivityOverlayName) != null)
            return;

        var root = new GameObject(SensitivityOverlayName);
        root.transform.SetParent(optionsPanel, false);
        var rootRect = root.AddComponent<RectTransform>();
        StretchFull(rootRect);

        var dim = new GameObject("Dim");
        dim.transform.SetParent(root.transform, false);
        var dimRect = dim.AddComponent<RectTransform>();
        StretchFull(dimRect);
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.72f);
        dimImg.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 220f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.09f, 0.09f, 0.12f, 0.97f);

        var title = CreateTmp(panel.transform, "Sensibilidade do mouse", 24, TextAlignmentOptions.Center);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(-32f, 36f);
        title.fontStyle = FontStyles.Bold;

        var valueTmp = CreateTmp(panel.transform, RecomecoLookSensitivitySettings.FormatPercent(), 22,
            TextAlignmentOptions.Center);
        var valueRect = valueTmp.rectTransform;
        valueRect.anchorMin = new Vector2(0.5f, 1f);
        valueRect.anchorMax = new Vector2(0.5f, 1f);
        valueRect.pivot = new Vector2(0.5f, 1f);
        valueRect.anchoredPosition = new Vector2(0f, -58f);
        valueRect.sizeDelta = new Vector2(120f, 28f);
        valueTmp.color = new Color(0.95f, 0.78f, 0.15f, 1f);

        var sliderRoot = new GameObject("SliderRoot", typeof(RectTransform));
        sliderRoot.transform.SetParent(panel.transform, false);
        var sliderRootRect = sliderRoot.GetComponent<RectTransform>();
        sliderRootRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRootRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRootRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRootRect.anchoredPosition = new Vector2(0f, -8f);
        sliderRootRect.sizeDelta = new Vector2(-48f, 36f);

        var slider = BuildSlider(sliderRoot.transform);
        slider.SetValueWithoutNotify(RecomecoLookSensitivitySettings.Slider01);

        void RefreshValue() => valueTmp.text = RecomecoLookSensitivitySettings.FormatPercent();
        RefreshValue();
        slider.onValueChanged.AddListener(v =>
        {
            RecomecoLookSensitivitySettings.Slider01 = v;
            RefreshValue();
        });

        var closeBtnGo = new GameObject("Btn_Fechar", typeof(RectTransform));
        closeBtnGo.transform.SetParent(panel.transform, false);
        var closeRect = closeBtnGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 18f);
        closeRect.sizeDelta = new Vector2(200f, 46f);

        var closeImg = closeBtnGo.AddComponent<Image>();
        closeImg.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;

        var closeLabel = CreateTmp(closeBtnGo.transform, "FECHAR", 20, TextAlignmentOptions.Center);
        StretchFull(closeLabel.rectTransform);
        closeLabel.fontStyle = FontStyles.Bold;

        void Close()
        {
            if (root != null)
                UnityEngine.Object.Destroy(root);
        }

        closeBtn.onClick.AddListener(Close);
        var closer = root.AddComponent<ControlsGuideOverlayCloser>();
        closer.Initialize(Close);

        root.transform.SetAsLastSibling();
    }

    /// <summary>Filho de um VerticalLayoutGroup (menu de pausa).</summary>
    public static void AddPauseSensitivityRow(Transform parent)
    {
        if (parent == null)
            return;

        var row = new GameObject(RowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 88f;
        rowLe.preferredHeight = 88f;

        var rowLayout = row.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        var header = new GameObject("Header", typeof(RectTransform));
        header.transform.SetParent(row.transform, false);
        var headerLe = header.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 24f;
        headerLe.minHeight = 24f;

        var hlg = header.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 8f;

        var label = CreateTmp(header.transform, "Sensibilidade do mouse", 18, TextAlignmentOptions.MidlineLeft);
        var labelLe = label.gameObject.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        labelLe.minWidth = 120f;

        var valueTmp = CreateTmp(header.transform, RecomecoLookSensitivitySettings.FormatPercent(), 18,
            TextAlignmentOptions.MidlineRight);
        var valueLe = valueTmp.gameObject.AddComponent<LayoutElement>();
        valueLe.minWidth = 48f;
        valueLe.preferredWidth = 56f;

        var sliderRoot = new GameObject("SliderRoot", typeof(RectTransform));
        sliderRoot.transform.SetParent(row.transform, false);
        var sliderLe = sliderRoot.AddComponent<LayoutElement>();
        sliderLe.preferredHeight = 32f;
        sliderLe.minHeight = 32f;

        var slider = BuildSlider(sliderRoot.transform);
        slider.SetValueWithoutNotify(RecomecoLookSensitivitySettings.Slider01);

        void RefreshValue() => valueTmp.text = RecomecoLookSensitivitySettings.FormatPercent();

        RefreshValue();
        slider.onValueChanged.AddListener(v =>
        {
            RecomecoLookSensitivitySettings.Slider01 = v;
            RefreshValue();
        });
    }

    static TextMeshProUGUI CreateTmp(Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = new Color(0.9f, 0.9f, 0.93f);
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Slider BuildSlider(Transform parent)
    {
        var sliderGo = new GameObject("Slider", typeof(RectTransform));
        sliderGo.transform.SetParent(parent, false);
        var sliderRect = sliderGo.GetComponent<RectTransform>();
        StretchFull(sliderRect);

        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.transition = Selectable.Transition.ColorTint;

        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(sliderRect, false);
        StretchFull(bgGo.GetComponent<RectTransform>());
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderRect, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        StretchFull(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(10f, 8f);
        fillAreaRect.offsetMax = new Vector2(-10f, -8f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.35f, 1f);
        slider.fillRect = fillRect;

        var handleSlide = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlide.transform.SetParent(sliderRect, false);
        var handleSlideRect = handleSlide.GetComponent<RectTransform>();
        StretchFull(handleSlideRect);
        handleSlideRect.offsetMin = new Vector2(10f, 0f);
        handleSlideRect.offsetMax = new Vector2(-10f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleSlide.transform, false);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 18f);
        handle.GetComponent<Image>().color = Color.white;

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.handleRect = handleRect;

        return slider;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}