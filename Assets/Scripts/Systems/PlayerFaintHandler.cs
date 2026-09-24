using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerFaintHandler
{
    static bool _faintInProgress;
    static bool _pendingHospitalWake;
    static int _lastBillCents;

    public static bool IsFaintInProgress => _faintInProgress || PlayerFaintSequence.IsPlaying;

    public static bool ConsumePendingHospitalWake()
    {
        if (!_pendingHospitalWake)
            return false;

        _pendingHospitalWake = false;
        return true;
    }

    public static int LastBillCents => _lastBillCents;

    public static void TryFaint(GameObject player)
    {
        if (player == null || _faintInProgress || PlayerFaintSequence.IsPlaying)
            return;

        var needs = player.GetComponent<PlayerNeeds>();
        if (needs != null && needs.IsFaintLocked)
            return;

        ExecuteFaint(player);
    }

    static void ExecuteFaint(GameObject player)
    {
        _faintInProgress = true;

        SellMinigameUI.ForceCloseIfOpen();
        ShopUI.ForceCloseIfOpen();
        if (PlayerDrivableVehicle.Active != null)
            PlayerDrivableVehicle.Active.ExitVehicle();
        GameplayPauseMenu.ForceCloseIfOpen();
        InteractionUI.HideMessage();

        var settings = RecomecoGameplaySettings.Instance;
        _lastBillCents = settings != null ? settings.hospitalBillCents : 8500;

        if (MoneyManager.instance != null)
            MoneyManager.instance.ChargeAllowDebt(_lastBillCents);

        var needs = player.GetComponent<PlayerNeeds>();
        if (needs != null)
            needs.SetFaintLocked(true);

        PlayerFaintSequence.Play(player, () => AfterFaintPresentation(player));
    }

    static void AfterFaintPresentation(GameObject player)
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != RecomecoSceneNames.Cidade)
        {
            _pendingHospitalWake = true;
            GameSession.SaveBeforeSceneLoad();
            PlayerScenePersistence.PrepareForSceneLoad();
            SceneTransitionState.SetNextSpawn(RecomecoSceneNames.HospitalEntrada);
            SceneManager.LoadScene(RecomecoSceneNames.Cidade);
            _faintInProgress = false;
            return;
        }

        FinishHospitalWake(player);
    }

    public static void FinishHospitalWake(GameObject player)
    {
        if (player == null)
            return;

        HospitalSpawnUtility.PlacePlayerAtHospital(player);

        var camera = Object.FindFirstObjectByType<Controller.PlayerCamera>();
        if (camera != null)
            camera.ResetFaintDistanceMultiplier();

        PlayerAnimatorSetup.RefreshLocomotion(player);

        var needs = player.GetComponent<PlayerNeeds>();
        if (needs != null)
            needs.ApplyHospitalRecovery(RecomecoGameplaySettings.Instance);

        ShowHospitalBillMessage();
        _faintInProgress = false;
    }

    public static void ShowHospitalBillMessage()
    {
        var billText = MoneyManager.FormatBRL(_lastBillCents);
        InteractionUI.ShowMessage(
            "Você desmaiou de fome. Acordou na frente do hospital.\nConta: " + billText,
            typeof(PlayerFaintHandler),
            InteractionUI.PriorityGameplay);

        FaintMessageAutoHide.Schedule(4.5f);
    }
}

sealed class FaintMessageAutoHide : MonoBehaviour
{
    float _timer;

    public static void Schedule(float seconds)
    {
        var go = new GameObject("_FaintMessageAutoHide");
        var comp = go.AddComponent<FaintMessageAutoHide>();
        comp._timer = seconds;
    }

    void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f)
            return;

        InteractionUI.HideMessage(typeof(PlayerFaintHandler));
        Destroy(gameObject);
    }
}
