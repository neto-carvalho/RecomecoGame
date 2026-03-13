using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    private TextMeshProUGUI hudText;

    void Start()
    {
        hudText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (GameManager.instance == null) return;

        hudText.text =
            "Latinhas: " + GameManager.instance.latinhas +
            "\nDinheiro: $" + GameManager.instance.dinheiro;
    }
}