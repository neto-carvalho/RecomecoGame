using UnityEngine;

public class SellItems : MonoBehaviour
{
    public float sellDistance = 3f;

    void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        float distance = Vector3.Distance(player.transform.position, transform.position);

        if (distance < sellDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                GameManager.instance.VenderLatinhas();
            }
        }
    }
}