#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CartoonLowPolyCityNpcSpawnMenu
{
    const string MenuPath = "Recomeco/NPC/Criar marcadores de spawn (SidewalkNpcSpawnPoint)";
    const string ExpandPath = "Recomeco/NPC/Completar marcadores existentes (até 12)";
    const string SpawnRootName = "SidewalkNpcSpawns";
    const int TargetSpawnCount = 12;
    const float Spacing = 14f;

    [MenuItem(MenuPath)]
    static void CreateSpawnMarkers()
    {
        var existing = GameObject.Find(SpawnRootName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("NPC spawn",
                    "Já existe \"" + SpawnRootName + "\". Completar até 12 marcadores no grupo atual?",
                    "Completar", "Cancelar"))
                return;

            ExpandSpawnMarkers(existing.transform, TargetSpawnCount);
            MarkSceneDirty();
            return;
        }

        CreateSpawnMarkersInternal();
    }

    [MenuItem(ExpandPath)]
    static void ExpandSpawnMarkersMenu()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("NPC spawn", "Abra a cena Cidade.", "OK");
            return;
        }

        var root = GameObject.Find(SpawnRootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("NPC spawn",
                "Não há \"" + SpawnRootName + "\" na cena. Use primeiro:\n" + MenuPath, "OK");
            return;
        }

        ExpandSpawnMarkers(root.transform, TargetSpawnCount);
        MarkSceneDirty();
        Selection.activeObject = root;
        EditorUtility.DisplayDialog("NPC spawn",
            "Marcadores atualizados. Arraste cada um para a calçada e salve a cena (Ctrl+S).", "OK");
    }

    public static void CreateSpawnMarkersInternal()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return;

        var existing = GameObject.Find(SpawnRootName);
        if (existing != null)
        {
            ExpandSpawnMarkers(existing.transform, TargetSpawnCount);
            return;
        }

        var pivot = Vector3.zero;
        var lojinha = GameObject.Find("Lojinha");
        if (lojinha != null)
            pivot = lojinha.transform.position + new Vector3(-3f, 0f, 2f);
        else if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            pivot = SceneView.lastActiveSceneView.camera.transform.position;

        var root = new GameObject(SpawnRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create SidewalkNpcSpawns");
        root.transform.position = pivot;

        AddSpawnChildren(root.transform, 0, TargetSpawnCount);

        Selection.activeObject = root;
        EditorGUIUtility.PingObject(root);
    }

    static void ExpandSpawnMarkers(Transform root, int targetCount)
    {
        var index = CountSpawnMarkers(root);
        if (index >= targetCount)
            return;

        AddSpawnChildren(root, index, targetCount);
    }

    static int CountSpawnMarkers(Transform root)
    {
        var count = 0;
        foreach (Transform ch in root)
        {
            if (ch.GetComponent<SidewalkNpcSpawnPoint>() != null)
                count++;
        }

        return count;
    }

    static void AddSpawnChildren(Transform root, int startIndex, int targetCount)
    {
        for (var i = startIndex; i < targetCount; i++)
        {
            var row = i / 4;
            var col = i % 4;
            var child = new GameObject($"SidewalkNpcSpawn_{i + 1:00}");
            Undo.RegisterCreatedObjectUndo(child, "Create NPC spawn marker");
            child.transform.SetParent(root, false);
            child.transform.localPosition = new Vector3((col - 1.5f) * Spacing, 0f, row * Spacing);
            child.transform.localRotation = Quaternion.Euler(0f, (i % 4) * 90f, 0f);
            if (child.GetComponent<SidewalkNpcSpawnPoint>() == null)
                child.AddComponent<SidewalkNpcSpawnPoint>();
        }
    }

    static void MarkSceneDirty()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }
}
#endif
