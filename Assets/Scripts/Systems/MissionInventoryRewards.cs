using UnityEngine;

/// <summary>Entrega itens de recompensa de missão no inventário do jogador.</summary>
public static class MissionInventoryRewards
{
    public static Inventory ResolveInventory()
    {
        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var onPlayer = player.GetComponent<Inventory>();
            if (onPlayer != null)
                return onPlayer;
        }

        return Object.FindFirstObjectByType<Inventory>();
    }

    /// <returns>true se colocou tudo (ou já tinha o necessário).</returns>
    public static bool TryGrant(ItemData item, int quantity, out string message, bool showTip = true)
    {
        message = null;
        if (item == null)
        {
            message = "Recompensa: item não configurado no jogo.";
            return false;
        }

        if (quantity <= 0)
            return true;

        var inventory = ResolveInventory();
        if (inventory == null)
        {
            message = "Recompensa: inventário não encontrado.";
            return false;
        }

        inventory.ReconnectUi();

        var added = inventory.AddItems(item, quantity);
        if (added < quantity)
        {
            message = "Inventário cheio — couberam só " + added + " de " + quantity + " (" + item.itemName + ").";
            if (showTip && added > 0)
                GameplayTipBannerUI.Show(message, 8f);
            return added > 0;
        }

        message = item.itemName + " ×" + quantity + " no inventário (I ou Tab).";
        if (showTip)
            GameplayTipBannerUI.Show("Missão concluída! " + message, 9f);

        return true;
    }

    public static void GrantCompletionReward(MissionId completed)
    {
        switch (completed)
        {
            case MissionId.BuyAtShop:
                GrantShopPackReward();
                break;
            case MissionId.BuyMealAtFood4U:
                GrantMealRewardIfMissing();
                break;
        }
    }

    static void GrantShopPackReward()
    {
        if (!MissionRewardPreview.TryGetShopPackGrant(out var item, out var unitsPerPack))
            return;

        var inventory = ResolveInventory();
        if (inventory == null)
            return;

        var have = inventory.GetItemCount(item.itemName);
        var need = Mathf.Max(0, unitsPerPack - have);
        if (need <= 0)
        {
            GameplayTipBannerUI.Show(
                "Pacotes no inventário. Próximo passo: revender na rua (I ou Tab).",
                8f);
            return;
        }

        TryGrant(item, need, out _);
    }

    static void GrantMealRewardIfMissing()
    {
        var item = MissionRewardPreview.ResolveMealItemForGrant();
        if (item == null)
            return;

        var inventory = ResolveInventory();
        if (inventory != null && inventory.GetItemCount(item.itemName) > 0)
            return;

        TryGrant(item, 1, out _);
    }
}