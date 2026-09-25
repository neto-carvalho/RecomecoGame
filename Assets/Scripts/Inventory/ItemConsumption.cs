using UnityEngine;

public static class ItemConsumption
{
    public static bool TryConsumeFromInventory(Inventory inventory, ItemData item, GameObject player)
    {
        if (inventory == null || item == null || player == null || !item.CanConsume)
            return false;

        if (inventory.GetItemCount(item.itemName) <= 0)
            return false;

        if (!ApplyEffects(player, item))
            return false;

        inventory.RemoveItem(item.itemName, 1);
        return true;
    }

    public static bool ApplyEffects(GameObject player, ItemData item)
    {
        if (player == null || item == null || !item.CanConsume)
            return false;

        var needs = player.GetComponent<PlayerNeeds>();
        if (needs == null)
            return false;

        if (Mathf.Abs(item.hungerRestore) > 0.001f)
            needs.AddHunger(item.hungerRestore);
        if (Mathf.Abs(item.healthRestore) > 0.001f)
            needs.AddHealth(item.healthRestore);
        if (Mathf.Abs(item.reputationRestore) > 0.001f)
            needs.AddReputation(item.reputationRestore);

        if (Mathf.Abs(item.hungerRestore) > 0.001f)
            MissionProgress.NotifyAteFood();

        return true;
    }
}
