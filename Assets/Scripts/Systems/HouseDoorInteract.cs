using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class HouseDoorInteract : MonoBehaviour
{
    [Tooltip("ID da moradia (ex.: casa_elegante)")]
    public string housingId = PlayerHousingState.CasaElegante;

    [Tooltip("Preço de compra em centavos (15000 = R$ 150,00)")]
    public int purchasePriceCents = 15000;

    [Tooltip("Nome exibido na UI")]
    public string houseDisplayName = "Casa elegante";

    [Tooltip("Cena do interior")]
    public string interiorSceneName = RecomecoSceneNames.InteriorCasaElegante;

    [Tooltip("Spawn ao entrar no interior")]
    public string interiorSpawnId = RecomecoSceneNames.EntradaCasaElegante;

    public KeyCode interactKey = KeyCode.E;

    Collider _trigger;
    bool _playerInside;
    float _feedbackTimer;

    void Awake()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger != null)
            _trigger.isTrigger = true;
    }

    void OnDisable()
    {
        SetInside(false);
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayerCollider(other))
            SetInside(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayerCollider(other))
            SetInside(false);
    }

    void LateUpdate()
    {
        var player = FindPlayerRoot();
        var inside = player != null && InteractionProximity.IsInsideTrigger(_trigger, player.transform);

        if (inside != _playerInside)
            SetInside(inside);

        if (!_playerInside)
        {
            if (_feedbackTimer > 0f)
                _feedbackTimer = 0f;
            InteractionUI.HideMessage(this);
            return;
        }

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f)
                ShowPrompt();
            return;
        }

        if (!Input.GetKeyDown(interactKey))
            return;

        if (PlayerHousingState.Owns(housingId))
            EnterHouse();
        else
            TryPurchase();
    }

    void TryPurchase()
    {
        if (PlayerHousingState.TryPurchase(housingId, purchasePriceCents))
        {
            ShowFeedback("Comprou " + houseDisplayName + "! Aperte E para entrar.");
            return;
        }

        if (MoneyManager.instance != null &&
            MoneyManager.instance.GetMoney() < purchasePriceCents)
        {
            ShowFeedback("Precisa de " + MoneyManager.FormatBRL(purchasePriceCents) + " para comprar.");
            return;
        }

        ShowFeedback("Não foi possível comprar a casa.");
    }

    void EnterHouse()
    {
        if (string.IsNullOrEmpty(interiorSceneName))
        {
            Debug.LogWarning("HouseDoorInteract: interiorSceneName vazio.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(interiorSceneName))
        {
            Debug.LogError(
                "HouseDoorInteract: cena '" + interiorSceneName +
                "' não está no Build Settings.");
            return;
        }

        GameSession.SaveBeforeSceneLoad();
        PlayerScenePersistence.PrepareForSceneLoad();
        SceneTransitionState.SetNextSpawn(interiorSpawnId);
        InteractionUI.HideMessage(this);
        SceneManager.LoadScene(interiorSceneName);
    }

    void ShowPrompt()
    {
        if (PlayerHousingState.Owns(housingId))
        {
            InteractionUI.ShowMessage("Aperte E para entrar em " + houseDisplayName, this,
                InteractionUI.PriorityNavigation);
            return;
        }

        InteractionUI.ShowMessage(
            "Aperte E para comprar " + houseDisplayName + " (" +
            MoneyManager.FormatBRL(purchasePriceCents) + ")",
            this,
            InteractionUI.PriorityNavigation);
    }

    void ShowFeedback(string message)
    {
        _feedbackTimer = 2f;
        InteractionUI.ShowMessage(message, this, InteractionUI.PriorityNavigation);
    }

    void SetInside(bool inside)
    {
        if (_playerInside == inside)
            return;

        _playerInside = inside;
        if (inside)
            ShowPrompt();
        else
            InteractionUI.HideMessage(this);
    }

    static GameObject FindPlayerRoot() => InteractionProximity.GetPlayer();

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        var root = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform.root
            : other.transform.root;

        if (root.CompareTag("Player"))
            return true;

        var traveling = PlayerScenePersistence.TravelingPlayer;
        return traveling != null && root.gameObject == traveling;
    }
}
