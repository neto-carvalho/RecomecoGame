#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class CityNavMeshSetupMenu
{
    const string MenuRoot = "Recomeco/Cidade/";
    const string RootName = "CityPedestrianNavMesh";

    [MenuItem(MenuRoot + "Assar NavMesh para pedestres")]
    static void BakePedestrianNavMesh()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog(
                "NavMesh pedestres",
                "Abra a cena Cidade antes de assar o NavMesh.",
                "OK");
            return;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create City NavMesh root");
        }

        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null)
            surface = Undo.AddComponent<NavMeshSurface>(root);

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.layerMask = ~0;
        surface.overrideTileSize = false;
        surface.overrideVoxelSize = false;
        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(
            "NavMesh pedestres",
            "NavMesh assado.\n\n" +
            "Se pedestres atravessarem prédios: selecione só terreno/ruas/calçadas no Hierarchy, " +
            "ative Navigation Static (Inspector → Static) e assine de novo.\n" +
            "Prédios devem ficar SEM Navigation Static.\n\n" +
            "Salve a cena (Ctrl+S) e dê Play.",
            "OK");
    }
}
#endif
