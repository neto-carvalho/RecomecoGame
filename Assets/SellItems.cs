using UnityEngine;

/// <summary>
/// Zona de venda (ex.: ferro velho). Vende itens do inventário e adiciona dinheiro pelo MoneyManager.
/// </summary>
public class SellItems : MonoBehaviour
{
    [Tooltip("Distância máxima para o jogador poder vender (E)")]
    public float sellDistance = 3f;

    [Tooltip("Nome do item vendido (ex: Latinha). Deve bater com ItemData.itemName.")]
    public string itemName = "Latinha";

    [Tooltip("Valor por unidade vendida, em CENTAVOS (200 = R$ 2,00)")]
    public int pricePerUnit = 200;

    [Tooltip("Mensagem exibida quando o jogador está perto (ex.: Aperte E para vender)")]
    public string messageNear = "Aperte E para vender";

    private bool playerWasInRange;

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool playerInRange = player != null && Vector3.Distance(player.transform.position, transform.position) < sellDistance;

        // Mostra ou esconde a mensagem ao se aproximar/afastar
        if (playerInRange && !playerWasInRange)
            InteractionUI.ShowMessage(messageNear, this);
        if (!playerInRange && playerWasInRange)
            InteractionUI.HideMessage(this);
        playerWasInRange = playerInRange;

        if (!playerInRange || player == null) return;

        if (!Input.GetKeyDown(KeyCode.E)) return;

        Inventory inv = player.GetComponent<Inventory>();
        if (inv == null) inv = FindFirstObjectByType<Inventory>();
        if (inv == null)
        {
            UnityEngine.Debug.LogWarning("SellItems: Inventário não encontrado.");
            return;
        }

        if (MoneyManager.instance == null)
        {
            UnityEngine.Debug.LogWarning("SellItems: MoneyManager não encontrado.");
            return;
        }

        int count = inv.GetItemCount(itemName);
        if (count <= 0)
        {
            UnityEngine.Debug.Log("Nenhum item para vender.");
            return;
        }

        int removed = inv.RemoveItem(itemName, count);
        int total = removed * pricePerUnit;
        MoneyManager.instance.AddMoney(total);
        UnityEngine.Debug.Log("Vendido: " + removed + " x " + itemName + " = " + MoneyManager.FormatBRL(total));
    }
}