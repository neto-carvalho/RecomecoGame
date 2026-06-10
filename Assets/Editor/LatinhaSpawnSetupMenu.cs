#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adiciona SpawnManager na cena sem alterar o terreno.
/// </summary>
public static class LatinhaSpawnSetupMenu
{
    const string MenuRoot = "Recomeco/Latinhas/";
    const string LatinhaPrefabPath = "Assets/Latinha.prefab";

    [MenuItem(MenuRoot + "Adicionar SpawnManager na cena ativa")]
    static void AddSpawnManagerToScene()
    {
        var existing = Object.FindFirstObjectByType<SpawnManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog(
                "Spawn de latinhas",
                "Já existe um SpawnManager na cena: \"" + existing.gameObject.name + "\".",
                "OK");
            return;
        }

        var latinha = AssetDatabase.LoadAssetAtPath<GameObject>(LatinhaPrefabPath);
        if (latinha == null)
        {
            EditorUtility.DisplayDialog(
                "Spawn de latinhas",
                "Prefab não encontrado em:\n" + LatinhaPrefabPath,
                "OK");
            return;
        }

        var spawnPoint = GameObject.Find("Spawn_EntradaCidade");
        Vector3 position;
        if (spawnPoint != null)
            position = spawnPoint.transform.position;
        else
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            position = player != null ? player.transform.position : Vector3.zero;
        }

        var go = new GameObject("SpawnManager");
        go.transform.position = position;

        var manager = go.AddComponent<SpawnManager>();
        manager.latinhaPrefab = latinha;
        manager.quantidadeInicial = 50;
        manager.areaSpawn = 35f;
        manager.tempoRespawn = 8f;
        manager.snapToGround = true;
        manager.raycastStartHeight = 120f;
        manager.raycastMaxDistance = 250f;
        manager.heightAboveGround = 0.04f;
        manager.maxGroundSlope = 42f;
        manager.maxAttemptsPerSpawn = 16;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
        {
            settings.latinhaPrefab = latinha;
            EditorUtility.SetDirty(settings);
        }

        Undo.RegisterCreatedObjectUndo(go, "Add SpawnManager");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);

        EditorUtility.DisplayDialog(
            "Spawn de latinhas",
            "SpawnManager criado em " + position +
            ".\n\nSalve a cena (Ctrl+S) após conferir no Play.",
            "OK");
    }
}
#endif
