using System.Text;
using UnityEngine;

/// <summary>
/// Loja onde o jogador compra pacotes de itens (ex.: Lojinha).
/// Perto da loja, as teclas 1..9 compram o pacote correspondente;
/// o preço sai do MoneyManager (centavos) e as unidades entram no Inventory.
/// </summary>
public class ShopZone : MonoBehaviour
{
    [System.Serializable]
    public class ShopProduct
    {
        [Tooltip("Item que o jogador recebe (uma unidade por slot do pacote)")]
        public ItemData item;

        [Tooltip("Quantas unidades vêm no pacote (ex.: pote de paçoca = 10)")]
        public int unitsPerPack = 10;

        [Tooltip("Preço do pacote na loja, em CENTAVOS (500 = R$ 5,00)")]
        public int packPriceCents = 500;

        [Tooltip("Nome do pacote mostrado na loja (ex.: Pote de paçoca). Vazio = nome do item.")]
        public string packLabel;

        public string DisplayName =>
            !string.IsNullOrEmpty(packLabel) ? packLabel : (item != null ? item.itemName : "?");
    }

    [Tooltip("Distância máxima para comprar")]
    public float interactDistance = 4f;

    [Tooltip("Título mostrado em cima da lista")]
    public string shopTitle = "LOJINHA";

    public ShopProduct[] products;

    bool _playerWasInRange;
    float _feedbackTimer;
    string _feedbackText;

    void Update()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var inRange = player != null &&
                      Vector3.Distance(player.transform.position, transform.position) < interactDistance;

        if (inRange && !_playerWasInRange)
            ShowCatalog();
        if (!inRange && _playerWasInRange)
            InteractionUI.HideMessage(this);
        _playerWasInRange = inRange;

        if (!inRange || player == null)
            return;

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f)
                ShowCatalog();
        }

        for (var i = 0; i < products.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                TryBuy(products[i], player);
        }
    }

    void ShowCatalog()
    {
        var sb = new StringBuilder();
        sb.Append("<b>").Append(shopTitle).Append("</b> — aperte o número para comprar:\n");
        for (var i = 0; i < products.Length && i < 9; i++)
        {
            var p = products[i];
            if (p == null || p.item == null)
                continue;

            sb.Append('[').Append(i + 1).Append("] ")
              .Append(p.DisplayName)
              .Append(" (").Append(p.unitsPerPack).Append(" un) ")
              .Append(MoneyManager.FormatBRL(p.packPriceCents));
            if (i < products.Length - 1)
                sb.Append(i % 2 == 0 ? "   " : "\n");
        }

        InteractionUI.ShowMessage(sb.ToString(), this);
    }

    void TryBuy(ShopProduct product, GameObject player)
    {
        if (product == null || product.item == null)
            return;

        if (MoneyManager.instance == null)
        {
            Debug.LogWarning("ShopZone: MoneyManager não encontrado.");
            return;
        }

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("ShopZone: Inventory não encontrado.");
            return;
        }

        if (MoneyManager.instance.GetMoney() < product.packPriceCents)
        {
            ShowFeedback("Dinheiro insuficiente para " + product.DisplayName + "!");
            return;
        }

        if (GetInventoryCapacity(inventory, product.item) < product.unitsPerPack)
        {
            ShowFeedback("Inventário sem espaço para " + product.unitsPerPack + " un!");
            return;
        }

        MoneyManager.instance.RemoveMoney(product.packPriceCents);
        for (var i = 0; i < product.unitsPerPack; i++)
            inventory.AddItem(product.item);

        ShowFeedback(
            "Comprou " + product.DisplayName + " (" + product.unitsPerPack + " un) por " +
            MoneyManager.FormatBRL(product.packPriceCents));
    }

    static int GetInventoryCapacity(Inventory inventory, ItemData item)
    {
        if (inventory.slots == null)
            return 0;

        var capacity = 0;
        foreach (var slot in inventory.slots)
        {
            if (slot == null)
                continue;
            if (slot.IsEmpty())
                capacity += inventory.maxStackPerSlot;
            else if (slot.CanStack(item))
                capacity += Mathf.Max(0, inventory.maxStackPerSlot - slot.quantity);
        }

        return capacity;
    }

    void ShowFeedback(string text)
    {
        _feedbackText = text;
        _feedbackTimer = 1.6f;
        InteractionUI.ShowMessage(_feedbackText, this);
    }
}
