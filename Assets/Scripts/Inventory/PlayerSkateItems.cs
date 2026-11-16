using UnityEngine;

public static class PlayerSkateItems
{
    public const string ItemName = "Skate";

    public static ItemData ResolveItemData()
    {
        return ItemCatalog.FindByName(ItemName);
    }

    public static Inventory ResolveInventory()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var onPlayer = player.GetComponent<Inventory>();
            if (onPlayer != null)
                return onPlayer;
        }

        return Object.FindFirstObjectByType<Inventory>();
    }

    public static bool PlayerOwnsSkate()
    {
        var inventory = ResolveInventory();
        return inventory != null && inventory.GetItemCount(ItemName) > 0;
    }

  /// <returns>true se ganhou agora ou já tinha.</returns>
    public static bool TryGrantToPlayer(out string message)
    {
        message = null;
        var item = ResolveItemData();
        if (item == null)
        {
            message = "Item Skate não encontrado (Assets/Items/Skate.asset).";
            return false;
        }

        var inventory = ResolveInventory();
        if (inventory == null)
        {
            message = "Inventário não encontrado.";
            return false;
        }

        inventory.ReconnectUi();

        if (inventory.GetItemCount(ItemName) > 0)
        {
            message = "Você já tem um skate no inventário.";
            return true;
        }

        if (!inventory.AddItem(item))
        {
            message = "Inventário cheio — não coube o skate.";
            return false;
        }

        inventory.RefreshAllSlots();
        message = "Skate no inventário. Na cidade, aperte B para usar.";
        return true;
    }
}