using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;

    private Inventory inventory;
    private bool setupOk;

    void Start()
    {
        inventory = FindFirstObjectByType<Inventory>();

        if (inventoryPanel == null)
        {
            UnityEngine.Debug.LogError("InventoryUI: arraste o painel do inventário no campo 'Inventory Panel'. Sem isso o inventário não abre.");
            setupOk = false;
            return;
        }

        inventoryPanel.SetActive(false);
        setupOk = true;
    }

    void Update()
    {
        if (!setupOk)
            return;

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            bool open = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(open);
            if (open && inventory != null)
                inventory.RefreshAllSlots();
        }
    }
}
