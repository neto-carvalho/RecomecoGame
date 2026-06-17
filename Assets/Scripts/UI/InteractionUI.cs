using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public const int PriorityNavigation = 0;
    public const int PriorityGameplay = 10;

    public static InteractionUI instance;

    static readonly Dictionary<object, MessageRequest> s_ActiveMessages = new();
    static object _messageOwner;

    public GameObject interactionTextObject;
    public TextMeshProUGUI interactionText;

    struct MessageRequest
    {
        public string Message;
        public int Priority;
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
            s_ActiveMessages.Clear();
            _messageOwner = null;
            if (instance != null)
                instance.HideText();
            return;
        }

        s_ActiveMessages.Remove(owner);
        ApplyBestMessage();
    }

    static void ApplyBestMessage()
    {
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

        interactionText.gameObject.SetActive(true);
        interactionText.text = message;
    }

    public void HideText()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
        else if (interactionTextObject != null)
            interactionTextObject.SetActive(false);
    }
}
