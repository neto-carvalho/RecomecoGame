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
                InteractionTransientMessage.Show(grantMsg, 5f);
                GameplayTipBannerUI.Show(grantMsg, settings.skateRewardTipSeconds);
            }
            return;
        }

        PlayerUnlocks.NotifyChanged();

        var key = settings.skateToggleKey;
        var keyLabel = key == KeyCode.None ? "B" : key.ToString();

        var banner = string.IsNullOrWhiteSpace(settings.skateUnlockBanner)
            ? "Parabéns! Você ganhou um skate. Na cidade, aperte " + keyLabel + " para andar."
            : settings.skateUnlockBanner;

        var tipSeconds = settings.skateRewardTipSeconds > 0f ? settings.skateRewardTipSeconds : 6.5f;
        var cornerTip = banner + " Confira no inventário (I ou Tab).";

        GameplayTipBannerUI.Show(cornerTip, tipSeconds);
    }
}