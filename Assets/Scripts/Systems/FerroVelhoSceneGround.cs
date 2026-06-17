using UnityEngine;
using UnityEngine.SceneManagement;

public static class FerroVelhoSceneGround
{
    const string GeometryName = "Geometry";
    const string TerrainParentName = "Terrain";
    const string TerrainMeshName = "default";

    public static bool HasWalkableSceneGround(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != GeometryName)
                continue;
            if (HasWalkableColliderUnder(root.transform))
                return true;
        }

        return HasWalkableColliderUnder(null);
    }

    public static bool HasWalkableSceneGround()
    {
        return HasWalkableSceneGround(SceneManager.GetActiveScene());
    }

    public static int EnsureSceneGroundColliders(Scene scene)
    {
        if (!scene.IsValid() || !FerroVelhoWalkableGround.IsFerroVelhoScene(scene))
            return 0;

        var count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != GeometryName)
                continue;
            count += EnsureUnderGeometry(root.transform);
        }

        if (count == 0)
            count += EnsureByMaterialFallback();

        return count;
    }

    public static int EnsureSceneGroundColliders()
    {
        return EnsureSceneGroundColliders(SceneManager.GetActiveScene());
    }

    static int EnsureUnderGeometry(Transform geometry)
    {
        var count = 0;
        var terrain = geometry.Find(TerrainParentName);
        if (terrain != null)
        {
            var mesh = terrain.Find(TerrainMeshName);
            if (mesh != null && EnsureMeshCollider(mesh.gameObject))
                count++;
            else
            {
                foreach (Transform child in terrain)
                {
                    if (EnsureMeshCollider(child.gameObject))
                        count++;
                }
            }
        }

        return count;
    }

    static int EnsureByMaterialFallback()
    {
        var count = 0;
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;
            if (!renderer.sharedMaterial.name.Contains("Terrain"))
                continue;
            if (EnsureMeshCollider(renderer.gameObject))
                count++;
        }

        return count;
    }

    static bool EnsureMeshCollider(GameObject go)
    {
        var filter = go.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
            return false;

        var collider = go.GetComponent<MeshCollider>();
        if (collider == null)
            collider = go.AddComponent<MeshCollider>();

        collider.sharedMesh = filter.sharedMesh;
        collider.convex = false;
        collider.isTrigger = false;
        collider.enabled = true;

        go.isStatic = true;
        return true;
    }

    static bool HasWalkableColliderUnder(Transform root)
    {
        var filters = root != null
            ? root.GetComponentsInChildren<MeshFilter>(true)
            : Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        foreach (var filter in filters)
        {
            if (filter == null || filter.sharedMesh == null)
                continue;
            if (!IsTerrainObject(filter.gameObject))
                continue;

            var collider = filter.GetComponent<MeshCollider>();
            if (collider != null && collider.enabled && !collider.isTrigger && collider.sharedMesh != null)
                return true;
        }

        return false;
    }

    static bool IsTerrainObject(GameObject go)
    {
        var walk = go.transform;
        var foundTerrainParent = false;
        var foundGeometry = false;

        while (walk != null)
        {
            if (walk.name == TerrainParentName)
                foundTerrainParent = true;
            if (walk.name == GeometryName)
                foundGeometry = true;
            walk = walk.parent;
        }

        if (foundGeometry && foundTerrainParent)
            return true;

        var renderer = go.GetComponent<MeshRenderer>();
        return renderer != null && renderer.sharedMaterial != null &&
               renderer.sharedMaterial.name.Contains("Terrain");
    }
}
