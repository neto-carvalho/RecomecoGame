using System;
using UnityEngine;

public static class GameSession
{
    [Serializable]
    public struct SlotSnapshot
    {
        public string itemName;
        public int quantity;
    }

    static SlotSnapshot[] _slots;
    static int _money = -1;
    static bool _hasInventory;

    public static void SaveBeforeSceneLoad()
    {
        if (MoneyManager.instance != null)
            _money = MoneyManager.instance.GetMoney();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        var inv = player.GetComponent<Inventory>();
        if (inv == null || inv.slots == null)
            return;

        _slots = new SlotSnapshot[inv.slots.Length];
        for (int i = 0; i < inv.slots.Length; i++)
        {
            var slot = inv.slots[i];
            if (slot == null || slot.IsEmpty())
            {
                _slots[i] = default;
                continue;
            }

            _slots[i] = new SlotSnapshot
            {
                itemName = slot.item != null ? slot.item.itemName : null,
                quantity = slot.quantity
            };
        }

        _hasInventory = true;
    }

    public static void ApplyToPlayer(GameObject player)
    {
        if (player == null)
            return;

        if (_money >= 0 && MoneyManager.instance != null)
            MoneyManager.instance.SetMoney(_money);

        if (!_hasInventory || _slots == null)
            return;

        var inv = player.GetComponent<Inventory>();
        if (inv == null || inv.slots == null)
            return;

        int n = Mathf.Min(inv.slots.Length, _slots.Length);
        for (int i = 0; i < n; i++)
        {
            inv.slots[i] = new InventorySlot();
            var snap = _slots[i];
            if (string.IsNullOrEmpty(snap.itemName) || snap.quantity <= 0)
                continue;

            var data = FindItemByName(snap.itemName);
            if (data == null)
                continue;

            inv.slots[i].item = data;
            inv.slots[i].quantity = snap.quantity;
        }

        inv.RefreshAllSlots();
    }

    public static void Reset()
    {
        _slots = null;
        _money = -1;
        _hasInventory = false;
    }

    static ItemData FindItemByName(string itemName)
    {
        var all = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in all)
        {
            if (item != null && item.itemName == itemName)
                return item;
        }

        return null;
    }
}
