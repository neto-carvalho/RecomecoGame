using UnityEngine;
using UnityEngine.SceneManagement;

public static class InteriorSceneLayout
{
    public static readonly string[] ContentRootNames = { "Room", "PACK1_demo" };

    const string EntryRootName = "PACK1_demo";
    const string ExitPortalName = "Portal_SaidaCasaElegante";

    static readonly string[] SceneMarkerNames =
    {
        "Spawn_EntradaCasaElegante",
        ExitPortalName,
        "Interacao_Salvar_Cama",
        "Interacao_Armario",
    };

    public static bool IsInteriorScene(Scene scene)
    {
        return scene.IsValid() && scene.name == RecomecoSceneNames.InteriorCasaElegante;
    }

    public static float GetTargetScale()
    {
        var settings = RecomecoGameplaySettings.Instance;
        return settings != null ? Mathf.Max(0.01f, settings.playerScaleCity) : 0.2f;
    }

    public static float GetSpawnHeightAboveFloor()
    {
        return Mathf.Max(0.25f, GetTargetScale() * 1.75f);
    }

    /// <summary>
    /// Prepara o interior antes do spawn: escala conteúdo (e marcadores na raiz), colisão e chão.
    /// Não sobrescreve X/Z do spawn definido no editor — só ajusta junto com a escala e o Y no chão.
    /// </summary>
    public static void EnsureGameplayReady()
    {
        if (!IsInteriorScene(SceneManager.GetActiveScene()))
            return;

        EnsureSpawnParentedToContent();
        ApplyScaleIfNeeded();
        Physics.SyncTransforms();
    }

    /// <summary>Chamar depois de EnsureColliders para o raycast acertar o chão.</summary>
    public static void FinalizeMarkerPositions()
    {
        if (!IsInteriorScene(SceneManager.GetActiveScene()))
            return;

        SnapSceneMarkersToFloor();
        Physics.SyncTransforms();
    }

    public static void EnsureScale()
    {
        ApplyScaleIfNeeded();
    }

    static bool ApplyScaleIfNeeded()
    {
        if (!IsInteriorScene(SceneManager.GetActiveScene()))
            return false;

        var target = GetTargetScale();
        var scaledAny = false;

        foreach (var rootName in ContentRootNames)
        {
            var root = GameObject.Find(rootName);
            if (root == null)
                continue;

            var current = root.transform.localScale.x;
            if (current <= target * 1.5f)
                continue;

            var factor = target / current;
            ScaleSceneMarkersWithRoot(root.transform, factor);
            root.transform.localScale = Vector3.one * target;
            scaledAny = true;
        }

        return scaledAny;
    }

    static void ScaleSceneMarkersWithRoot(Transform root, float factor)
    {
        if (root == null || !TryGetBoundsUnderRoot(root, out var bounds))
            return;

        var expanded = bounds;
        expanded.Expand(4f);
        var pivot = root.position;

        foreach (var markerName in SceneMarkerNames)
        {
            var marker = GameObject.Find(markerName);
            if (marker == null || IsUnderContentRoot(marker.transform))
                continue;

            var t = marker.transform;
            var world = t.position;
            var probe = new Vector3(world.x, bounds.center.y, world.z);
            if (!expanded.Contains(probe))
                continue;

            t.position = pivot + factor * (world - pivot);
        }
    }

    static bool IsUnderContentRoot(Transform t)
    {
        while (t != null)
        {
            foreach (var rootName in ContentRootNames)
            {
                if (t.name == rootName)
                    return true;
            }

            t = t.parent;
        }

        return false;
    }

