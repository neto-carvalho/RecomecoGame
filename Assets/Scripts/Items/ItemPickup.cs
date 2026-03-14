using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;

    private Inventory playerInventory;
    private bool playerNear;

    void Start()
    {
        playerInventory = FindObjectOfType<Inventory>();
    }

    void Update()
    {
        if (playerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInventory == null)
            {
                UnityEngine.Debug.LogError("Inventory não encontrado!");
                return;
            }

            if (item == null)
            {
                UnityEngine.Debug.LogError("ItemData não configurado!");
                return;
            }

            bool added = playerInventory.AddItem(item);

            if (added)
            {
                UnityEngine.Debug.Log("Item coletado: " + item.itemName);
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            if (InteractionUI.instance != null)
                InteractionUI.instance.ShowText("Aperte E para coletar");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            if (InteractionUI.instance != null)
                InteractionUI.instance.HideText();
        }
    }
}