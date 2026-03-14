using UnityEngine;
using TMPro;

/// <summary>
/// Atualiza um texto da UI com o dinheiro atual do MoneyManager (Fase 5 do roadmap).
/// Coloque este script no mesmo GameObject que tem o TextMeshProUGUI ou arraste a referência.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class MoneyUI : MonoBehaviour
{
    [Tooltip("Prefixo exibido antes do valor (ex: \"Dinheiro: R$\")")]
    public string prefix = "R$ ";

    private TextMeshProUGUI textField;

    void Start()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (textField == null) return;
        if (MoneyManager.instance == null)
        {
            textField.text = prefix + "0";
            return;
        }
        textField.text = prefix + MoneyManager.instance.GetMoney().ToString();
    }
}
