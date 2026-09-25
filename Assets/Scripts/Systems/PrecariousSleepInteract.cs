using UnityEngine;

/// <summary>
/// Descanso na moradia precária (barraca): só à noite, uma vez por noite; não salva o jogo.
/// </summary>
public class PrecariousSleepInteract : MonoBehaviour, IInteractionPromptOwner
{
    const string TipAfterSleep =
        "Dormir em um lugar precário como este pode causar dores ou problemas de saúde.";

    [Tooltip("Distância para interagir")]
    public float interactDistance = 2.8f;

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

        if (inRange)
            MissionProgress.NotifyShelterLocated();

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
        if (cycle != null && !cycle.IsNight())
        {
            ShowFeedback("Só dá para dormir entre " + GameplayDayNightCycle.GetBarracaSleepWindowText() + ".");
            return;
        }

        if (cycle != null && !cycle.CanSleepInBarraca())
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

        if (cycle != null)
        {
            cycle.RunBarracaSleepTransition(
                () => needs.ApplyPrecariousSleep(RecomecoGameplaySettings.Instance),
                () =>
                {
                    MissionProgress.NotifyPrecariousSleep();
                    var settings = RecomecoGameplaySettings.Instance;
                    var tipSeconds = settings != null ? settings.precariousSleepTipSeconds : 7.5f;
                    GameplayTipBannerUI.Show(TipAfterSleep, tipSeconds);
                });
        }
        else
        {
            needs.ApplyPrecariousSleep(RecomecoGameplaySettings.Instance);
            MissionProgress.NotifyPrecariousSleep();
            GameplayTipBannerUI.Show(TipAfterSleep, 7.5f);
        }

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
        if (cycle == null)
        {
            InteractionUI.ShowMessage("Aperte E para dormir", this);
            return;
        }

        if (!cycle.IsNight())
        {
            InteractionUI.ShowMessage(
                "Dormir — das " + GameplayDayNightCycle.GetBarracaSleepWindowText(),
                this);
            return;
        }

        if (!cycle.CanSleepInBarraca())
        {
            InteractionUI.ShowMessage("Barraca — já descansou esta noite", this);
            return;
        }

        InteractionUI.ShowMessage("Aperte E para dormir", this);
    }

    void ShowFeedback(string message)
    {
        _feedbackTimer = 2.8f;
        InteractionUI.ShowMessage(message, this);
    }
}
