using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Import Mixamo skate FBX, bake root, cria Character_Skate e liga no settings.</summary>
public static class PlayerSkateBuildMenu
{
    const string SkateFolder = "Assets/Animations/Skate";
    const string PushFbx = SkateFolder + "/Skate_Push.fbx";
    const string IdleFbx = SkateFolder + "/Skate_Idle.fbx";
    const string JumpSourceFbx =
        "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Mesh/Aminset_Basic.fbx";
    const string ControllerPath = SkateFolder + "/Character_Skate.controller";

    [MenuItem("Recomeco/Player/Skate — reimportar FBX (corrigir root motion)")]
    public static void ReimportSkateFbxRootMotion()
    {
        ConfigureMixamoFbx(PushFbx, loop: false);
        ConfigureMixamoFbx(IdleFbx, loop: true);
        EditorUtility.DisplayDialog("Recomeco", "FBX de skate reimportados com bake de root.", "OK");
    }

    [MenuItem("Recomeco/Player/Skate — configurar FBX e criar Animator")]
    public static void BuildSkateAnimator()
    {
        if (!AssetDatabase.LoadAssetAtPath<Object>(PushFbx) || !AssetDatabase.LoadAssetAtPath<Object>(IdleFbx))
        {
            EditorUtility.DisplayDialog(
                "Recomeco",
                "Faltam Skate_Push.fbx e Skate_Idle.fbx em\n" + SkateFolder,
                "OK");
            return;
        }

        if (!ConfigureMixamoFbx(PushFbx, loop: false) || !ConfigureMixamoFbx(IdleFbx, loop: true))
        {
            EditorUtility.DisplayDialog(
                "Recomeco",
                "Falha ao importar FBX (Rig). Abra Skate_Push no Inspector:\n" +
                "Rig = Humanoid → Create From This Model → Apply.\n" +
                "Depois rode este menu de novo.",
                "OK");
            return;
        }

        AssetDatabase.Refresh();

        var pushClip = LoadPrimaryClip(PushFbx);
        var idleClip = LoadPrimaryClip(IdleFbx);
        var jumpClip = LoadNamedClip(JumpSourceFbx, "Jump_Start");

        if (pushClip == null || idleClip == null)
        {
            EditorUtility.DisplayDialog(
                "Recomeco",
                "Não foi possível ler AnimationClips dos FBX de skate. Veja o Console.",
                "OK");
            return;
        }

        var controller = BuildController(pushClip, idleClip, jumpClip);
        if (controller == null)
            return;

        var settings = Resources.Load<RecomecoGameplaySettings>("RecomecoGameplaySettings");
        if (settings != null)
        {
            settings.skateMovementController = controller;
            settings.requireSkateAnimatorForRiding = true;
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Recomeco — Skate",
            "Animator criado:\n" + ControllerPath +
            "\n\n• Ride = idle no skate (Speed acelera o loop)\n" +
            "• Push = remada (trigger)\n" +
            "• Jump = Jump_Start do ithappy (provisório — sem ollie no Mixamo)\n\n" +
            "Play → PageUp → B na Cidade.",
            "OK");
    }

    /// <summary>
    /// Mixamo "Without Skin": usar Create From This Model — Copy Avatar do ithappy quebra (ossos mixamorig).
    /// Os clips ainda retargetam no player humanoid em runtime.
    /// </summary>
    static bool ConfigureMixamoFbx(string path, bool loop)
    {
        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return false;

        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importCameras = false;
        importer.importLights = false;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.sourceAvatar = null;
        importer.importAnimation = true;

        var clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
            clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0)
        {
            Debug.LogError("PlayerSkateBuildMenu: sem clips em " + path);
            return false;
        }

        for (var i = 0; i < clips.Length; i++)
        {
            clips[i].name = loop ? "Skate_Idle" : "Skate_Push";
            clips[i].loopTime = loop;
            clips[i].lockRootRotation = true;
            clips[i].keepOriginalOrientation = true;
            clips[i].lockRootHeightY = true;
            clips[i].lockRootPositionXZ = true;
            clips[i].heightFromFeet = false;
            clips[i].keepOriginalPositionY = false;
            clips[i].keepOriginalPositionXZ = false;
        }

        importer.clipAnimations = clips;

        importer.SaveAndReimport();

        if (!HasHumanoidAvatar(path))
        {
            Debug.LogError(
                "PlayerSkateBuildMenu: avatar humanoid não criado em " + path +
                ". No Inspector: Rig → Humanoid → Configure → Apply.");
            return false;
        }

        return true;
    }

    static bool HasHumanoidAvatar(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Avatar avatar && avatar.isHuman)
                return true;
        }

        return false;
    }

    static AnimationClip LoadPrimaryClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                return clip;
        }

        return null;
    }

    static AnimationClip LoadNamedClip(string fbxPath, string clipName)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && clip.name == clipName)
                return clip;
        }

        return null;
    }

    static AnimatorController BuildController(AnimationClip push, AnimationClip idle, AnimationClip jump)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AddParameter(controller, "Hor", AnimatorControllerParameterType.Float);
        AddParameter(controller, "Vert", AnimatorControllerParameterType.Float);
        AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "IsJump", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "Push", AnimatorControllerParameterType.Trigger);

        var layer = controller.layers[0];
        var sm = layer.stateMachine;

        var ride = sm.AddState("Ride", new Vector3(300, 0, 0));
        ride.motion = idle;
        ride.speed = 1f;
        ride.speedParameterActive = true;
        ride.speedParameter = "Speed";

        var pushState = sm.AddState("Push", new Vector3(300, 120, 0));
        pushState.motion = push;

        var pushAny = sm.AddAnyStateTransition(pushState);
        pushAny.AddCondition(AnimatorConditionMode.If, 0, "Push");
        pushAny.duration = 0.08f;
        pushAny.hasExitTime = false;
        pushAny.canTransitionToSelf = false;

        var pushBack = pushState.AddTransition(ride);
        pushBack.hasExitTime = true;
        pushBack.exitTime = 0.9f;
        pushBack.duration = 0.12f;

        sm.defaultState = ride;

        if (jump != null)
        {
            var jumpState = sm.AddState("Jump", new Vector3(540, 0, 0));
            jumpState.motion = jump;

            var toJump = ride.AddTransition(jumpState);
            toJump.AddCondition(AnimatorConditionMode.If, 0, "IsJump");
            toJump.duration = 0.06f;
            toJump.hasExitTime = false;

            var fromJump = jumpState.AddTransition(ride);
            fromJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJump");
            fromJump.duration = 0.12f;
            fromJump.hasExitTime = true;
            fromJump.exitTime = 0.85f;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void AddParameter(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        if (c.parameters.Any(p => p.name == name))
            return;

        c.AddParameter(name, type);
    }
}