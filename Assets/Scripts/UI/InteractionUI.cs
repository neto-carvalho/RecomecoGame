using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI instance;

    public GameObject interactionTextObject;
    public TextMeshProUGUI interactionText;

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

    /// <summary>Usa UI da cena ativa ou a que viajou com DontDestroyOnLoad.</summary>
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

    public static void ShowMessage(string message)
    {
        BindForActiveScene();
        if (instance != null)
            instance.ShowText(message);
    }

    public static void HideMessage()
    {
        if (instance != null)
            instance.HideText();
    }

    void TryAutoWire()
    {
        if (interactionText == null && interactionTextObject != null)
        {
            interactionText = interactionTextObject.GetComponent<TextMeshProUGUI>()
                ?? interactionTextObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Sempre alinhar: quem aparece/some é o GameObject do TMP (evita arrastar o objeto errado no campo "Interaction Text Object").
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