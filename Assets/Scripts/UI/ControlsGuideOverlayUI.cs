using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay com a arte completa de controles (menu, pausa e início de partida).
/// </summary>
public static class ControlsGuideOverlayUI
{
    public const string SpriteResourcePath = "UI/tutorial_controles_completo";

    const string OverlayName = "ControlsGuideOverlay";

    public static GameObject Open(Transform parent, Action onClosed = null)
    {
        if (parent == null)
            return null;

        var existing = parent.Find(OverlayName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.SetAsLastSibling();
            return existing.gameObject;
        }

        var sprite = LoadSprite();
        if (sprite == null)
        {
            Debug.LogWarning("ControlsGuideOverlayUI: sprite de controles não encontrado em Resources/" +
                             SpriteResourcePath);
            return null;
        }

        var root = new GameObject(OverlayName);
        root.transform.SetParent(parent, false);
        var rootRect = root.AddComponent<RectTransform>();
        StretchFull(rootRect);

        var dim = new GameObject("Dim");
        dim.transform.SetParent(root.transform, false);
        var dimRect = dim.AddComponent<RectTransform>();
        StretchFull(dimRect);
        var dimImg = dim.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.82f);
        dimImg.raycastTarget = true;

        var artGo = new GameObject("Art");
        artGo.transform.SetParent(root.transform, false);
        var artRect = artGo.AddComponent<RectTransform>();
        artRect.anchorMin = new Vector2(0.5f, 0.5f);
        artRect.anchorMax = new Vector2(0.5f, 0.5f);
        artRect.pivot = new Vector2(0.5f, 0.5f);
        artRect.anchoredPosition = Vector2.zero;

        var art = artGo.AddComponent<Image>();
        art.sprite = sprite;
        art.preserveAspect = true;
        art.color = Color.white;
        art.raycastTarget = false;
        FitImageToScreen(artRect, sprite, centerOnScreen: true);

        var hint = CreateTmp(root.transform, "ESC ou FECHAR para voltar", 17f, TextAlignmentOptions.Center);
        var hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 24f);
        hintRect.sizeDelta = new Vector2(520f, 28f);
        hint.color = new Color(0.9f, 0.88f, 0.82f, 0.95f);

        var closeBtn = CreateButton(root.transform, "FECHAR", new Color(0.35f, 0.28f, 0.08f, 1f));
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 52f);
        closeRect.sizeDelta = new Vector2(220f, 46f);

        void Close()
        {
            if (root != null)
                UnityEngine.Object.Destroy(root);
            onClosed?.Invoke();
        }

        closeBtn.onClick.AddListener(Close);

        var closer = root.AddComponent<ControlsGuideOverlayCloser>();
        closer.Initialize(Close);

        root.transform.SetAsLastSibling();
        return root;
    }

    public static void FitImageToScreen(RectTransform imageRect, Sprite sprite, bool centerOnScreen)
    {
        if (imageRect == null || sprite == null)
            return;

        var aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        var maxWidth = Screen.width * 0.94f;
        var maxHeight = Screen.height * (centerOnScreen ? 0.78f : 0.46f);

        var width = maxWidth;
        var height = width / aspect;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        imageRect.sizeDelta = new Vector2(width, height);
        if (!centerOnScreen)
            imageRect.anchoredPosition = new Vector2(0f, Screen.height * 0.04f);
    }

    public static Sprite LoadSprite()
    {
        var sprite = Resources.Load<Sprite>(SpriteResourcePath);
        if (sprite != null)
            return sprite;

        var tex = Resources.Load<Texture2D>(SpriteResourcePath);
        if (tex == null)
            return null;

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    static TextMeshProUGUI CreateTmp(
        Transform parent, string text, float size, TextAlignmentOptions align)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Button CreateButton(Transform parent, string label, Color color)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;

        var text = CreateTmp(go.transform, label, 20f, TextAlignmentOptions.Center);
        StretchFull(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

sealed class ControlsGuideOverlayCloser : MonoBehaviour
{
    Action _close;

    public void Initialize(Action close)
    {
        _close = close;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            _close?.Invoke();
    }
}