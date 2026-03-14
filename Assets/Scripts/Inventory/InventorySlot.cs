[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty()
    {
        return item == null;
    }

    public bool CanStack(ItemData newItem)
    {
        return item == newItem;
    }
}