using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public const int PriorityNavigation = 0;
    public const int PriorityGameplay = 10;

    public static InteractionUI instance;

    static readonly Dictionary<object, MessageRequest> s_ActiveMessages = new();
    static object _messageOwner;
    static bool _sceneHookRegistered;

    public GameObject interactionTextObject;
    public TextMeshProUGUI interactionText;

    struct MessageRequest
    {
        public string Message;
        public int Priority;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneHook()
    {
        if (_sceneHookRegistered)
            return;
        _sceneHookRegistered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAllMessages();
    }

    void Awake()
    {
        TryAutoWire();
        RegisterIfValid();
    }

    void Start()
    {
        TryAutoWire();
        RegisterIfValid();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void LateUpdate()
    {
        PurgeInactivePromptOwners();
        if (s_ActiveMessages.Count == 0)
        {
            if (interactionText != null && interactionText.gameObject.activeSelf)
                HideText();
            return;
        }

        ApplyBestMessage();
    }

    void RegisterIfValid()
    {
        if (interactionText != null)
            Register(this);
        else if (instance == null)
            instance = this;
    }

    public static void Register(InteractionUI ui)
    {
        if (ui == null)
            return;
        ui.TryAutoWire();
        if (ui.interactionText != null)
            instance = ui;
    }

    public static void BindForActiveScene()
    {
        if (instance != null && instance.interactionText != null)
            return;

        var hudRoot = GameplayHudBootstrap.GetHudRoot();
        if (hudRoot != null)
        {
            var hudUi = hudRoot.GetComponentInChildren<InteractionUI>(true);
            if (hudUi != null && hudUi.interactionText != null)
            {
                Register(hudUi);
                return;
            }
        }

        foreach (var ui in FindObjectsByType<InteractionUI>(FindObjectsSortMode.None))
        {
            if (ui == null)
                continue;
            ui.TryAutoWire();
            if (ui.interactionText != null)
            {
                Register(ui);
                return;
            }
        }
    }

    public static void ShowMessage(string message, object owner = null, int priority = PriorityGameplay)
    {
        BindForActiveScene();
        if (owner == null)
            owner = instance != null ? (object)instance : typeof(InteractionUI);

        s_ActiveMessages[owner] = new MessageRequest
        {
            Message = message,
            Priority = priority,
        };

        ApplyBestMessage();
    }

    public static void HideMessage(object owner = null)
    {
        if (owner == null)
        {
            ClearAllMessages();
            return;
        }

        s_ActiveMessages.Remove(owner);
        PurgeDeadOwners();
        ApplyBestMessage();
    }

    static void ClearAllMessages()
    {
        s_ActiveMessages.Clear();
        _messageOwner = null;
        if (instance != null)
            instance.HideText();
    }

    static void PurgeDeadOwners()
    {
        var dead = new List<object>();
        foreach (var pair in s_ActiveMessages)
        {
            if (pair.Key is Object unityObject && unityObject == null)
                dead.Add(pair.Key);
        }

        foreach (var key in dead)
            s_ActiveMessages.Remove(key);
    }

    static void PurgeInactivePromptOwners()
    {
        var inactive = new List<object>();
        foreach (var pair in s_ActiveMessages)
        {
            if (pair.Key is IInteractionPromptOwner prompt && !prompt.IsInteractionPromptActive())
                inactive.Add(pair.Key);
        }

        foreach (var key in inactive)
            s_ActiveMessages.Remove(key);
    }

    static void ApplyBestMessage()
    {
        PurgeDeadOwners();
        PurgeInactivePromptOwners();

        if (s_ActiveMessages.Count == 0)
        {
            _messageOwner = null;
            if (instance != null)
                instance.HideText();
            return;
        }

        MessageRequest best = default;
        object bestOwner = null;
        var found = false;

        foreach (var pair in s_ActiveMessages)
        {
            if (pair.Key is Object unityObject && unityObject == null)
                continue;

            if (pair.Key is IInteractionPromptOwner prompt && !prompt.IsInteractionPromptActive())
                continue;

            var request = pair.Value;
            if (!found ||
                request.Priority > best.Priority ||
                (request.Priority == best.Priority && pair.Key == _messageOwner))
            {
                best = request;
                bestOwner = pair.Key;
                found = true;
            }
        }

        if (!found)
        {
            ClearAllMessages();
            return;
        }

        _messageOwner = bestOwner;
        if (instance != null)
            instance.ShowText(best.Message);
    }

    void TryAutoWire()
    {
        if (interactionText == null && interactionTextObject != null)
        {
            interactionText = interactionTextObject.GetComponent<TextMeshProUGUI>()
                ?? interactionTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (interactionText != null)
            interactionTextObject = interactionText.gameObject;
        else if (interactionTextObject != null)
            interactionText = interactionTextObject.GetComponent<TextMeshProUGUI>()
                ?? interactionTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void ShowText(string message)
    {
        if (interactionText == null)
        {
            UnityEngine.Debug.LogWarning(
                "InteractionUI: preencha o campo 'Interaction Text' com o componente TextMeshProUGUI do texto de interação.");
            return;
        }

        SuppressStrayInteractionLabels();
        interactionText.gameObject.SetActive(true);
        interactionText.text = message;
    }

    public void HideText()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
        else if (interactionTextObject != null)
            interactionTextObject.SetActive(false);

        SuppressStrayInteractionLabels();
    }

    static void SuppressStrayInteractionLabels()
    {
        foreach (var ui in FindObjectsByType<InteractionUI>(FindObjectsSortMode.None))
        {
            if (ui == null || ui == instance)
                continue;

            if (ui.interactionText != null)
                ui.interactionText.gameObject.SetActive(false);
            else if (ui.interactionTextObject != null)
                ui.interactionTextObject.SetActive(false);
        }
    }
}
