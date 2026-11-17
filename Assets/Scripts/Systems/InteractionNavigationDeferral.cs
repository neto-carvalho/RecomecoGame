using UnityEngine;

/// <summary>
/// Permite que portais e táxi consumam o E antes de ações de gameplay (venda, etc.).
/// </summary>
public static class InteractionNavigationDeferral
{
    public static bool IsPlayerInAnyTransitionZone()
    {
        var player = InteractionProximity.GetPlayer();
        if (player == null)
            return false;

        var playerTransform = player.transform;

        foreach (var zone in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
        {
            if (zone == null || !zone.isActiveAndEnabled)
                continue;

            if (zone.IsPlayerInside(playerTransform))
                return true;
        }

        return false;
    }

    public static bool HasActiveNavigationPrompt()
    {
        foreach (var zone in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
        {
            if (zone != null && zone.isActiveAndEnabled && zone.IsInteractionPromptActive())
                return true;
        }

        return false;
    }

    /// <summary>
    /// Bloqueia venda/loja só quando o jogador está no portal e ainda longe do ponto de gameplay.
    /// Na porta do ferro velho, a venda volta a aparecer.
    /// </summary>
    public static bool ShouldDeferGameplayOverNavigation(
        Vector3 gameplayPosition,
        float gameplayRange,
        Transform player)
    {
        if (player == null)
            return false;

        if (!IsPlayerInAnyTransitionZone())
            return false;

        return !InteractionProximity.IsWithinHorizontalRange(
            gameplayPosition,
            gameplayRange,
            player,
            scaleWithPlayer: false);
    }
}