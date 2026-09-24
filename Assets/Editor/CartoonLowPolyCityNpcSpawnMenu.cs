#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CartoonLowPolyCityNpcSpawnMenu
{
    const string MenuPath = "Recomeco/NPC/Criar marcadores de spawn (SidewalkNpcSpawnPoint)";

    [MenuItem(MenuPath)]
    static void CreateSpawnMarkers()
    {
        if (GameObject.Find("SidewalkNpcSpawns") != null)
        {
            if (!EditorUtility.DisplayDialog("NPC spawn",
                    "Já existe um objeto chamado \"SidewalkNpcSpawns\" na cena. Criar outro grupo mesmo assim?",
                    "Sim", "Cancelar"))
                return;
        }

        CreateSpawnMarkersInternal();
    }

    public static void CreateSpawnMarkersInternal()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return;

        if (GameObject.Find("SidewalkNpcSpawns") != null)
            return;

        var pivot = Vector3.zero;
        var lojinha = GameObject.Find("Lojinha");
        if (lojinha != null)
            pivot = lojinha.transform.position + new Vector3(-3f, 0f, 2f);
        else if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
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

        Selection.activeObject = root;
        EditorGUIUtility.PingObject(root);
    }
}
#endif
