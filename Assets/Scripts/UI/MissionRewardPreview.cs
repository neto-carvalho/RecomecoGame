using UnityEngine;

/// <summary>Ícone e texto de recompensa exibidos no painel de missão.</summary>
public static class MissionRewardPreview
{
    static Sprite s_moneyIcon;

    public enum GrantStyle
    {
        /// <summary>Item/dinheiro só ao fazer a ação (comprar, vender).</summary>
        EarnByPlaying,

        /// <summary>Entrega automática ao concluir a missão (ex.: skate).</summary>
        OnMissionComplete,
    }

    public struct Line
    {
        public bool Show;
        public string Label;
        public Sprite Icon;
        public GrantStyle Style;
    }

    public static Line GetForMission(MissionId id)
    {
        var settings = RecomecoGameplaySettings.Instance;

        switch (id)
        {
            case MissionId.CollectCans:
            case MissionId.CollectCansForHouse:
                return MoneyLine("dinheiro no ferro velho");

            case MissionId.SellAtJunkyard:
            case MissionId.SellAtJunkyardForHouse:
                return MoneyLine("dinheiro");

            case MissionId.BuyAtShop:
                return ItemLine(ResolveShopPackItem(), "Pacote", GrantStyle.OnMissionComplete);

            case MissionId.Resell:
                if (settings != null && settings.skateUnlockOnResellMission && !PlayerUnlocks.HasSkate)
                    return ItemLine(ResolveSkateItem(), "Skate", GrantStyle.OnMissionComplete);
                return default;

            case MissionId.BuyMealAtFood4U:
                return ItemLine(ResolveMealItem(), "Lanche", GrantStyle.OnMissionComplete);

            default:
                return default;
        }
    }

    static Line MoneyLine(string label)
    {
        return new Line
        {
            Show = true,
            Label = "Você recebe: " + label,
            Icon = MoneyIcon(),
            Style = GrantStyle.EarnByPlaying,
        };
    }

    static Line ItemLine(ItemData item, string shortName, GrantStyle style)
    {
        var icon = item != null ? item.icon : null;
        var name = item != null && !string.IsNullOrEmpty(item.itemName) ? item.itemName : shortName;
        var label = style == GrantStyle.OnMissionComplete
            ? "Recompensa ao concluir: " + name
            : "Você recebe: " + name;

        return new Line
        {
            Show = true,
            Label = label,
            Icon = icon,
            Style = style,
        };
    }

    static Sprite MoneyIcon()
    {
        if (s_moneyIcon == null)
            s_moneyIcon = Resources.Load<Sprite>("UI/icone_dinheiro");
        return s_moneyIcon;
    }

    static ItemData ResolveSkateItem()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.skateItem != null)
            return settings.skateItem;

        return PlayerSkateItems.ResolveItemData();
    }

    public static bool TryGetShopPackGrant(out ItemData item, out int unitsPerPack)
    {
        item = ResolveShopPackItem();
        unitsPerPack = 10;

        foreach (var shop in Object.FindObjectsByType<ShopZone>(FindObjectsSortMode.None))
        {
            if (shop == null || shop.products == null || shop.products.Length == 0)
                continue;
            if (shop.shopKind != ShopZone.ShopKind.Resell)
                continue;

            foreach (var product in shop.products)
            {
                if (product?.item == null || product.item.consumable)
                    continue;
                item = product.item;
                unitsPerPack = Mathf.Max(1, product.unitsPerPack);
                return true;
            }
        }

        return item != null;
    }

    public static ItemData ResolveMealItemForGrant() => ResolveMealItem();

    static ItemData ResolveShopPackItem()
    {
        foreach (var shop in Object.FindObjectsByType<ShopZone>(FindObjectsSortMode.None))
        {
            if (shop == null || shop.products == null || shop.products.Length == 0)
                continue;

            if (shop.shopKind != ShopZone.ShopKind.Resell)
                continue;

            foreach (var product in shop.products)
            {
                if (product != null && product.item != null && !product.item.consumable)
                    return product.item;
            }
        }

        return ItemCatalog.FindByName("Chiclete")
               ?? ItemCatalog.FindByName("Cocada")
               ?? ItemCatalog.FindByName("Biscoito");
    }

    static ItemData ResolveMealItem()
    {
        foreach (var shop in Object.FindObjectsByType<ShopZone>(FindObjectsSortMode.None))
        {
            if (shop == null || shop.products == null)
                continue;

            if (shop.shopKind != ShopZone.ShopKind.FastFood)
                continue;

            foreach (var product in shop.products)
            {
                if (product?.item != null && product.item.CanConsume)
                    return product.item;
            }
        }

        return ItemCatalog.FindByName("LancheClassico")
               ?? ItemCatalog.FindByName("HotDog")
               ?? ItemCatalog.FindByName("X-Salada");
    }
}