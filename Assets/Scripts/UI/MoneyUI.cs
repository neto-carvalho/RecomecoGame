using UnityEngine;
using TMPro;

/// <summary>
/// Atualiza um texto da UI com o dinheiro atual do MoneyManager (Fase 5 do roadmap).
/// Coloque este script no mesmo GameObject que tem o TextMeshProUGUI ou arraste a referência.
/// </summary>
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
