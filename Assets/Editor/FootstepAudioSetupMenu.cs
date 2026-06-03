#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FootstepAudioSetupMenu
{
    const string MenuRoot = "Recomeco/Áudio/";
    const string FootstepsFolder = "Assets/Audio/Footsteps";
    const string EssentialsRoot = "Assets/Footsteps - Essentials";
    const string LibraryPath = "Assets/Audio/FootstepSurfaceLibrary.asset";

    const string DefaultWalkSurface = "Footsteps_Tile";
    const string AlternateGrassSurface = "Footsteps_Grass";

    static readonly (FootstepSurfaceType type, string essentialsFolder)[] EssentialsSurfaces =
    {
        (FootstepSurfaceType.Tile, "Footsteps_Tile"),
        (FootstepSurfaceType.Grass, "Footsteps_Grass"),
        (FootstepSurfaceType.Gravel, "Footsteps_Gravel"),
        (FootstepSurfaceType.Water, "Footsteps_Water"),
        (FootstepSurfaceType.Wood, "Footsteps_Wood"),
        (FootstepSurfaceType.Metal, "Footsteps_Metal"),
        (FootstepSurfaceType.Mud, "Footsteps_Mud"),
        (FootstepSurfaceType.Sand, "Footsteps_Sand"),
        (FootstepSurfaceType.Snow, "Footsteps_Snow"),
        (FootstepSurfaceType.Rock, "Footsteps_Rock"),
        (FootstepSurfaceType.Leaves, "Footsteps_Leaves"),
        (FootstepSurfaceType.DirtyGround, "Footsteps_DirtyGround"),
    };

    [MenuItem(MenuRoot + "Configurar passos por superfície (biblioteca + personagens)")]
    static void SetupSurfaceFootsteps()
    {
        var library = GetOrCreateSurfaceLibrary();
        if (library == null)
        {
            EditorUtility.DisplayDialog(
                "Passos",
                "Não foi possível criar a biblioteca.\nImporta «Footsteps - Essentials» em Assets/.",
                "OK");
            return;
        }

        var chars = 0;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && EnsureFootstepAudio(player, library))
            chars++;

        foreach (var walker in UnityEngine.Object.FindObjectsByType<SidewalkNpcWalker>(FindObjectsSortMode.None))
        {
            if (walker != null && EnsureFootstepAudio(walker.gameObject, library))
                chars++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Passos por superfície",
            "Biblioteca: " + LibraryPath + "\n\n" +
            "Detecta chão pelo nome (Asphalt → ladrilho, Grass → relva, Water → água).\n\n" +
            $"Personagens configurados: {chars}\n\nGuarda a cena (Ctrl+S) e dá Play.",
            "OK");
    }

    [MenuItem(MenuRoot + "Criar/atualizar biblioteca de superfícies (Essentials)")]
    static void RebuildSurfaceLibraryMenu()
    {
        var library = GetOrCreateSurfaceLibrary(forceRebuild: true);
        if (library == null)
            return;

        EditorUtility.DisplayDialog("Biblioteca", "Superfícies actualizadas em:\n" + LibraryPath, "OK");
    }

    [MenuItem(MenuRoot + "Corrigir terreno natureza/lago (passos relva/terra)")]
    static void TagNatureTerrainFootsteps()
    {
        var count = 0;
        foreach (var terrain in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (terrain == null)
                continue;

            var name = terrain.gameObject.name.ToLowerInvariant();
            if (!name.Contains("nature") && !name.Contains("mato") && !name.Contains("lago"))
                continue;

            var marker = terrain.GetComponent<FootstepSurfaceMarker>();
            if (marker == null)
                marker = Undo.AddComponent<FootstepSurfaceMarker>(terrain.gameObject);

            var so = new SerializedObject(marker);
            so.FindProperty("surface").enumValueIndex = (int)FootstepSurfaceType.Grass;
            so.ApplyModifiedProperties();
            count++;
        }

        EditorUtility.DisplayDialog(
            "Terreno",
            count > 0
                ? $"Marcador de relva em {count} Terrain(s).\nO som real usa as texturas pintadas (mato/estrada)."
                : "Nenhum Terrain «NatureTerrain» / «Mato» na cena.",
            "OK");
    }

    [MenuItem(MenuRoot + "Marcar chão seleccionado (superfície manual)")]
    static void AddMarkerToSelection()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Marcador", "Selecciona um ou mais objetos de chão na Hierarchy.", "OK");
            return;
        }

        var count = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go.GetComponent<Collider>() == null && go.GetComponent<MeshCollider>() == null)
                continue;

            var marker = go.GetComponent<FootstepSurfaceMarker>();
            if (marker == null)
                marker = Undo.AddComponent<FootstepSurfaceMarker>(go);

            var surface = FootstepSurfaceResolver.FromObjectName(go.name);
            if (surface == FootstepSurfaceType.Default)
                surface = FootstepSurfaceType.Tile;

            var so = new SerializedObject(marker);
            so.FindProperty("surface").enumValueIndex = (int)surface;
            so.ApplyModifiedProperties();
            count++;
        }

        EditorUtility.DisplayDialog("Marcador", $"Marcadores em {count} objeto(s). Ajusta «Surface» no Inspector se precisares.", "OK");
    }

    [MenuItem(MenuRoot + "Adicionar passos ao Player e NPCs")]
    static void AddFootstepsToCharacters()
    {
        var library = AssetDatabase.LoadAssetAtPath<FootstepSurfaceLibrary>(LibraryPath);
        var count = 0;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && EnsureFootstepAudio(player, library))
            count++;

        foreach (var walker in UnityEngine.Object.FindObjectsByType<SidewalkNpcWalker>(FindObjectsSortMode.None))
        {
            if (walker != null && EnsureFootstepAudio(walker.gameObject, library))
                count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Passos",
            $"{count} personagem(ns).\n\nPara sons por chão:\n" +
            "Recomeco → Áudio → Configurar passos por superfície",
            "OK");
    }

    [MenuItem(MenuRoot + "Atribuir Footsteps Essentials (calçada)")]
    static void AssignFootstepsEssentials()
    {
        var walk = LoadEssentialsClips(DefaultWalkSurface, run: false);
        var run = LoadEssentialsClips(DefaultWalkSurface, run: true);
        if (walk.Count == 0)
        {
            walk = LoadEssentialsClips("Footsteps_Gravel", run: false);
            run = LoadEssentialsClips("Footsteps_Gravel", run: true);
        }

        if (walk.Count == 0)
        {
            EditorUtility.DisplayDialog("Passos", "Pack não encontrado em " + EssentialsRoot, "OK");
            return;
        }

        if (run.Count == 0)
            run = walk;

        AssignFallbackClips(walk.ToArray(), run.ToArray(),
            $"Fallback calçada: {walk.Count} andar, {run.Count} correr.");
    }

    [MenuItem(MenuRoot + "Atribuir Footsteps Essentials (relva)")]
    static void AssignFootstepsEssentialsGrass()
    {
        var walk = LoadEssentialsClips(AlternateGrassSurface, run: false);
        var run = LoadEssentialsClips(AlternateGrassSurface, run: true);
        if (walk.Count == 0)
        {
            EditorUtility.DisplayDialog("Passos", "Sem clips de relva.", "OK");
            return;
        }

        if (run.Count == 0)
            run = walk;
        AssignFallbackClips(walk.ToArray(), run.ToArray(), "Fallback relva atribuído.");
    }

    [MenuItem(MenuRoot + "Atribuir sons Kenney (concreto/relva)")]
    static void AssignKenneyFootsteps()
    {
        var concrete = LoadKenneyFootstepClips("concrete");
        var grass = LoadKenneyFootstepClips("grass");
        var walk = concrete.Count > 0 ? concrete : grass;
        if (walk.Count == 0)
        {
            EditorUtility.DisplayDialog("Passos", "Nenhum clip Kenney. Usa Configurar passos por superfície.", "OK");
            return;
        }

        var run = concrete.Count > 0 ? concrete : walk;
        AssignFallbackClips(walk.ToArray(), run.ToArray(), "Clips Kenney (fallback).");
    }

    static FootstepSurfaceLibrary GetOrCreateSurfaceLibrary(bool forceRebuild = false)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            AssetDatabase.CreateFolder("Assets", "Audio");

        var library = AssetDatabase.LoadAssetAtPath<FootstepSurfaceLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<FootstepSurfaceLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        if (forceRebuild || !LibraryHasContent(library))
            PopulateLibrary(library);

        AssetDatabase.SaveAssets();
        return library;
    }

    static bool LibraryHasContent(FootstepSurfaceLibrary library)
    {
        var so = new SerializedObject(library);
        var prop = so.FindProperty("surfaces");
        return prop != null && prop.arraySize > 0;
    }

    static void PopulateLibrary(FootstepSurfaceLibrary library)
    {
        var entries = new List<FootstepSurfaceLibrary.SurfaceClips>();

        foreach (var (type, folder) in EssentialsSurfaces)
        {
            var walk = LoadEssentialsClips(folder, run: false);
            var run = LoadEssentialsClips(folder, run: true);
            if (walk.Count == 0)
                continue;
            if (run.Count == 0)
                run = walk;

            entries.Add(new FootstepSurfaceLibrary.SurfaceClips
            {
                surface = type,
                walk = walk.ToArray(),
                run = run.ToArray(),
            });
        }

        library.EditorSetSurfaces(entries.ToArray(), FootstepSurfaceType.Tile);
        EditorUtility.SetDirty(library);
    }

    static void AssignFallbackClips(AudioClip[] walk, AudioClip[] run, string message)
    {
        var assigned = 0;
        foreach (var foot in UnityEngine.Object.FindObjectsByType<FootstepAudio>(FindObjectsSortMode.None))
        {
            foot.SetClips(walk, run);
            EditorUtility.SetDirty(foot);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Passos", $"{message}\n\n{assigned} personagem(ns).", "OK");
    }

    static bool EnsureFootstepAudio(GameObject go, FootstepSurfaceLibrary library)
    {
        if (go.GetComponent<CharacterController>() == null)
            return false;

        var foot = go.GetComponent<FootstepAudio>();
        if (foot == null)
        {
            Undo.AddComponent<FootstepAudio>(go);
            foot = go.GetComponent<FootstepAudio>();
        }

        if (go.GetComponent<AudioSource>() == null)
            Undo.AddComponent<AudioSource>(go);

        if (library != null)
            foot.SetSurfaceLibrary(library);

        ApplySerializedFootstepFlags(foot, library, useSurface: library != null);
        TryAutoAssignFallbackIfEmpty(foot);
        EditorUtility.SetDirty(go);
        return true;
    }

    static void ApplySerializedFootstepFlags(FootstepAudio foot, FootstepSurfaceLibrary library, bool useSurface)
    {
        var so = new SerializedObject(foot);
        var useProp = so.FindProperty("useSurfaceDetection");
        if (useProp != null)
            useProp.boolValue = useSurface;
        var libProp = so.FindProperty("surfaceLibrary");
        if (libProp != null && library != null)
            libProp.objectReferenceValue = library;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void TryAutoAssignFallbackIfEmpty(FootstepAudio foot)
    {
        var so = new SerializedObject(foot);
        var walkProp = so.FindProperty("walkClips");
        if (walkProp == null || walkProp.arraySize > 0)
            return;

        var walk = LoadEssentialsClips(DefaultWalkSurface, run: false);
        var run = LoadEssentialsClips(DefaultWalkSurface, run: true);
        if (walk.Count == 0)
            return;
        if (run.Count == 0)
            run = walk;

        foot.SetClips(walk.ToArray(), run.ToArray());
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static List<AudioClip> LoadEssentialsClips(string surfaceFolderName, bool run)
    {
        var result = new List<AudioClip>();
        if (!AssetDatabase.IsValidFolder(EssentialsRoot))
            return result;

        var token = run ? "_Run_" : "_Walk_";
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { EssentialsRoot });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains(surfaceFolderName + "/"))
                continue;

            var name = Path.GetFileNameWithoutExtension(path);
            if (!name.Contains(token) || name.Contains("_Jump"))
                continue;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                result.Add(clip);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }

    static List<AudioClip> LoadKenneyFootstepClips(string surfaceKeyword)
    {
        var result = new List<AudioClip>();
        if (!AssetDatabase.IsValidFolder("Assets/Audio"))
            return result;

        var searchIn = new[] { "Assets/Audio", FootstepsFolder };
        var guids = AssetDatabase.FindAssets("t:AudioClip", searchIn);
        var key = surfaceKeyword.ToLowerInvariant();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            if (!name.Contains("footstep") || !name.Contains(key))
                continue;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                result.Add(clip);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }
}
#endif
