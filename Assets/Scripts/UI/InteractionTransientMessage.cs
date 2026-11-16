using System.Collections;
using UnityEngine;

/// <summary>Legendas de gameplay que somem sozinhas (não bloqueiam a tela como prompts de interação).</summary>
public sealed class InteractionTransientMessage : MonoBehaviour
{
    static InteractionTransientMessage s_instance;

    public static InteractionTransientMessage Instance
    {
        get
        {
            if (s_instance != null)
                return s_instance;

            s_instance = FindFirstObjectByType<InteractionTransientMessage>();
            if (s_instance != null)
                return s_instance;

            var go = new GameObject(nameof(InteractionTransientMessage));
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<InteractionTransientMessage>();
            return s_instance;
        }
    }

    Coroutine _hideRoutine;

    public static void Show(string message, float visibleSeconds = 5f)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var seconds = visibleSeconds;
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.interactionTransientMessageSeconds > 0f)
            seconds = Mathf.Min(visibleSeconds, settings.interactionTransientMessageSeconds);

        seconds = Mathf.Clamp(seconds, 2f, 12f);

        InteractionUI.BindForActiveScene();
        Instance.InternalShow(message, seconds);
    }

    void InternalShow(string message, float visibleSeconds)
    {
        if (_hideRoutine != null)
            StopCoroutine(_hideRoutine);

        InteractionUI.ShowMessage(message, this, InteractionUI.PriorityGameplay + 2);
        _hideRoutine = StartCoroutine(HideAfter(visibleSeconds));
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        InteractionUI.HideMessage(this);
        _hideRoutine = null;
    }

    void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }
}