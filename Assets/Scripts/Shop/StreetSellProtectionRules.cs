using UnityEngine;

/// <summary>
/// Chance de o pedestre aceitar abrir a venda conforme a barra Proteção (0–100%).
/// </summary>
public static class StreetSellProtectionRules
{
    public static float GetNegotiateAcceptChance(PlayerNeeds needs)
    {
        if (needs == null)
            return 1f;

        var protection = Mathf.Clamp(needs.Protection, 0f, PlayerNeeds.MaxProtection);
        if (protection <= 0f)
            return 0f;

        if (protection >= PlayerNeeds.MaxProtection - 0.01f)
            return 1f;

        return protection / PlayerNeeds.MaxProtection;
    }

    public static bool TryPedestrianAcceptsNegotiation(PlayerNeeds needs, out string refuseMessage, out bool showBannerTip)
    {
        refuseMessage = null;
        showBannerTip = false;

        var chance = GetNegotiateAcceptChance(needs);
        if (chance <= 0f)
        {
            var settings = RecomecoGameplaySettings.Instance;
            refuseMessage = settings != null
                ? settings.npcSellRefuseZeroProtectionTip
                : "Coma algo na lanchonete para recuperar proteção.";
            showBannerTip = true;
            return false;
        }

        if (chance >= 1f - 0.0001f)
            return true;

        if (Random.value <= chance)
            return true;

        var cfg = RecomecoGameplaySettings.Instance;
        refuseMessage = cfg != null
            ? cfg.npcSellRefuseRandomTip
            : "O pedestre não quis negociar.";
        showBannerTip = needs != null && needs.Protection < 50f;
        return false;
    }
}