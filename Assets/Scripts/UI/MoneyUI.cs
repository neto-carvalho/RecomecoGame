using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MoneyUI : MonoBehaviour
{
    [Tooltip("Prefixo exibido antes do valor formatado (ex: \"Dinheiro: \")")]
    public string prefix = "";

    private TextMeshProUGUI textField;

    void Start()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (textField == null) return;
        int cents = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0;
        textField.text = prefix + MoneyManager.FormatBRL(cents);
    }
}
