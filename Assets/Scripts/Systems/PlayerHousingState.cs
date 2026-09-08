using System;
using System.Collections.Generic;

public static class PlayerHousingState
{
    public const string CasaElegante = "casa_elegante";

    static readonly HashSet<string> s_Owned = new(StringComparer.Ordinal);

    public static bool Owns(string housingId)
    {
        return !string.IsNullOrEmpty(housingId) && s_Owned.Contains(housingId);
    }

    public static bool TryPurchase(string housingId, int priceCents)
    {
        if (string.IsNullOrEmpty(housingId) || Owns(housingId))
            return false;

        if (MoneyManager.instance == null || MoneyManager.instance.GetMoney() < priceCents)
            return false;

        MoneyManager.instance.RemoveMoney(priceCents);
        s_Owned.Add(housingId);
        return true;
    }

    public static void Grant(string housingId)
    {
        if (string.IsNullOrEmpty(housingId))
            return;

        s_Owned.Add(housingId);
    }

    public static string[] ExportOwned()
    {
        if (s_Owned.Count == 0)
            return Array.Empty<string>();

        var result = new string[s_Owned.Count];
        s_Owned.CopyTo(result);
        return result;
    }

    public static void ImportOwned(string[] housingIds)
    {
        s_Owned.Clear();
        if (housingIds == null)
            return;

        foreach (var id in housingIds)
        {
            if (!string.IsNullOrEmpty(id))
                s_Owned.Add(id);
        }
    }

    public static void Reset()
    {
        s_Owned.Clear();
    }
}
