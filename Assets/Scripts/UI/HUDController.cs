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
        int dinheiro = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0;
        hudText.text = "Dinheiro: R$ " + dinheiro;
    }
}