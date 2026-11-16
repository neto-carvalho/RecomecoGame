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

    [Tooltip("Proteção recuperada ao consumir (0 = usa padrão da lanchonete nas configurações).")]
    public float protectionRestore;

    [Tooltip("Texto curto na loja / inventário. Vazio = montado pelos valores acima.")]
    public string consumableHint;

    [Header("Equipamento")]
    [Tooltip("Ferramenta única (ex.: skate): aparece no inventário, não vende na rua.")]
    public bool gearItem;

    public bool CanStreetSell => unitSellPriceCents > 0;

    public bool IsMealItem => consumable && !CanStreetSell;

    public bool CanConsume =>
        consumable &&
        (Mathf.Abs(hungerRestore) > 0.001f ||
         Mathf.Abs(healthRestore) > 0.001f ||
         Mathf.Abs(reputationRestore) > 0.001f ||
         Mathf.Abs(protectionRestore) > 0.001f);

    public string GetEffectHint()
    {
        if (!string.IsNullOrEmpty(consumableHint))
            return consumableHint;

        if (!CanConsume)
            return string.Empty;

        return BuildEffectHint(hungerRestore, healthRestore, reputationRestore, protectionRestore);
    }

    public static string BuildEffectHint(float hunger, float health, float reputation, float protection = 0f)
    {
        var parts = new System.Collections.Generic.List<string>(4);
        if (Mathf.Abs(hunger) > 0.001f)
            parts.Add(FormatSigned(hunger, " fome"));
        if (Mathf.Abs(health) > 0.001f)
            parts.Add(FormatSigned(health, " vida"));
        if (Mathf.Abs(reputation) > 0.001f)
            parts.Add(FormatSigned(reputation, " rep."));
        if (Mathf.Abs(protection) > 0.001f)
            parts.Add(FormatSigned(protection, " prot."));

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
