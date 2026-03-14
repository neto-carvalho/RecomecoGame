using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int slotCount = 12;

    public InventorySlot[] slots;
    public SlotUI[] slotUIs;

    void Awake()
    {
        slots = new InventorySlot[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = new InventorySlot();
        }

        UnityEngine.Debug.Log("Inventário iniciado com " + slotCount + " slots");
    }

    public bool AddItem(ItemData item)
    {
        // STACK
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].CanStack(item))
            {
                slots[i].quantity++;

                slotUIs[i].SetItem(slots[i].item, slots[i].quantity);

                return true;
            }
        }

        // SLOT VAZIO
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty())
            {
                slots[i].item = item;
                slots[i].quantity = 1;

                slotUIs[i].SetItem(item, 1);

                return true;
            }
        }

        return false;
    }

    /// <summary>Retorna a quantidade total de itens com o nome dado no inventário.</summary>
    public int GetItemCount(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return 0;
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].item.itemName == itemName)
                total += slots[i].quantity;
        }
        return total;
    }

    /// <summary>Remove até 'amount' itens com o nome dado. Retorna quantos foram removidos.</summary>
    public int RemoveItem(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName) || amount <= 0) return 0;
        int removed = 0;
        for (int i = 0; i < slots.Length && removed < amount; i++)
        {
            if (slots[i].IsEmpty() || slots[i].item.itemName != itemName) continue;

            int take = Mathf.Min(slots[i].quantity, amount - removed);
            slots[i].quantity -= take;
            removed += take;

            if (slots[i].quantity <= 0)
            {
                slots[i].item = null;
                slots[i].quantity = 0;
                slotUIs[i].SetItem(null, 0);
            }
            else
            {
                slotUIs[i].SetItem(slots[i].item, slots[i].quantity);
            }
        }
        return removed;
    }
}