using UnityEngine;

public class SellItems : MonoBehaviour
{
    [Tooltip("Distância máxima para o jogador poder vender (E)")]
    public float sellDistance = 3f;

    [Tooltip("Nome do item vendido (ex: Latinha). Deve bater com ItemData.itemName.")]
    public string itemName = "Latinha";

    [Tooltip("Valor por unidade vendida, em CENTAVOS (200 = R$ 2,00)")]
    public int pricePerUnit = 200;

    [Tooltip("Mensagem exibida quando o jogador está perto (ex.: Aperte E para vender)")]
    public string messageNear = "Aperte E para vender";

    bool _playerWasInRange;
    float _feedbackTimer;

    void Update()
    {
        var player = FindPlayer();
        var inRange = player != null &&
                      Vector3.Distance(player.transform.position, transform.position) < sellDistance;

        if (!inRange && _playerWasInRange)
            InteractionUI.HideMessage(this);

        if (inRange && player != null)
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
            }
            else if (!_playerWasInRange || _feedbackTimer <= 0f)
            {
                ShowOffer(player);
            }

            if (Input.GetKeyDown(KeyCode.E))
                TrySell(player);
        }

        _playerWasInRange = inRange;
    }

    static GameObject FindPlayer()
    {
        var traveling = PlayerScenePersistence.TravelingPlayer;
        if (traveling != null)
            return traveling;

        return GameObject.FindGameObjectWithTag("Player");
    }

    void ShowOffer(GameObject player)
    {
        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();

        var count = inventory != null ? inventory.GetItemCount(itemName) : 0;
        if (count <= 0)
        {
            InteractionUI.ShowMessage(
                messageNear + " — você não tem " + itemName + " no inventário.",
                this);
            return;
        }

        InteractionUI.ShowMessage(
            messageNear + " " + count + "x " + itemName + " — " +
            MoneyManager.FormatBRL(pricePerUnit) + " cada",
            this);
    }

    void TrySell(GameObject player)
    {
        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            UnityEngine.Debug.LogWarning("SellItems: Inventário não encontrado.");
            return;
        }

        if (MoneyManager.instance == null)
        {
            UnityEngine.Debug.LogWarning("SellItems: MoneyManager não encontrado.");
            return;
        }

        var count = inventory.GetItemCount(itemName);
        if (count <= 0)
        {
            InteractionUI.ShowMessage("Sem " + itemName + " para vender.", this);
            _feedbackTimer = 1.5f;
            return;
        }

        var removed = inventory.RemoveItem(itemName, count);
        var total = removed * pricePerUnit;
        MoneyManager.instance.AddMoney(total);

        InteractionUI.ShowMessage(
            "Vendeu " + removed + "x " + itemName + " por " + MoneyManager.FormatBRL(total) + "!",
            this);
        _feedbackTimer = 1.5f;
    }
}