    public static void SnapSceneMarkersToFloor()
    {
        foreach (var markerName in SceneMarkerNames)
        {
            var marker = GameObject.Find(markerName);
            if (marker != null)
                SnapTransformToFloor(marker.transform);
        }

        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp != null)
                SnapTransformToFloor(sp.transform);
        }
    }

    public static void SnapTransformToFloor(Transform target)
    {
        if (target == null)
            return;

        var pos = target.position;
        if (!TryGetFloorAt(pos, out var floorY))
            return;

        target.position = new Vector3(pos.x, floorY + GetSpawnHeightAboveFloor(), pos.z);
    }

    public static bool TryGetFloorAt(Vector3 worldPos, out float floorY)
    {
        floorY = worldPos.y;

        var origin = worldPos + Vector3.up * 80f;
        var hits = Physics.RaycastAll(origin, Vector3.down, 250f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var bestY = float.MinValue;
        var found = false;

        foreach (var hit in hits)
        {
            if (!InteriorSurfaceFilter.IsWalkableFloorHit(hit))
                continue;

            if (hit.point.y > worldPos.y + 3f)
                continue;

            if (hit.point.y <= bestY)
                continue;

            bestY = hit.point.y;
            found = true;
        }

        if (found)
        {
            floorY = bestY;
            return true;
        }

        if (TryGetNearestContentBounds(worldPos, out var bounds))
        {
            floorY = bounds.min.y;
            return true;
        }

        return false;
    }

    public static bool TryGetContentBounds(out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var rootName in ContentRootNames)
        {
            var root = GameObject.Find(rootName);
            if (root == null)
                continue;

            if (!TryGetBoundsUnderRoot(root.transform, out var rootBounds))
                continue;

            if (!hasBounds)
            {
                bounds = rootBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rootBounds);
            }
        }

        return hasBounds;
    }

    static bool TryGetBoundsUnderRoot(Transform root, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    static bool TryGetNearestContentBounds(Vector3 worldPos, out Bounds bounds)
    {
        bounds = default;
        Transform nearestRoot = null;
        var bestDist = float.MaxValue;

        foreach (var rootName in ContentRootNames)
        {
            var root = GameObject.Find(rootName);
            if (root == null || !TryGetBoundsUnderRoot(root.transform, out var rootBounds))
                continue;

            var dist = HorizontalDistance(worldPos, rootBounds.center);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            nearestRoot = root.transform;
            bounds = rootBounds;
        }

        return nearestRoot != null;
    }

    static Transform GetNearestContentRoot(Vector3 worldPos)
    {
        Transform nearestRoot = null;
        var bestDist = float.MaxValue;

        foreach (var rootName in ContentRootNames)
        {
            var root = GameObject.Find(rootName);
            if (root == null || !TryGetBoundsUnderRoot(root.transform, out var rootBounds))
                continue;

            var dist = HorizontalDistance(worldPos, rootBounds.center);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            nearestRoot = root.transform;
        }

        return nearestRoot;
    }

    public static void EnsureSpawnParentedToContent()
    {
        var spawn = FindSpawnPoint(RecomecoSceneNames.EntradaCasaElegante);
        if (spawn == null || IsUnderContentRoot(spawn.transform))
            return;

        var nearest = GetNearestContentRoot(spawn.transform.position);
        if (nearest != null)
            spawn.transform.SetParent(nearest, true);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>Posição inicial sugerida no menu do editor (entrada em PACK1_demo).</summary>
    public static bool TryComputeMarkerPositions(out Vector3 spawnPos, out Vector3 exitPos)
    {
        spawnPos = default;
        exitPos = default;

        var height = GetSpawnHeightAboveFloor();

        if (!TryGetBoundsForRootName(EntryRootName, out var entryBounds) &&
            !TryGetContentBounds(out entryBounds))
        {
            return false;
        }

        spawnPos = new Vector3(entryBounds.center.x, entryBounds.min.y + height, entryBounds.min.z + 0.8f);

        if (!TryGetContentBounds(out var allBounds))
            allBounds = entryBounds;

        exitPos = new Vector3(allBounds.min.x + 0.8f, allBounds.min.y + height, allBounds.center.z);
        return true;
    }

    static bool TryGetBoundsForRootName(string rootName, out Bounds bounds)
    {
        bounds = default;
        var root = GameObject.Find(rootName);
        return root != null && TryGetBoundsUnderRoot(root.transform, out bounds);
    }

    public static bool TryGetSafeSpawnPosition(string spawnId, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        var spawn = FindSpawnPoint(spawnId);
        if (spawn == null)
            return false;

        position = spawn.transform.position + spawn.positionOffset;
        rotation = spawn.transform.rotation;
        return true;
    }

    static SceneSpawnPoint FindSpawnPoint(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            return null;

        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp != null && sp.spawnId == spawnId)
                return sp;
        }

        return null;
    }
}
