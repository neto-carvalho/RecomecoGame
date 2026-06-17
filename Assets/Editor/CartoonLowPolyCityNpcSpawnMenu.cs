#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CartoonLowPolyCityNpcSpawnMenu
{
    const string MenuPath = "Recomeco/NPC/Criar marcadores de spawn (SidewalkNpcSpawnPoint)";

    [MenuItem(MenuPath)]
    static void CreateSpawnMarkers()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            EditorUtility.DisplayDialog("NPC spawn", "Abre uma cena primeiro.", "OK");
            return;
        }

        if (scene.rootCount > 0 && GameObject.Find("SidewalkNpcSpawns") != null)
        {
            if (!EditorUtility.DisplayDialog("NPC spawn",
                    "Já existe um objeto chamado \"SidewalkNpcSpawns\" na cena. Criar outro grupo mesmo assim?",
                    "Sim", "Cancelar"))
                return;
        }

        var pivot = Vector3.zero;
        if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
            pivot = SceneView.lastActiveSceneView.camera.transform.position;

        var root = new GameObject("SidewalkNpcSpawns");
        Undo.RegisterCreatedObjectUndo(root, "Create SidewalkNpcSpawns");
        root.transform.position = pivot;

        for (var i = 0; i < 4; i++)
        {
            var child = new GameObject($"SidewalkNpcSpawn_{i + 1:00}");
            Undo.RegisterCreatedObjectUndo(child, "Create spawn");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(i * 2f, 0f, 0f);
            child.transform.localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 0f : 180f, 0f);
            child.AddComponent<SidewalkNpcSpawnPoint>();
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log(
            "[NPC] Criado \"SidewalkNpcSpawns\" com 4 filhos. Arrasta cada filho para a calçada, roda o objeto (Y) " +
            "para o sentido da rua e ajusta Patrol Half Length no Inspector. Guarda a cena (Ctrl+S).");
    }
}
#endif
