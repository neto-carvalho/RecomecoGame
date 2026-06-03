using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Colliders físicos nos meshes do junkyard (Props/Buildings) — o pack vem só com MeshRenderer.
/// </summary>
public static class FerroVelhoSceneColliders
{
    const string GeometryName = "Geometry";
    const string TerrainName = "Terrain";

    public static int EnsureColliders(Scene scene)
    {
        if (!scene.IsValid())
            return 0;

        var count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != GeometryName)
                continue;
            count += EnsureUnderGeometry(root.transform);
        }

        return count;
    }

    public static int EnsureColliders()
    {
        return EnsureColliders(SceneManager.GetActiveScene());
    }

    static int EnsureUnderGeometry(Transform geometryRoot)
    {
        var count = 0;
        foreach (var filter in geometryRoot.GetComponentsInChildren<MeshFilter>(true))
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
        if (go == null)
            return true;

        if (go.CompareTag("Player"))
            return true;

        if (IsUnderTerrain(go))
            return true;

        if (go.GetComponent<MeshRenderer>() == null)
            return true;

        if (go.GetComponent<SceneSpawnPoint>() != null ||
            go.GetComponent<SceneTransitionZone>() != null ||
            go.GetComponent<SellItems>() != null)
            return true;

        var name = go.name;
        if (name == "Chao_FerroVelho" || name.StartsWith("Spawn_") || name.StartsWith("Portal_"))
            return true;

        foreach (var col in go.GetComponents<Collider>())
        {
            if (col != null && col.enabled && !col.isTrigger)
                return true;
        }

        return false;
    }

    static bool IsUnderTerrain(GameObject go)
    {
        for (var t = go.transform; t != null; t = t.parent)
        {
            if (t.name != TerrainName)
                continue;
            for (var p = t.parent; p != null; p = p.parent)
            {
                if (p.name == GeometryName)
                    return true;
            }
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
