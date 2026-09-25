using UnityEngine;

public class BedSaveInteract : MonoBehaviour, IInteractionPromptOwner
{
    [Tooltip("Distância máxima para dormir/salvar")]
    public float interactDistance = 2.5f;

    [Tooltip("Spawn usado ao continuar depois de salvar na cama")]
    public string saveSpawnId = RecomecoSceneNames.EntradaCasaElegante;

    public KeyCode interactKey = KeyCode.E;

    float _feedbackTimer;
    bool _playerInRange;

    public bool IsInteractionPromptActive() => _playerInRange && isActiveAndEnabled;

    void OnDisable()
    {
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

        ShowPrompt();

        if (!Input.GetKeyDown(interactKey))
            return;

        var cycle = GameplayDayNightCycle.Instance;
        var isNight = cycle != null && cycle.IsNight();

        if (isNight)
            TrySleepAtNight(player, cycle);
        else
            TrySaveOnly();
    }

    void TrySaveOnly()
    {
        if (SaveGameManager.SaveCurrentGame(saveSpawnId))
            ShowFeedback("Jogo salvo.");
        else
            ShowFeedback("Não foi possível salvar.");
    }

    void TrySleepAtNight(GameObject player, GameplayDayNightCycle cycle)
    {
        if (!cycle.CanSleepInBed())
        {
            ShowFeedback("Você já descansou esta noite. Espere o próximo anoitecer.");
            return;
        }

        var needs = player != null ? player.GetComponent<PlayerNeeds>() : null;
        if (needs == null)
        {
            ShowFeedback("Não foi possível descansar.");
            return;
        }

        cycle.RunSafeBedSleepTransition(
            () => needs.ApplySafeSleep(RecomecoGameplaySettings.Instance),
            () =>
            {
                if (SaveGameManager.SaveCurrentGame(saveSpawnId))
                {
                    MissionProgress.NotifySafeBedRest();
                    ShowFeedback("Descanso na cama. Jogo salvo.");
                }
                else
                    ShowFeedback("Descansou, mas não foi possível salvar.");
            });
        InteractionUI.HideMessage(this);
        _feedbackTimer = 1.2f;
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
    }

    void ShowPrompt()
    {
        var cycle = GameplayDayNightCycle.Instance;
        var isNight = cycle != null && cycle.IsNight();
        var text = isNight
            ? "Aperte E para dormir na cama (salva o jogo)"
            : "Aperte E para salvar o jogo";
        InteractionUI.ShowMessage(text, this);
    }

    void ShowFeedback(string message)
    {
        _feedbackTimer = 2.2f;
        InteractionUI.ShowMessage(message, this);
    }
}
