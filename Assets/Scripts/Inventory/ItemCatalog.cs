using UnityEngine;

/// <summary>Resolve ItemData em runtime (evita assets não carregados fora de Resources).</summary>
public static class ItemCatalog
{
    public static ItemData FindByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.skateItem != null &&
            settings.skateItem.itemName == itemName)
            return settings.skateItem;

        var fromResources = Resources.Load<ItemData>("Recomeco/Items/" + itemName);
        if (fromResources != null)
            return fromResources;

        var all = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in all)
        {
            if (item != null && item.itemName == itemName)
                return item;
        }

        return null;
    }
}