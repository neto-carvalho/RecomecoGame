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
    }

    void Start()
    {
        // Várias instâncias na cena: o singleton deve ser sempre uma com TMP válido (evita Canvas com campos vazios).
        TryAutoWire();
        if (interactionText != null)
            instance = this;
        else if (instance == null)
            instance = this;
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