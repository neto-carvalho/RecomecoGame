using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;

    public Sprite icon;

    [Tooltip("Preço de venda por unidade na rua, em CENTAVOS (50 = R$ 0,50). 0 = não vende na rua.")]
    public int unitSellPriceCents;
}