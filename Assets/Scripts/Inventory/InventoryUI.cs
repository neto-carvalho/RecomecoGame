using System.Collections;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;

    Inventory inventory;
    bool setupOk;

    void Start()
    {
        ResolveInventory();

        if (inventoryPanel == null)
        {
            UnityEngine.Debug.LogError("InventoryUI: arraste o painel do inventário no campo 'Inventory Panel'. Sem isso o inventário não abre.");
            setupOk = false;
            return;
        }

        inventoryPanel.SetActive(false);
        setupOk = true;
    }

    void ResolveInventory()
    {
        if (inventory != null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            inventory = player.GetComponent<Inventory>();

        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
    }

    void Update()
    {
        if (!setupOk)
            return;

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            var open = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(open);
            if (open)
                RefreshInventoryDisplay();
        }
    }

    void RefreshInventoryDisplay()
    {
        ResolveInventory();
        if (inventory == null)
            return;

        inventory.ReconnectUi();
        inventory.RefreshAllSlots();
        Canvas.ForceUpdateCanvases();
        StartCoroutine(RefreshInventoryNextFrame());
    }

    IEnumerator RefreshInventoryNextFrame()
    {
        yield return null;
        if (inventory == null || inventoryPanel == null || !inventoryPanel.activeSelf)
            yield break;

        inventory.RefreshAllSlots();
        Canvas.ForceUpdateCanvases();
    }
}
