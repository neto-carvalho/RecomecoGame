using System.Collections.Generic;
using UnityEngine;

public class HomeStorage : MonoBehaviour
{
    [Tooltip("ID único deste armário (salvo no arquivo de save)")]
    public string storageId = "casa_elegante_armario";

    [Tooltip("Quantidade de slots do armário")]
    public int slotCount = 12;

    [Tooltip("Stack máximo por slot")]
    public int maxStackPerSlot = 20;

    public InventorySlot[] slots;

    static readonly Dictionary<string, HomeStorageSnapshot> PendingById = new();

    void Awake()
    {
        EnsureSlots();
        TryApplyPendingSnapshot();
    }

    void TryApplyPendingSnapshot()
    {
        if (string.IsNullOrEmpty(storageId))
            return;

        if (!PendingById.TryGetValue(storageId, out var snapshot))
            return;

        ImportSnapshot(snapshot);
        PendingById.Remove(storageId);
    }

    void EnsureSlots()
    {
        if (slots != null && slots.Length == slotCount)
            return;

        slots = new InventorySlot[slotCount];
        for (var i = 0; i < slotCount; i++)
            slots[i] = new InventorySlot();
    }

    public bool TryDepositOne(Inventory playerInventory, int playerSlotIndex)
    {
        if (playerInventory == null || playerInventory.slots == null)
            return false;

        if (playerSlotIndex < 0 || playerSlotIndex >= playerInventory.slots.Length)
            return false;

        var playerSlot = playerInventory.slots[playerSlotIndex];
        if (playerSlot == null || playerSlot.IsEmpty())
            return false;

        EnsureSlots();

        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty() || !slots[i].CanStack(playerSlot.item))
                continue;

            var space = maxStackPerSlot - slots[i].quantity;
            if (space <= 0)
                continue;

            if (slots[i].IsEmpty())
            {
                slots[i].item = playerSlot.item;
                slots[i].quantity = 0;
            }

            slots[i].quantity += 1;
            playerSlot.quantity -= 1;
            if (playerSlot.quantity <= 0)
            {
                playerSlot.item = null;
                playerSlot.quantity = 0;
            }

            playerInventory.RefreshAllSlots();
            return true;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty())
                continue;

            slots[i].item = playerSlot.item;
            slots[i].quantity = 1;
            playerSlot.quantity -= 1;
            if (playerSlot.quantity <= 0)
            {
                playerSlot.item = null;
                playerSlot.quantity = 0;
            }

            playerInventory.RefreshAllSlots();
            return true;
        }

        return false;
    }

    public bool TryWithdrawOne(Inventory playerInventory, int storageSlotIndex)
    {
        if (playerInventory == null || playerInventory.slots == null)
            return false;

        EnsureSlots();
        if (storageSlotIndex < 0 || storageSlotIndex >= slots.Length)
            return false;

        var storageSlot = slots[storageSlotIndex];
        if (storageSlot == null || storageSlot.IsEmpty())
            return false;

        if (!playerInventory.AddItem(storageSlot.item))
            return false;

        storageSlot.quantity -= 1;
        if (storageSlot.quantity <= 0)
        {
            storageSlot.item = null;
            storageSlot.quantity = 0;
        }

        return true;
    }

    public string BuildSummary()
    {
        EnsureSlots();
        var parts = new List<string>();
        var empty = true;

        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty())
                continue;

            empty = false;
            parts.Add((i + 1) + ") " + slots[i].item.itemName + " x" + slots[i].quantity);
        }

        if (empty)
            return "Armário vazio.";

        return string.Join("   ", parts);
    }

    public HomeStorageSnapshot ExportSnapshot()
    {
        EnsureSlots();
        var snap = new HomeStorageSnapshot
        {
            storageId = storageId,
            slots = new GameSession.SlotSnapshot[slots.Length],
        };

        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty())
            {
                snap.slots[i] = default;
                continue;
            }

            snap.slots[i] = new GameSession.SlotSnapshot
            {
                itemName = slots[i].item != null ? slots[i].item.itemName : null,
                quantity = slots[i].quantity,
            };
        }

        return snap;
    }

    public void ImportSnapshot(HomeStorageSnapshot snapshot)
    {
        EnsureSlots();
        if (snapshot.slots == null)
            return;

        var n = Mathf.Min(slots.Length, snapshot.slots.Length);
        for (var i = 0; i < n; i++)
        {
            slots[i] = new InventorySlot();
            var itemSnap = snapshot.slots[i];
            if (string.IsNullOrEmpty(itemSnap.itemName) || itemSnap.quantity <= 0)
                continue;

            var data = FindItemByName(itemSnap.itemName);
            if (data == null)
                continue;

            slots[i].item = data;
            slots[i].quantity = itemSnap.quantity;
        }
    }

    public static HomeStorageSnapshot[] ExportAll()
    {
        var storages = Object.FindObjectsByType<HomeStorage>(FindObjectsSortMode.None);
        if (storages == null || storages.Length == 0)
            return null;

        var result = new HomeStorageSnapshot[storages.Length];
        for (var i = 0; i < storages.Length; i++)
            result[i] = storages[i].ExportSnapshot();

        return result;
    }

    public static void ImportAll(HomeStorageSnapshot[] snapshots)
    {
        PendingById.Clear();
        if (snapshots == null || snapshots.Length == 0)
            return;

        foreach (var snapshot in snapshots)
        {
            if (string.IsNullOrEmpty(snapshot.storageId))
                continue;

            PendingById[snapshot.storageId] = snapshot;
        }

        foreach (var storage in Object.FindObjectsByType<HomeStorage>(FindObjectsSortMode.None))
        {
            if (storage != null)
                storage.TryApplyPendingSnapshot();
        }
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
