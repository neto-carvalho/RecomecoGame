using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    public static InteractionUI instance;

    public GameObject interactionTextObject;
    public TextMeshProUGUI interactionText;

    void Awake()
    {
        instance = this;
    }

    public void ShowText(string message)
    {
        interactionTextObject.SetActive(true);
        interactionText.text = message;
    }

    public void HideText()
    {
        interactionTextObject.SetActive(false);
    }
}