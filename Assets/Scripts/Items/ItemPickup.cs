using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public float pickupDistance = 2f;
    private GameObject player;
    private bool playerPerto = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.transform.position, transform.position);

        if (distance < pickupDistance)
        {
            if (!playerPerto)
            {
                InteractionUI.instance.ShowText("Pressione E para coletar");
                playerPerto = true;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                GameManager.instance.AddLatinha();
                SpawnManager.instance.RespawnLatinha();

                InteractionUI.instance.HideText();

                Destroy(gameObject);
            }
        }
        else
        {
            if (playerPerto)
            {
                InteractionUI.instance.HideText();
                playerPerto = false;
            }
        }
    }
}