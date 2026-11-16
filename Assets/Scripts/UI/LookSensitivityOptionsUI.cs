using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slider de sensibilidade do mouse — menu Opções e pausa.
/// </summary>
public static class LookSensitivityOptionsUI
{
    const string RowName = "LookSensitivityRow";

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

        if (box.Find(RowName) != null)
            return;

        CreateAnchoredRow(box, new Vector2(0f, -24f), new Vector2(620f, 120f));
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

    static void CreateAnchoredRow(RectTransform parent, Vector2 pos, Vector2 size)
    {
        var row = new GameObject(RowName, typeof(RectTransform));
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.SetParent(parent, false);
        rowRect.anchoredPosition = pos;
        rowRect.sizeDelta = size;

        var label = CreateTmp(rowRect, "Sensibilidade do mouse", 20, TextAlignmentOptions.MidlineLeft);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(-80f, 32f);

        var valueTmp = CreateTmp(rowRect, RecomecoLookSensitivitySettings.FormatPercent(), 20,
            TextAlignmentOptions.MidlineRight);
        var valueRect = valueTmp.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 1f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.pivot = new Vector2(1f, 1f);
        valueRect.anchoredPosition = new Vector2(-4f, 0f);
        valueRect.sizeDelta = new Vector2(72f, 32f);

        var sliderRoot = new GameObject("SliderRoot", typeof(RectTransform));
        var sliderRootRect = sliderRoot.GetComponent<RectTransform>();
        sliderRootRect.SetParent(rowRect, false);
        sliderRootRect.anchorMin = new Vector2(0f, 0f);
        sliderRootRect.anchorMax = new Vector2(1f, 0f);
        sliderRootRect.pivot = new Vector2(0.5f, 0f);
        sliderRootRect.anchoredPosition = new Vector2(0f, 0f);
        sliderRootRect.sizeDelta = new Vector2(0f, 32f);

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
