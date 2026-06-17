using UnityEngine;

public static class PlayerAnimatorSetup
{
    const string MovementControllerPath =
        "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Controllers/Character_Movement.controller";

    public static bool Apply(GameObject player, RecomecoGameplaySettings settings)
    {
        if (player == null)
            return false;

        var movement = GetMovementController(settings);
        if (movement == null)
        {
            Debug.LogWarning("PlayerAnimatorSetup: Character_Movement não encontrado (Resources/RecomecoGameplaySettings).");
            return false;
        }

        var avatar = settings != null ? settings.playerAvatar : null;
        var primary = FindPrimaryAnimator(player);
        if (primary == null)
        {
            Debug.LogWarning("PlayerAnimatorSetup: nenhum Animator humanoide no Player.");
            return false;
        }

        EnsureVisualHierarchyActive(player);

        foreach (var a in player.GetComponentsInChildren<Animator>(true))
        {
            if (!IsLocomotionAnimator(a, primary))
                continue;

            if (!a.gameObject.activeInHierarchy)
                a.gameObject.SetActive(true);
            a.enabled = true;
            a.runtimeAnimatorController = movement;
            a.applyRootMotion = false;
            if (avatar != null && (a.avatar == null || !a.avatar.isHuman))
                a.avatar = avatar;
        }

        var rootAnim = player.GetComponent<Animator>();
        if (rootAnim != null && rootAnim != primary && primary.runtimeAnimatorController == movement)
            Object.Destroy(rootAnim);

        return true;
    }

    static bool IsLocomotionAnimator(Animator a, Animator primary)
    {
        if (a == null)
            return false;
        if (a == primary)
            return true;
        if (a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            return true;
        return a.avatar != null && a.avatar.isHuman;
    }

    static Animator FindPrimaryAnimator(GameObject player)
    {
        Animator best = null;
        var bestScore = -1;

        foreach (var a in player.GetComponentsInChildren<Animator>(true))
        {
            var score = ScoreAnimator(a);
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        if (best != null)
            return best;

        foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var a = smr.GetComponent<Animator>();
            if (a == null)
                a = smr.GetComponentInParent<Animator>();
            if (a != null)
                return a;
        }

        return player.GetComponent<Animator>();
    }

    static int ScoreAnimator(Animator a)
    {
        if (a == null)
            return -1;

        var score = 0;
        if (a.isActiveAndEnabled)
            score += 5;

        if (a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            score += 30;

        if (a.avatar != null && a.avatar.isHuman)
            score += 25;

        var ctrl = a.runtimeAnimatorController;
        if (ctrl != null && ctrl.name.IndexOf("Character_Movement", System.StringComparison.OrdinalIgnoreCase) >= 0)
            score += 50;

        return score;
    }

    static void EnsureVisualHierarchyActive(GameObject player)
    {
        if (!player.activeSelf)
            player.SetActive(true);

        foreach (var smr in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr != null && !smr.gameObject.activeSelf)
                smr.gameObject.SetActive(true);
        }
    }

    static RuntimeAnimatorController GetMovementController(RecomecoGameplaySettings settings)
    {
        if (settings != null && settings.movementController != null)
            return settings.movementController;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MovementControllerPath);
#else
        return null;
#endif
    }

    public static void RefreshLocomotion(GameObject player)
    {
        if (player == null)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        Apply(player, settings);

        var mover = player.GetComponent<Controller.CharacterMover>();
        if (mover != null)
        {
            if (settings != null)
                settings.ApplyToMover(mover, player.transform);
            mover.RebindAnimator();
        }

    }
}
