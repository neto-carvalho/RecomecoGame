using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;

    private Inventory inventory;

    void Start()
    {
        inventoryPanel.SetActive(false);
        inventory = FindFirstObjectByType<Inventory>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool open = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(open);
            if (open && inventory != null)
                inventory.RefreshAllSlots();
        }
    }
}