using UnityEngine;

public static class MissionRewards
{
    public static void GrantForCompletedMission(MissionId completed)
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null)
            return;

        MissionInventoryRewards.GrantCompletionReward(completed);

        if (completed == MissionId.Resell && settings.skateUnlockOnResellMission)
            GrantSkateReward(settings);
    }

    static void GrantSkateReward(RecomecoGameplaySettings settings)
    {
        if (PlayerUnlocks.HasSkate)
            return;

        if (!PlayerSkateItems.TryGrantToPlayer(out var grantMsg))
        {
            if (!string.IsNullOrEmpty(grantMsg))
            {
                InteractionUI.ShowMessage(grantMsg);
                GameplayTipBannerUI.Show(grantMsg, 9f);
            }
            return;
        }

        PlayerUnlocks.NotifyChanged();

        var key = settings.skateToggleKey;
        var keyLabel = key == KeyCode.None ? "B" : key.ToString();

        var tip = "Skate no inventário! Na cidade, pressione " + keyLabel +
                  " para montar/desmontar. Abra o inventário (I ou Tab) para conferir.";

        if (!PlayerSkateController.IsSkateLocomotionReady())
            tip += " (Animações de skate ainda não configuradas.)";

        GameplayTipBannerUI.Show(tip, 10f);

        if (!string.IsNullOrEmpty(settings.skateUnlockBanner))
            InteractionUI.ShowMessage(settings.skateUnlockBanner, null, InteractionUI.PriorityGameplay + 5);
    }
}