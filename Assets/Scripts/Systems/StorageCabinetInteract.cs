using System.Text;
using UnityEngine;

[RequireComponent(typeof(HomeStorage))]
public class StorageCabinetInteract : MonoBehaviour, IInteractionPromptOwner
{
    public float interactDistance = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    HomeStorage _storage;
    bool _open;
    float _feedbackTimer;
    bool _playerInRange;

    public bool IsInteractionPromptActive() => _playerInRange && isActiveAndEnabled;

    void Awake()
    {
        _storage = GetComponent<HomeStorage>();
    }

    void OnDisable()
    {
        _open = false;
        _feedbackTimer = 0f;
        InteractionUI.HideMessage(this);
    }

    void Update()
    {
        var player = InteractionProximity.GetPlayer();
        var playerTransform = player != null ? player.transform : null;
        var inRange = InteractionProximity.IsWithinRange(transform.position, interactDistance, playerTransform);
        _playerInRange = inRange;

        if (!inRange)
        {
            _open = false;
            if (_feedbackTimer > 0f)
                _feedbackTimer = 0f;
            InteractionUI.HideMessage(this);
            return;
        }

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f)
                ShowOpenPanel();
            return;
        }

        if (!_open)
            ShowPrompt();

        if (Input.GetKeyDown(interactKey))
        {
            _open = !_open;
            if (_open)
                ShowOpenPanel();
            else
                ShowPrompt();
            return;
        }

        if (!_open || player == null)
            return;

        HandleTransfers(player.GetComponent<Inventory>());
    }

    void HandleTransfers(Inventory inventory)
    {
        if (inventory == null)
            return;

        for (var i = 0; i < 9; i++)
        {
            var numberKey = KeyCode.Alpha1 + i;
            var keypad = KeyCode.Keypad1 + i;

            if (Input.GetKeyDown(numberKey) || Input.GetKeyDown(keypad))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (_storage.TryWithdrawOne(inventory, i))
                        ShowFeedback("Retirou 1 un. do armário (slot " + (i + 1) + ").");
                    else
                        ShowFeedback("Não deu para retirar do slot " + (i + 1) + ".");
                }
                else if (_storage.TryDepositOne(inventory, i))
                {
                    ShowFeedback("Guardou 1 un. do inventário (slot " + (i + 1) + ").");
                }
                else
                {
                    ShowFeedback("Não deu para guardar do slot " + (i + 1) + ".");
                }

                return;
            }
        }
    }

    void ShowPrompt()
    {
        InteractionUI.ShowMessage("Aperte E para abrir o armário", this);
    }

    void ShowOpenPanel()
    {
        var sb = new StringBuilder();
        sb.Append("<b>ARMÁRIO</b>\n");
        sb.Append(_storage.BuildSummary());
        sb.Append("\n\n1-9: guardar 1 un. do slot do inventário");
        sb.Append("\nShift+1-9: retirar 1 un. do armário");
        sb.Append("\nE: fechar");
        InteractionUI.ShowMessage(sb.ToString(), this);
    }

    void ShowFeedback(string message)
    {
        _feedbackTimer = 1.4f;
        InteractionUI.ShowMessage(message, this);
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
    }
}
