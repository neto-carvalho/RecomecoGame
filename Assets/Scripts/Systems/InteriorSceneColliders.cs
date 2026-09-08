using UnityEngine;
using UnityEngine.SceneManagement;

public static class InteriorSceneColliders
{
    public static bool IsInteriorScene(Scene scene)
    {
        return InteriorSceneLayout.IsInteriorScene(scene);
    }

    public static int EnsureColliders()
    {
        return EnsureColliders(SceneManager.GetActiveScene());
    }

    public static int EnsureColliders(Scene scene)
    {
        if (!IsInteriorScene(scene))
            return 0;

        var count = 0;
        foreach (var rootName in InteriorSceneLayout.ContentRootNames)
        {
            var root = GameObject.Find(rootName);
            if (root == null)
                continue;

            count += EnsureUnderRoot(root.transform);
            InteriorSurfaceFilter.DisableSolidCollidersOnCeilings(root.transform);
        }

        if (count == 0)
            count += EnsureUnderRoot(null);

        return count;
    }

    static int EnsureUnderRoot(Transform root)
    {
        var count = 0;
        MeshFilter[] filters;

        if (root != null)
            filters = root.GetComponentsInChildren<MeshFilter>(true);
        else
            filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        foreach (var filter in filters)
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

        var name = go.name;
        if (name.StartsWith("Interacao_") || name.StartsWith("Portal_") || name.StartsWith("Spawn_"))
            return true;

        if (name is "Main Camera" or "Directional Light")
            return true;

        if (InteriorSurfaceFilter.IsCeilingOrRoof(go))
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
