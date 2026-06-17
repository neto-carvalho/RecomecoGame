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
                UnityEngine.Debug.LogError("Inventory n�o encontrado!");
                return;
            }

            if (item == null)
            {
                UnityEngine.Debug.LogError("ItemData n�o configurado!");
                return;
            }

            bool added = playerInventory.AddItem(item);

            if (added)
            {
                InteractionUI.HideMessage(this);
                if (SpawnManager.instance != null)
                    SpawnManager.instance.RespawnLatinha();
                UnityEngine.Debug.Log("Item coletado: " + item.itemName);
                Destroy(gameObject);
            }
            else
            {
                InteractionUI.ShowMessage("Invent�rio cheio!", this);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerNear = true;
        InteractionUI.ShowMessage("Aperte E para coletar", this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerNear = false;
        InteractionUI.HideMessage(this);
    }

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        if (other.GetComponentInParent<CharacterController>() != null)
            return true;

        return other.transform.root.CompareTag("Player");
    }
}
