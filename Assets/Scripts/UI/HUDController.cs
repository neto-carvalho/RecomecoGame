using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    private TextMeshProUGUI hudText;
    private Inventory inventory;

    void Start()
    {
        hudText = GetComponent<TextMeshProUGUI>();
        inventory = FindObjectOfType<Inventory>();
    }

    void Update()
    {
        int latinhas = (inventory != null) ? inventory.GetItemCount("Latinha") : 0;
        int dinheiro = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0;

        hudText.text =
            "Latinhas: " + latinhas +
            "\nDinheiro: R$ " + dinheiro;
    }
}