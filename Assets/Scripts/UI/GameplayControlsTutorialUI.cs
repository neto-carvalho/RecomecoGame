using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Exibe a arte de controles no início de uma partida nova (some sozinha após alguns segundos).
/// </summary>
public sealed class GameplayControlsTutorialUI : MonoBehaviour
{
    static bool s_pendingForNewGame;
    static GameplayControlsTutorialUI s_instance;

    Image _image;
    RectTransform _imageRect;
    CanvasGroup _group;
    Coroutine _routine;

    public static void RequestForNewGame()
    {
        s_pendingForNewGame = true;
    }

    public static void ClearPending()
    {
        s_pendingForNewGame = false;
    }

    public static void TryShowForNewGame()
    {
        if (SaveGameManager.HasPendingLoad)
            return;

        if (!s_pendingForNewGame)
            return;

        s_pendingForNewGame = false;

        if (s_instance == null)
            ResolveInstance();

        if (s_instance == null)
            return;

        s_instance.Play();
    }

    public void Wire(Image image, RectTransform imageRect, CanvasGroup group)
    {
        _image = image;
        _imageRect = imageRect;
        _group = group;

        var sprite = ControlsGuideOverlayUI.LoadSprite();
        if (_image != null && sprite != null)
        {
            _image.sprite = sprite;
            _image.preserveAspect = true;
        }

        gameObject.SetActive(false);
        s_instance = this;
    }

    void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    void Play()
    {
        if (_image == null || _image.sprite == null)
            return;

        ControlsGuideOverlayUI.FitImageToScreen(_imageRect, _image.sprite, centerOnScreen: true);
        _imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        _imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        _imageRect.pivot = new Vector2(0.5f, 0.5f);
        _imageRect.anchoredPosition = Vector2.zero;

        if (_routine != null)
            StopCoroutine(_routine);

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        _routine = StartCoroutine(ShowRoutine(ResolveDuration()));
    }

    float ResolveDuration()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.controlsTutorialSeconds > 0.5f)
            return settings.controlsTutorialSeconds;

        return 7f;
    }

    IEnumerator ShowRoutine(float visibleSeconds)
    {
        const float fadeIn = 0.35f;
        const float fadeOut = 0.55f;
        var hold = Mathf.Max(1.5f, visibleSeconds - fadeIn - fadeOut);

        if (_group != null)
            _group.alpha = 0f;

        var t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            if (_group != null)
                _group.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }

        if (_group != null)
            _group.alpha = 1f;

        yield return new WaitForSecondsRealtime(hold);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            if (_group != null)
                _group.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }

        gameObject.SetActive(false);
        _routine = null;
    }

    static void ResolveInstance()
    {
        if (s_instance != null)
            return;

        var all = Object.FindObjectsByType<GameplayControlsTutorialUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all != null && all.Length > 0)
            s_instance = all[0];
    }

    public static Sprite LoadTutorialSpriteForBootstrap()
    {
        return ControlsGuideOverlayUI.LoadSprite();
    }
}