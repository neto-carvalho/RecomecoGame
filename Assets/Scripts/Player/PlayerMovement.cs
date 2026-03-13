using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A))
            horizontal = -1;

        if (Input.GetKey(KeyCode.D))
            horizontal = 1;

        if (Input.GetKey(KeyCode.W))
            vertical = 1;

        if (Input.GetKey(KeyCode.S))
            vertical = -1;

        Vector3 move = new Vector3(horizontal, 0, vertical);

        transform.Translate(move * speed * Time.deltaTime);
    }
}