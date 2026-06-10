using UnityEngine;

/// <summary>
/// Ponto de venda na rua: aperte E para vender 1 unidade por vez.
/// O preço unitário vem de ItemData.unitSellPriceCents (itens com 0 não vendem).
/// </summary>
public class StreetSellZone : MonoBehaviour
{
    [Tooltip("Distância máxima para vender")]
    public float interactDistance = 3.5f;

    [Tooltip("Texto antes do item (ex.: Aperte E para vender)")]
    public string messagePrefix = "Aperte E para vender";

    bool _playerWasInRange;
    float _feedbackTimer;

    void Update()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var inRange = player != null &&
                      Vector3.Distance(player.transform.position, transform.position) < interactDistance;

        if (!inRange && _playerWasInRange)
            InteractionUI.HideMessage(this);

        if (inRange && player != null)
        {
            var inventory = GetInventory(player);

            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
            }
            else if (!_playerWasInRange || _feedbackTimer <= 0f)
            {
                ShowOffer(inventory);
            }

            if (Input.GetKeyDown(KeyCode.E))
                TrySellOne(inventory);
        }

        _playerWasInRange = inRange;
    }

    static Inventory GetInventory(GameObject player)
    {
        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = Object.FindFirstObjectByType<Inventory>();
        return inventory;
    }

    void ShowOffer(Inventory inventory)
    {
        var (item, _) = FindNextSellable(inventory);
        if (item == null)
        {
            InteractionUI.ShowMessage("Nada para vender na rua (compre na Lojinha).", this);
            return;
        }

        InteractionUI.ShowMessage(
            messagePrefix + " 1x " + item.itemName + " — " +
            MoneyManager.FormatBRL(item.unitSellPriceCents), this);
    }

    void TrySellOne(Inventory inventory)
    {
        if (inventory == null || MoneyManager.instance == null)
            return;

        var (item, _) = FindNextSellable(inventory);
        if (item == null)
            return;

        var removed = inventory.RemoveItem(item.itemName, 1);
        if (removed <= 0)
            return;

        MoneyManager.instance.AddMoney(item.unitSellPriceCents);

        InteractionUI.ShowMessage(
            "Vendeu 1x " + item.itemName + " por " + MoneyManager.FormatBRL(item.unitSellPriceCents), this);
        _feedbackTimer = 1.2f;
    }

    static (ItemData item, int count) FindNextSellable(Inventory inventory)
    {
        if (inventory == null || inventory.slots == null)
            return (null, 0);

        foreach (var slot in inventory.slots)
        {
            if (slot == null || slot.IsEmpty() || slot.item == null)
                continue;
            if (slot.item.unitSellPriceCents <= 0)
                continue;
            return (slot.item, slot.quantity);
        }

        return (null, 0);
    }
}
