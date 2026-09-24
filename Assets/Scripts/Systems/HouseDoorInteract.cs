using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class HouseDoorInteract : MonoBehaviour, IInteractionPromptOwner
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

    [Tooltip("Distância horizontal (XZ) para mostrar o prompt na cidade")]
    public float promptDistance = 2.75f;

    public KeyCode interactKey = KeyCode.E;

    Collider _trigger;
    bool _playerInRange;
    float _feedbackTimer;

    public bool IsInteractionPromptActive() => _playerInRange && isActiveAndEnabled;

    void Awake()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger != null)
            _trigger.isTrigger = true;
    }

    void OnDisable()
    {
        SetInRange(false);
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
    }

    void LateUpdate()
    {
        var player = InteractionProximity.GetPlayer();
        var inRange = player != null &&
                      InteractionProximity.IsWithinHorizontalRange(
                          transform.position, promptDistance, player.transform, scaleWithPlayer: false);

        if (inRange != _playerInRange)
            SetInRange(inRange);

        if (!_playerInRange)
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

    void SetInRange(bool inRange)
    {
        if (_playerInRange == inRange)
            return;

        _playerInRange = inRange;
        if (inRange)
            ShowPrompt();
        else
            InteractionUI.HideMessage(this);
    }
}
