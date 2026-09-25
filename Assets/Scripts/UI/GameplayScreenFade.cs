using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayScreenFade
{
    static CanvasGroup s_group;
    static Canvas s_canvas;

    public static IEnumerator FadeOut(float duration)
    {
        EnsureOverlay();
        if (s_group == null)
            yield break;

        s_group.blocksRaycasts = true;
        s_group.alpha = 0f;
        s_canvas.gameObject.SetActive(true);

        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            s_group.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            yield return null;
        }

        s_group.alpha = 1f;
    }

    public static IEnumerator FadeIn(float duration)
    {
        EnsureOverlay();
        if (s_group == null)
            yield break;

        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            s_group.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            yield return null;
        }

        s_group.alpha = 0f;
        s_group.blocksRaycasts = false;
        s_canvas.gameObject.SetActive(false);
    }

    static void EnsureOverlay()
    {
        if (s_group != null)
            return;

        var canvasGo = new GameObject("GameplayScreenFade");
        Object.DontDestroyOnLoad(canvasGo);

        s_canvas = canvasGo.AddComponent<Canvas>();
        s_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        s_canvas.sortingOrder = 480;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("Fade");
        imageGo.transform.SetParent(canvasGo.transform, false);
        var rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        s_group = canvasGo.AddComponent<CanvasGroup>();
        s_group.alpha = 0f;
        s_group.blocksRaycasts = false;
        canvasGo.SetActive(false);
    }
}
