#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public static class GameplayDebugCheats
{
    const int AddMoneyCents = 50000;
    const int MaxMoneyCents = 999900;

    static float _messageTimer;
    static string _message;

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

    static void ShowHelp()
    {
        ShowMessage(
            "Debug: F9 +R$500 | F10 max | F11 casa | F12 ajuda",
            4f);
    }

    static void ShowMessage(string message, float seconds = 2f)
    {
        _message = message;
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
