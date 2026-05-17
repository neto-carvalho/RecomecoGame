#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Atalhos para importar personagens Mixamo e criar prefabs visuais para <see cref="SidewalkNpcWalker"/>.
/// O prefab tem de incluir a hierarquia completa do FBX (ossos + mesh), não só o SkinnedMeshRenderer.
/// </summary>
public static class MixamoNpcSetupMenu
{
    const string MixamoFolder = "Assets/Characters/Mixamo";
    const string SkinPrefabFolder = "Assets/Prefabs/NPC/Skins";
    const string MovementControllerPath =
        "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Controllers/Character_Movement.controller";

    [MenuItem("Recomeco/NPC/Mixamo/Configurar FBX selecionados (Humanoid)")]
    static void ConfigureSelectedMixamoFbx()
    {
        var changed = 0;
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!IsFbxPath(path))
                continue;

            if (EnsureHumanoidRig(path, reimport: true))
                changed++;
        }

        if (changed == 0)
        {
            EditorUtility.DisplayDialog(
                "Mixamo",
                "Seleciona um ou mais ficheiros .fbx do Mixamo no Project e volta a correr este menu.",
                "OK");
            return;
        }

        Debug.Log($"[Mixamo] {changed} FBX configurado(s) como Humanoid (com Avatar).");
    }

    [MenuItem("Recomeco/NPC/Mixamo/Criar prefab de skin a partir da seleção")]
    static void CreateSkinPrefabFromSelection()
    {
        var fbxPath = ResolveFbxAssetPathFromSelection();
        if (string.IsNullOrEmpty(fbxPath))
        {
            EditorUtility.DisplayDialog(
                "Mixamo",
                "Seleciona o ficheiro .fbx no Project (recomendado) ou uma instância na Hierarchy ligada a um FBX.",
                "OK");
            return;
        }

        if (!EnsureHumanoidRig(fbxPath, reimport: true))
        {
            ShowMissingAvatarHelp(fbxPath);
            return;
        }

        EnsureFolder(MixamoFolder);
        EnsureFolder(SkinPrefabFolder);

        var defaultName = $"MixamoSkin_{Path.GetFileNameWithoutExtension(fbxPath)}";
        var savePath = EditorUtility.SaveFilePanelInProject(
            "Guardar prefab de skin NPC",
            defaultName,
            "prefab",
            "Guarda a hierarquia COMPLETA do FBX (ossos + mesh).",
            SkinPrefabFolder);

        if (string.IsNullOrEmpty(savePath))
            return;

        if (!TrySaveSkinPrefabFromFbx(fbxPath, savePath, out var error))
        {
            EditorUtility.DisplayDialog("Mixamo", error, "OK");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[Mixamo] Prefab criado com hierarquia completa: {savePath}");
    }

    [MenuItem("Recomeco/NPC/Mixamo/Reparar prefab(s) de skin selecionado(s)")]
    static void RepairSelectedSkinPrefabs()
    {
        var repaired = 0;
        foreach (var obj in Selection.objects)
        {
            var prefabPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab"))
                continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                continue;

            var fbxPath = ResolveFbxPathFromBrokenSkinPrefab(prefab);
            if (string.IsNullOrEmpty(fbxPath))
            {
                Debug.LogWarning($"[Mixamo] Não foi possível encontrar o FBX de origem para {prefabPath}.");
                continue;
            }

            if (!EnsureHumanoidRig(fbxPath, reimport: false))
            {
                Debug.LogWarning($"[Mixamo] Avatar em falta em {fbxPath}.");
                continue;
            }

            if (!TrySaveSkinPrefabFromFbx(fbxPath, prefabPath, out var error))
            {
                Debug.LogWarning($"[Mixamo] Falha ao reparar {prefabPath}: {error}");
                continue;
            }

            repaired++;
            Debug.Log($"[Mixamo] Prefab reparado: {prefabPath} ← {fbxPath}");
        }

        if (repaired == 0)
        {
            EditorUtility.DisplayDialog(
                "Mixamo",
                "Seleciona no Project os prefabs MixamoSkin_… (ex.: MixamoSkin_Ch29) e volta a correr este menu.",
                "OK");
        }
        else
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Mixamo", $"{repaired} prefab(s) reparado(s) com hierarquia completa.", "OK");
        }
    }

    [MenuItem("Recomeco/NPC/Mixamo/Criar prefab de skin a partir da seleção", true)]
    static bool CreateSkinPrefabFromSelectionValidate() => Selection.objects.Length > 0;

    [MenuItem("Recomeco/NPC/Mixamo/Reparar prefab(s) de skin selecionado(s)", true)]
    static bool RepairSelectedSkinPrefabsValidate() => Selection.objects.Length > 0;

    /// <summary>Instancia o modelo completo do FBX, configura Animator e grava prefab.</summary>
    static bool TrySaveSkinPrefabFromFbx(string fbxPath, string savePath, out string error)
    {
        error = null;
        var avatar = LoadAvatarFromFbx(fbxPath);
        if (avatar == null)
        {
            error = "Avatar em falta no FBX.";
            return false;
        }

        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MovementControllerPath);
        if (controller == null)
        {
            error = $"Controller não encontrado: {MovementControllerPath}";
            return false;
        }

        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelPrefab == null)
        {
            error = $"Não foi possível carregar: {fbxPath}";
            return false;
        }

        var temp = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        if (temp == null)
            temp = Object.Instantiate(modelPrefab);

        try
        {
            temp.name = Path.GetFileNameWithoutExtension(fbxPath);
            StripGameplayComponents(temp);
            ConfigureAnimatorOnHierarchy(temp, controller, avatar);

            if (!HasBonesInHierarchy(temp))
            {
                error =
                    "O FBX não gerou hierarquia de ossos na cena. Confirma Rig → Humanoid → Apply no importador.";
                return false;
            }

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, savePath);
            return saved != null;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    static void ConfigureAnimatorOnHierarchy(GameObject root, RuntimeAnimatorController controller, Avatar avatar)
    {
        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = root.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.avatar = avatar;
        animator.applyRootMotion = false;
    }

    /// <summary>Prefab “partido” = só mesh no root, ossos ainda apontam para o FBX mas não existem na hierarquia.</summary>
    static bool HasBonesInHierarchy(GameObject root)
    {
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.bones == null || smr.bones.Length == 0)
                continue;

            var bone = smr.bones[0];
            if (bone != null && bone.transform.IsChildOf(root.transform))
                return true;
        }

        return root.transform.childCount > 0;
    }

    static string ResolveFbxPathFromBrokenSkinPrefab(GameObject prefabAsset)
    {
        foreach (var smr in prefabAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null)
                continue;
            var meshPath = AssetDatabase.GetAssetPath(smr.sharedMesh);
            if (IsFbxPath(meshPath))
                return meshPath;
        }

        return null;
    }

    static string ResolveFbxAssetPathFromSelection()
    {
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (IsFbxPath(path))
                return path;
        }

        if (Selection.activeGameObject != null)
        {
            var root = Selection.activeGameObject;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (IsFbxPath(path))
                return path;

            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var meshPath = AssetDatabase.GetAssetPath(smr.sharedMesh);
                if (IsFbxPath(meshPath))
                    return meshPath;
            }
        }

        foreach (var obj in Selection.objects)
        {
            var prefabPath = AssetDatabase.GetAssetPath(obj);
            if (!prefabPath.EndsWith(".prefab"))
                continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                continue;
            var fbx = ResolveFbxPathFromBrokenSkinPrefab(prefab);
            if (!string.IsNullOrEmpty(fbx))
                return fbx;
        }

        return null;
    }

    static void ShowMissingAvatarHelp(string fbxPath)
    {
        EditorUtility.DisplayDialog(
            "Mixamo — Avatar em falta",
            "Não foi possível gerar o Avatar Humanoid para:\n" + fbxPath + "\n\n" +
            "Rig → Humanoid → Configure → Done → Apply.",
            "OK");
    }

    static bool IsFbxPath(string path) =>
        !string.IsNullOrEmpty(path) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase);

    static bool EnsureHumanoidRig(string fbxPath, bool reimport)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
            return false;

        var needsReimport = importer.animationType != ModelImporterAnimationType.Human
                            || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        // Sem clips embutidos do Mixamo — locomotion vem do Character_Movement (retarget Humanoid).
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        if (needsReimport && reimport)
            importer.SaveAndReimport();

        return LoadAvatarFromFbx(fbxPath) != null;
    }

    static Avatar LoadAvatarFromFbx(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer?.sourceAvatar != null && importer.sourceAvatar.isValid)
            return importer.sourceAvatar;

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Avatar av && av.isValid)
                return av;
        }

        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void StripGameplayComponents(GameObject root)
    {
        foreach (var cc in root.GetComponentsInChildren<CharacterController>(true))
            Object.DestroyImmediate(cc);
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(rb);
    }
}
#endif
