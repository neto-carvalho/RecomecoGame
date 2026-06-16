using System;
using UnityEngine;

/// <summary>
/// Aplica a skin padrão do jogador (mesma da cena Cidade) quando a cena usa Base_Mesh sem customização.
/// Respeita peças já configuradas manualmente na Hierarchy (cabelo, roupa, etc.).
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
        if (player == null)
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

            if (ShouldKeepSceneMesh(smr, slot.Value))
                continue;

            if (!NeedsDefaultAppearance(player))
                continue;

            var mesh = settings.GetAppearanceMesh(slot.Value);
            if (mesh != null)
                smr.sharedMesh = mesh;
        }

        if (!NeedsDefaultAppearance(player))
            return;

        var animator = player.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return;

        if (settings.playerAvatar != null)
            animator.avatar = settings.playerAvatar;
        if (settings.movementController != null)
        {
            animator.runtimeAnimatorController = settings.movementController;
            animator.applyRootMotion = false;
        }
    }

    /// <summary>Personagem já customizado na cena (não é o default Outwear_050).</summary>
    static bool NeedsDefaultAppearance(GameObject player)
    {
        foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null)
                continue;

            var meshName = smr.sharedMesh.name;
            if (meshName.IndexOf("Outwear_050", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    static bool ShouldKeepSceneMesh(SkinnedMeshRenderer smr, AppearanceSlot slot)
    {
        var meshName = smr.sharedMesh.name;
        var goName = smr.gameObject.name;

        switch (slot)
        {
            case AppearanceSlot.Hair:
                return goName.IndexOf("Hairstyle", StringComparison.OrdinalIgnoreCase) >= 0 &&
                       meshName.IndexOf("Hairstyle", StringComparison.OrdinalIgnoreCase) >= 0;

            case AppearanceSlot.Face:
                return (goName.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("Faces", StringComparison.OrdinalIgnoreCase) >= 0) &&
                       (meshName.IndexOf("emotion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        meshName.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0);

            case AppearanceSlot.Outwear:
                return (goName.IndexOf("Outerwear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("Outwear", StringComparison.OrdinalIgnoreCase) >= 0) &&
                       meshName.IndexOf("Outwear", StringComparison.OrdinalIgnoreCase) >= 0;

            case AppearanceSlot.Pants:
                return goName.IndexOf("Pants", StringComparison.OrdinalIgnoreCase) >= 0 &&
                       meshName.IndexOf("Pants", StringComparison.OrdinalIgnoreCase) >= 0;

            case AppearanceSlot.Body:
                return goName.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0 &&
                       meshName.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0;

            default:
                return false;
        }
    }

    static AppearanceSlot? ClassifySlot(SkinnedMeshRenderer smr)
    {
        var goName = smr.gameObject.name;

        if (goName.IndexOf("Hairstyle", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppearanceSlot.Hair;

        if (goName.IndexOf("Face", StringComparison.OrdinalIgnoreCase) >= 0 ||
            goName.IndexOf("Faces", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppearanceSlot.Face;

        if (goName.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppearanceSlot.Body;

        if (goName.IndexOf("Outerwear", StringComparison.OrdinalIgnoreCase) >= 0 ||
            goName.IndexOf("Outwear", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppearanceSlot.Outwear;

        if (goName.IndexOf("Pants", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppearanceSlot.Pants;

        return ClassifySlotByBounds(smr);
    }

    static AppearanceSlot? ClassifySlotByBounds(SkinnedMeshRenderer smr)
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
