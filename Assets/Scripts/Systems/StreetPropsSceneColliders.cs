using UnityEngine;
using UnityEngine.SceneManagement;

public static class StreetPropsSceneColliders
{
    public const string MoradiaRootName = "Lugar abandonado";

    public static int EnsureMoradiaColliders(Scene scene)
    {
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
            return 0;

        var root = FindMoradiaRoot(scene);
        return root != null ? EnsureUnderRoot(root.transform) : 0;
    }

    public static int EnsureMoradiaColliders()
    {
        return EnsureMoradiaColliders(SceneManager.GetActiveScene());
    }

    static GameObject FindMoradiaRoot(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == MoradiaRootName)
                return root;
        }

        return GameObject.Find(MoradiaRootName);
    }

    static int EnsureUnderRoot(Transform root)
    {
        var count = 0;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null)
                continue;
            if (ShouldSkip(filter.gameObject))
                continue;
            if (TryAddMeshCollider(filter))
                count++;
        }

        return count;
    }

    static bool ShouldSkip(GameObject go)
    {
        if (go == null || go.CompareTag("Player"))
            return true;

        if (go.GetComponent<MeshRenderer>() == null)
            return true;

        if (go.GetComponent<SceneSpawnPoint>() != null)
            return true;

        foreach (var col in go.GetComponents<Collider>())
        {
            if (col != null && col.enabled && !col.isTrigger)
                return true;
        }

        return false;
    }

    static bool TryAddMeshCollider(MeshFilter filter)
    {
        var go = filter.gameObject;
        var mesh = filter.sharedMesh;

        var collider = go.GetComponent<MeshCollider>();
        if (collider == null)
            collider = go.AddComponent<MeshCollider>();

        collider.sharedMesh = mesh;
        collider.convex = false;
        collider.isTrigger = false;
        collider.enabled = true;

        go.isStatic = true;
        return true;
    }
}
