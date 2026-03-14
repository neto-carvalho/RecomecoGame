using UnityEngine;

/// <summary>
/// Controla movimentação do jogador: andar, correr, pular e agachar.
/// Requer CharacterController no mesmo GameObject.
/// Controles: WASD = mover, Shift = correr, Space = pular, Ctrl = agachar.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidade")]
    [Tooltip("Velocidade ao andar")]
    public float walkSpeed = 5f;
    [Tooltip("Velocidade ao correr (Shift)")]
    public float runSpeed = 10f;
    [Tooltip("Velocidade ao agachar (Ctrl)")]
    public float crouchSpeed = 2.5f;

    [Header("Pulo")]
    [Tooltip("Força do pulo")]
    public float jumpForce = 8f;
    [Tooltip("Força da gravidade aplicada ao personagem")]
    public float gravity = -25f;

    [Header("Agachar")]
    [Tooltip("Altura do CharacterController quando agachado")]
    public float crouchHeight = 1f;
    [Tooltip("Tempo para agachar/levantar (suavização)")]
    public float crouchTime = 0.2f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // Valores padrão do CharacterController (altura em pé)
    private float standingHeight;
    private Vector3 standingCenter;
    private float currentHeight;
    private Vector3 currentCenter;
    private bool isCrouching;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            UnityEngine.Debug.LogError("PlayerMovement: CharacterController não encontrado no mesmo GameObject.");
            return;
        }

        standingHeight = controller.height;
        standingCenter = controller.center;
        currentHeight = standingHeight;
        currentCenter = standingCenter;
    }

    void Update()
    {
        // CharacterController só funciona com PhysX. Se estiver inativo, não chama Move (evita erro no console).
        if (controller == null || !controller.enabled)
            return;

        // Verifica se está no chão (permite pequena tolerância)
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // Pequena força para manter no chão

        // --- Entrada de movimento (WASD) ---
        float horizontal = Input.GetKey(KeyCode.A) ? -1f : (Input.GetKey(KeyCode.D) ? 1f : 0f);
        float vertical = Input.GetKey(KeyCode.W) ? 1f : (Input.GetKey(KeyCode.S) ? -1f : 0f);

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // --- Escolhe velocidade: agachar > correr > andar ---
        float targetSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            targetSpeed = crouchSpeed;
            isCrouching = true;
        }
        else
            isCrouching = false;

        if (inputDir.magnitude >= 0.1f && !isCrouching)
            targetSpeed = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? runSpeed : walkSpeed;

        // Movimento no mundo (direção relativa ao eixo do personagem)
        Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.z;
        controller.Move(move * targetSpeed * Time.deltaTime);

        // --- Pulo ---
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        // --- Gravidade ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- Agachar: altera altura e centro do CharacterController ---
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        Vector3 targetCenter = isCrouching ? new Vector3(standingCenter.x, crouchHeight * 0.5f, standingCenter.z) : standingCenter;

        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime / crouchTime);
        currentCenter = Vector3.Lerp(currentCenter, targetCenter, Time.deltaTime / crouchTime);

        controller.height = currentHeight;
        controller.center = currentCenter;
    }
}
