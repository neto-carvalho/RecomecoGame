using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Tooltip("Nùmero de slots do inventùrio")]
    public int slotCount = 12;

    [Tooltip("Quantidade mùxima do mesmo item por slot (stack)")]
    public int maxStackPerSlot = 20;

    public InventorySlot[] slots;
    public SlotUI[] slotUIs;

    void Awake()
    {
        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();

        // Se slotUIs nùo foi preenchido no Inspector, tenta achar no painel do inventùrio
        if (slotUIs == null || slotUIs.Length == 0)
        {
            InventoryUI invUI = FindFirstObjectByType<InventoryUI>();
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

    /// <summary>Atualiza a exibiùùo de todos os slots na UI. Chamar ao abrir o inventùrio.</summary>
    public void RefreshAllSlots()
    {
        TryFindSlotUIsIfMissing();
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

    void TryFindSlotUIsIfMissing()
    {
        if (slotUIs != null && slotUIs.Length > 0) return;
        InventoryUI invUI = FindFirstObjectByType<InventoryUI>();
        if (invUI == null || invUI.inventoryPanel == null) return;
        SlotUI[] found = GetSlotUIsInOrder(invUI.inventoryPanel.transform);
        if (found == null || found.Length == 0) return;
        slotCount = Mathf.Min(slotCount, found.Length);
        slotUIs = new SlotUI[slotCount];
        for (int i = 0; i < slotCount; i++)
            slotUIs[i] = found[i];
        if (slots == null || slots.Length < slotCount)
        {
            InventorySlot[] old = slots;
            slots = new InventorySlot[slotCount];
            for (int i = 0; i < slotCount; i++)
                slots[i] = (old != null && i < old.Length) ? old[i] : new InventorySlot();
        }
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;
        int n = GetSlotCount();
        if (n == 0) return false;

        // 1) Tentar empilhar em slot existente (respeitando limite por slot)
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

        // 2) Usar um slot vazio (quando o primeiro estù cheio ou para novo tipo de item)
        for (int i = 0; i < n; i++)
        {
            if (!slots[i].IsEmpty()) continue;
            slots[i].item = item;
            slots[i].quantity = 1;
            if (slotUIs != null && i < slotUIs.Length && slotUIs[i] != null)
            {
                slotUIs[i].SetItem(item, 1);
                if (i > 0) UnityEngine.Debug.Log("[Inventario] Item no slot " + (i + 1) + ": " + item.itemName);
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

    /// <summary>Obtùm os SlotUIs na ordem exata dos filhos do painel (Slot, Slot (1), Slot (2)...).</summary>
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

    /// <summary>Retorna a quantidade total de itens com o nome dado no inventùrio.</summary>
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

    /// <summary>Remove atù 'amount' itens com o nome dado. Retorna quantos foram removidos.</summary>
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