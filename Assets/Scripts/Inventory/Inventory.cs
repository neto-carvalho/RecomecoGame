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
}