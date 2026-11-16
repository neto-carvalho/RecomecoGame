using System;

/// <summary>Progressão do skate — posse = item no inventário.</summary>
public static class PlayerUnlocks
{
    public const string SkateId = "skate";

    public static event Action Changed;

    public static bool HasSkate => PlayerSkateItems.PlayerOwnsSkate();

    public static void GrantSkate()
    {
        if (!PlayerSkateItems.TryGrantToPlayer(out var message))
        {
            if (!string.IsNullOrEmpty(message))
                InteractionTransientMessage.Show(message, 5f);
            return;
        }

        NotifyChanged();
    }

    public static void NotifyChanged()
    {
        Changed?.Invoke();
        RefreshPlayerSkate();
    }

    /// <summary>Save antigo sem item no inventário.</summary>
    public static void Import(bool legacyHadSkate)
    {
        if (legacyHadSkate && !HasSkate)
            PlayerSkateItems.TryGrantToPlayer(out _);

        Changed?.Invoke();
        RefreshPlayerSkate();
    }

    public static void Reset()
    {
        Changed?.Invoke();
    }

    static void RefreshPlayerSkate()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<PlayerSkateController>();
        if (controller != null)
            controller.RefreshFromUnlocks();
    }
}