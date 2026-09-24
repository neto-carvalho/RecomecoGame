using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;

    public Sprite icon;

    [Tooltip("Preço de venda por unidade na rua, em CENTAVOS (50 = R$ 0,50). 0 = não vende na rua.")]
    public int unitSellPriceCents;

    [Header("Consumo (FOOD4U / inventário)")]
    [Tooltip("Se true, o jogador pode usar o item para recuperar fome/vida.")]
    public bool consumable;

    [Tooltip("Fome recuperada ao consumir (0–100).")]
    public float hungerRestore;

    [Tooltip("Vida recuperada (ou perdida se negativo) ao consumir.")]
    public float healthRestore;

    [Tooltip("Reputação ganha ou perdida ao consumir.")]
    public float reputationRestore;

    [Tooltip("Texto curto na loja / inventário. Vazio = montado pelos valores acima.")]
    public string consumableHint;

    public bool CanStreetSell => unitSellPriceCents > 0;

    public bool IsMealItem => consumable && !CanStreetSell;

    public bool CanConsume =>
        consumable &&
        (Mathf.Abs(hungerRestore) > 0.001f ||
         Mathf.Abs(healthRestore) > 0.001f ||
         Mathf.Abs(reputationRestore) > 0.001f);

    public string GetEffectHint()
    {
        if (!string.IsNullOrEmpty(consumableHint))
            return consumableHint;

        if (!CanConsume)
            return string.Empty;

        return BuildEffectHint(hungerRestore, healthRestore, reputationRestore);
    }

    public static string BuildEffectHint(float hunger, float health, float reputation)
    {
        var parts = new System.Collections.Generic.List<string>(3);
        if (Mathf.Abs(hunger) > 0.001f)
            parts.Add(FormatSigned(hunger, " fome"));
        if (Mathf.Abs(health) > 0.001f)
            parts.Add(FormatSigned(health, " vida"));
        if (Mathf.Abs(reputation) > 0.001f)
            parts.Add(FormatSigned(reputation, " rep."));

        return parts.Count > 0 ? string.Join(", ", parts) : string.Empty;
    }

    static string FormatSigned(float value, string suffix)
    {
        var rounded = Mathf.RoundToInt(value);
        if (rounded > 0)
            return "+" + rounded + suffix;
        if (rounded < 0)
            return rounded + suffix;

        return string.Empty;
    }
}
