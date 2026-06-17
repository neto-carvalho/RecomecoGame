using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Tooltip("N?mero de slots do invent?rio")]
    public int slotCount = 12;

    [Tooltip("Quantidade m?xima do mesmo item por slot (stack)")]
    public int maxStackPerSlot = 20;

    public InventorySlot[] slots;
    public SlotUI[] slotUIs;

    void Awake()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();

       
        if (slotUIs == null || slotUIs.Length == 0)
        {
            InventoryUI invUI = GameplayHudBootstrap.ResolveInventoryUi();
            if (invUI != null && invUI.inventoryPanel != null)
            {
                SlotUI[] found = GetSlotUIsInOrder(invUI.inventoryPanel.transform);
                if (found != null && found.Length > 0)
                {
                    slotCount = Mathf.Min(slotCount, found.Length);
                    slotUIs = new SlotUI[slotCount];
                    slots = new InventorySlot[slotCount];
                    for (int i = 0; i < slotCount; i++)
                    {
                        slotUIs[i] = found[i];
                        slots[i] = new InventorySlot();
                    }
                }
            }
            if (slotUIs == null || slotUIs.Length == 0)
                UnityEngine.Debug.LogWarning("Inventory: atribua os SlotUIs no Inspector ou use um painel com filhos Slot (com SlotUI).");
        }
    }

    public void ReconnectUi()
    {
        BindSlotUIsFromActiveInventoryUi();
        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        EnsureSlotUiBinding();
        int n = GetSlotCount();
        if (n == 0) return;
        for (int i = 0; i < n; i++)
        {
            if (slotUIs[i] == null) continue;
            if (slots[i].IsEmpty())
                slotUIs[i].SetItem(null, 0);
            else
                slotUIs[i].SetItem(slots[i].item, slots[i].quantity);
        }
    }

    void EnsureSlotUiBinding()
    {
        if (AreSlotUIsBoundToActiveInventoryUi())
            return;

        BindSlotUIsFromActiveInventoryUi();
    }

    bool AreSlotUIsBoundToActiveInventoryUi()
    {
        if (slotUIs == null || slotUIs.Length == 0)
            return false;

        InventoryUI invUI = GameplayHudBootstrap.ResolveInventoryUi();
        if (invUI == null || invUI.inventoryPanel == null)
            return false;

        Transform panel = invUI.inventoryPanel.transform;
        foreach (SlotUI slotUi in slotUIs)
        {
            if (slotUi == null || !slotUi.gameObject.activeInHierarchy)
                return false;
            if (!slotUi.transform.IsChildOf(panel))
                return false;
        }

        return true;
    }

    void BindSlotUIsFromActiveInventoryUi()
    {
        InventoryUI invUI = GameplayHudBootstrap.ResolveInventoryUi();
        if (invUI == null || invUI.inventoryPanel == null)
            return;

        SlotUI[] found = GetSlotUIsInOrder(invUI.inventoryPanel.transform);
        if (found == null || found.Length == 0)
            return;

        int uiCount = found.Length;
        slotUIs = new SlotUI[uiCount];
        for (int i = 0; i < uiCount; i++)
            slotUIs[i] = found[i];

        if (slots == null || slots.Length < uiCount)
        {
            InventorySlot[] old = slots;
            slots = new InventorySlot[uiCount];
            for (int i = 0; i < uiCount; i++)
                slots[i] = (old != null && i < old.Length) ? old[i] : new InventorySlot();
        }

        slotCount = Mathf.Min(slotCount, uiCount);
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;
        EnsureSlotUiBinding();
        int n = GetSlotCount();
        if (n == 0) return false;

       
        for (int i = 0; i < n; i++)
        {
            if (slots[i].IsEmpty() || !slots[i].CanStack(item)) continue;
            int space = maxStackPerSlot - slots[i].quantity;
            if (space <= 0) continue;

            slots[i].quantity += 1;
            if (slotUIs != null && i < slotUIs.Length && slotUIs[i] != null)
                slotUIs[i].SetItem(slots[i].item, slots[i].quantity);
            return true;
        }

       
        for (int i = 0; i < n; i++)
        {
            if (!slots[i].IsEmpty()) continue;
            slots[i].item = item;
            slots[i].quantity = 1;
            if (slotUIs != null && i < slotUIs.Length && slotUIs[i] != null)
            {
                slotUIs[i].SetItem(item, 1);
            }
            return true;
        }

        return false;
    }

    int GetSlotCount()
    {
        if (slots == null) return 0;
        if (slotUIs == null) return slots.Length;
        return Mathf.Min(slots.Length, slotUIs.Length);
    }

    static SlotUI[] GetSlotUIsInOrder(Transform panel)
    {
        if (panel == null) return null;
        var list = new System.Collections.Generic.List<SlotUI>();
        for (int i = 0; i < panel.childCount; i++)
        {
            SlotUI su = panel.GetChild(i).GetComponent<SlotUI>();
            if (su != null) list.Add(su);
        }
        return list.Count > 0 ? list.ToArray() : null;
    }

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
                if (slotUIs != null && i < slotUIs.Length && slotUIs[i] != null)
                    slotUIs[i].SetItem(null, 0);
            }
            else
            {
                if (slotUIs != null && i < slotUIs.Length && slotUIs[i] != null)
                    slotUIs[i].SetItem(slots[i].item, slots[i].quantity);
            }
        }
        return removed;
    }
}
