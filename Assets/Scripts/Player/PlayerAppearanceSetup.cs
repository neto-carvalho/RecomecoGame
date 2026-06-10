using UnityEngine;

/// <summary>
/// Aplica a skin padrão do jogador (mesma da cena Cidade) quando a cena usa Base_Mesh sem customização.
/// </summary>
public static class PlayerAppearanceSetup
{
    public enum AppearanceSlot
    {
        Body,
        Face,
        Hair,
        Outwear,
        Pants,
    }

    public static void Apply(GameObject player)
    {
        if (player == null || !NeedsAppearance(player))
            return;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null)
            return;

        foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr == null || smr.sharedMesh == null)
                continue;

            var slot = ClassifySlot(smr);
            if (slot == null)
                continue;

            var mesh = settings.GetAppearanceMesh(slot.Value);
            if (mesh != null)
                smr.sharedMesh = mesh;
        }

        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            if (settings.playerAvatar != null)
                animator.avatar = settings.playerAvatar;
            if (settings.movementController != null)
            {
                animator.runtimeAnimatorController = settings.movementController;
                animator.applyRootMotion = false;
            }
        }
    }

    static bool NeedsAppearance(GameObject player)
    {
        foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null)
                continue;

            var name = smr.sharedMesh.name;
            if (name.IndexOf("Outwear_050", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
        }

        return true;
    }

    static AppearanceSlot? ClassifySlot(SkinnedMeshRenderer smr)
    {
        var bounds = smr.sharedMesh.bounds;
        var centerY = bounds.center.y;
        var extentX = bounds.extents.x;
        var extentY = bounds.extents.y;

        if (extentY >= 0.85f)
            return AppearanceSlot.Body;

        if (centerY >= 1.55f)
            return extentX < 0.22f ? AppearanceSlot.Face : AppearanceSlot.Hair;

        if (extentX >= 0.5f && centerY >= 0.9f && centerY <= 1.35f)
            return AppearanceSlot.Outwear;

        if (centerY <= 0.8f)
            return AppearanceSlot.Pants;

        return null;
    }
}
