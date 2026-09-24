#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Controller;
using UnityEngine;

public static class GameplayDebugCheats
{
    const int AddMoneyCents = 50000;
    const int MaxMoneyCents = 999900;

    static float _messageTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        if (!IsEnabled())
            return;

        var go = new GameObject(nameof(GameplayDebugCheatsRunner));
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<GameplayDebugCheatsRunner>();
    }

    public static bool IsEnabled()
    {
        var settings = RecomecoGameplaySettings.Instance;
        return settings == null || settings.enableDebugCheats;
    }

    public static void HandleInput()
    {
        if (!IsEnabled())
            return;

        if (RecomecoSceneNames.IsMenuScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
            return;

        if (Input.GetKeyDown(KeyCode.F9))
            AddMoney(AddMoneyCents, "+R$ 500");

        if (Input.GetKeyDown(KeyCode.F10))
            SetMoney(MaxMoneyCents, "Dinheiro máximo de teste");

        if (Input.GetKeyDown(KeyCode.F11))
            GrantCasaElegante();

        if (Input.GetKeyDown(KeyCode.F5))
            AdjustNeeds(-35f, 0f, 0f, "Fome -35%");

        if (Input.GetKeyDown(KeyCode.F6))
            AdjustNeeds(0f, -35f, 0f, "Vida -35%");

        if (Input.GetKeyDown(KeyCode.F7))
            AdjustNeeds(0f, 0f, -12f, "Rep. -12%");

        if (Input.GetKeyDown(KeyCode.F8))
            SetNeedsForStarveTest();

        if (Input.GetKeyDown(KeyCode.F3))
            ResetNeeds();

        if (Input.GetKeyDown(KeyCode.F2))
            TriggerFaintTest();

        if (Input.GetKeyDown(KeyCode.F12))
            ShowHelp();
    }

    public static void TickMessage()
    {
        if (_messageTimer <= 0f)
            return;

        _messageTimer -= Time.unscaledDeltaTime;
        if (_messageTimer <= 0f)
            InteractionUI.HideMessage(typeof(GameplayDebugCheats));
    }

    static void AddMoney(int cents, string label)
    {
        if (MoneyManager.instance == null)
        {
            ShowMessage("MoneyManager não encontrado.");
            return;
        }

        MoneyManager.instance.AddMoney(cents);
        ShowMessage(label + "  →  " + MoneyManager.FormatBRL(MoneyManager.instance.GetMoney()));
    }

    static void SetMoney(int cents, string label)
    {
        if (MoneyManager.instance == null)
        {
            ShowMessage("MoneyManager não encontrado.");
            return;
        }

        MoneyManager.instance.SetMoney(cents);
        ShowMessage(label + "  →  " + MoneyManager.FormatBRL(cents));
    }

    static void GrantCasaElegante()
    {
        PlayerHousingState.Grant(PlayerHousingState.CasaElegante);
        ShowMessage("Casa elegante liberada para teste.");
    }

    static PlayerNeeds ResolveNeeds()
    {
        if (PlayerNeeds.Instance != null)
            return PlayerNeeds.Instance;

        var mover = Object.FindFirstObjectByType<CharacterMover>();
        return mover != null ? mover.GetComponent<PlayerNeeds>() : null;
    }

    static void AdjustNeeds(float hungerDelta, float healthDelta, float reputationDelta, string label)
    {
        var needs = ResolveNeeds();
        if (needs == null)
        {
            ShowMessage("PlayerNeeds não encontrado (entre em uma cena de gameplay).");
            return;
        }

        if (hungerDelta != 0f)
            needs.AddHunger(hungerDelta);
        if (healthDelta != 0f)
            needs.AddHealth(healthDelta);
        if (reputationDelta != 0f)
            needs.AddReputation(reputationDelta);

        ShowMessage(label + "  →  " + FormatNeedsLine(needs));
    }

    static void ResetNeeds()
    {
        var needs = ResolveNeeds();
        if (needs == null)
        {
            ShowMessage("PlayerNeeds não encontrado.");
            return;
        }

        needs.SetNeedsForDebug(PlayerNeeds.MaxHunger, PlayerNeeds.MaxHealth, PlayerNeeds.MaxReputation);
        ShowMessage("Necessidades no máximo.  " + FormatNeedsLine(needs));
    }

    static void SetNeedsForStarveTest()
    {
        var needs = ResolveNeeds();
        if (needs == null)
        {
            ShowMessage("PlayerNeeds não encontrado.");
            return;
        }

        needs.SetNeedsForDebug(0f, 18f, needs.Reputation);
        ShowMessage("Fome zerada + vida baixa (teste rápido).  " + FormatNeedsLine(needs));
    }

    static void TriggerFaintTest()
    {
        var needs = ResolveNeeds();
        var player = needs != null ? needs.gameObject : null;
        if (player == null)
        {
            var mover = Object.FindFirstObjectByType<CharacterMover>();
            player = mover != null ? mover.gameObject : null;
        }

        if (player == null || needs == null)
        {
            ShowMessage("Player não encontrado.");
            return;
        }

        needs.SetNeedsForDebug(0f, 0f, needs.Reputation);
        PlayerFaintHandler.TryFaint(player);
        ShowMessage("Desmaio forçado (hospital).");
    }

    static string FormatNeedsLine(PlayerNeeds needs)
    {
        return "Vida " + Mathf.RoundToInt(needs.Health) + "% | Fome " + Mathf.RoundToInt(needs.Hunger) +
               "% | Rep. " + Mathf.RoundToInt(needs.Reputation) + "%";
    }

    static void ShowHelp()
    {
        ShowMessage(
            "Debug: F2 desmaio | F3 max needs | F5 fome-35 | F6 vida-35 | F7 rep-12 | F8 fome 0\n" +
            "F9 +R$500 | F10 max $ | F11 casa | F12 ajuda",
            6f);
    }

    static void ShowMessage(string message, float seconds = 2f)
    {
        _messageTimer = seconds;
        InteractionUI.ShowMessage("[TEST] " + message, typeof(GameplayDebugCheats));
    }

    sealed class GameplayDebugCheatsRunner : MonoBehaviour
    {
        void Update()
        {
            HandleInput();
            TickMessage();
        }
    }
}
#endif
