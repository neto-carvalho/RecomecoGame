using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dica temporária abaixo do dinheiro (ex.: após dormir na barraca).
/// </summary>
public class GameplayTipBannerUI : MonoBehaviour
{
    const float TitleBandHeight = 30f;
    const float VerticalPadding = 14f;
    const float MinPanelHeight = 72f;
    const float MaxPanelHeight = 172f;

    static GameplayTipBannerUI s_instance;

    TextMeshProUGUI _body;
    CanvasGroup _group;
    LayoutElement _layout;
    Coroutine _hideRoutine;

    public static void Show(string message, float visibleSeconds = 7f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (s_instance == null)
        {
            var all = Object.FindObjectsByType<GameplayTipBannerUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all != null && all.Length > 0)
                s_instance = all[0];
        }

        if (s_instance == null)
        {
            Debug.Log("[Dica] " + message);
            return;
        }

        s_instance.InternalShow(message, visibleSeconds);
    }

    public void Wire(TextMeshProUGUI body, CanvasGroup group)
    {
        _body = body;
        _group = group;
        _layout = GetComponent<LayoutElement>();
        if (_layout == null)
            _layout = gameObject.AddComponent<LayoutElement>();
        ApplyBodyRectLayout();
        gameObject.SetActive(false);
    }

    void InternalShow(string message, float visibleSeconds)
    {
        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        if (_body != null)
            _body.text = message;

        ApplyBodyRectLayout();
        FitPanelHeightToText();

        gameObject.SetActive(true);
        if (_group != null)
            _group.alpha = 1f;

        _hideRoutine = StartCoroutine(HideAfter(visibleSeconds));
    }

    void ApplyBodyRectLayout()
    {
        if (_body == null)
            return;

        var bodyRect = _body.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(12f, VerticalPadding * 0.5f);
        bodyRect.offsetMax = new Vector2(-12f, -TitleBandHeight);

        _body.alignment = TextAlignmentOptions.TopLeft;
        _body.textWrappingMode = TextWrappingModes.Normal;
        _body.overflowMode = TextOverflowModes.Overflow;
        _body.fontSize = 17f;
        _body.lineSpacing = -2f;
    }

    void FitPanelHeightToText()
    {
        if (_layout == null || _body == null)
            return;

        _body.ForceMeshUpdate();
        var textHeight = _body.preferredHeight;
        var total = TitleBandHeight + textHeight + VerticalPadding;
        _layout.minHeight = MinPanelHeight;
        _layout.preferredHeight = Mathf.Clamp(total, MinPanelHeight, MaxPanelHeight);

        var parent = transform.parent as RectTransform;
        if (parent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }

    IEnumerator HideAfter(float seconds)
    {
        var fade = 0.45f;
        var hold = Mathf.Max(0.5f, seconds - fade);
        yield return new WaitForSecondsRealtime(hold);

        if (_group != null)
        {
            var t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / fade);
                yield return null;
            }
        }

        gameObject.SetActive(false);
        _hideRoutine = null;
    }

    public static void ApplyLayoutToExisting(Transform bannerRoot)
    {
        if (bannerRoot == null)
            return;

        var ui = bannerRoot.GetComponent<GameplayTipBannerUI>();
        var body = bannerRoot.Find("Body")?.GetComponent<TextMeshProUGUI>();
        if (ui != null && body != null)
        {
            var group = bannerRoot.GetComponent<CanvasGroup>();
            ui.Wire(body, group);
            return;
        }

        var layout = bannerRoot.GetComponent<LayoutElement>() ?? bannerRoot.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;

        var title = bannerRoot.Find("Title") as RectTransform;
        if (title != null)
        {
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.sizeDelta = new Vector2(-24f, TitleBandHeight);
            title.anchoredPosition = new Vector2(0f, -4f);
        }

        if (body != null)
        {
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(12f, VerticalPadding * 0.5f);
            bodyRect.offsetMax = new Vector2(-12f, -TitleBandHeight);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.fontSize = 17f;
            body.lineSpacing = -2f;
        }
    }
}
