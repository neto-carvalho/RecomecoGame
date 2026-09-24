using System.Collections.Generic;
using UnityEngine;

public static class CityTrafficRouteGenerator
{
    public const string AutoRoutePrefix = "Rota_Auto_";
    public const string NetworkRoutePrefix = "Rota_Rede_";

    public static bool IsGeneratedRouteName(string routeName)
    {
        if (string.IsNullOrEmpty(routeName))
            return false;

        return routeName.StartsWith(AutoRoutePrefix) || routeName.StartsWith(NetworkRoutePrefix);
    }
    const float MinRoadLength = 10f;
    const float WaypointSpacing = 14f;

    public static bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
            return false;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return false;

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.size.sqrMagnitude > 0.01f;
    }

    public static List<Vector3> BuildCenterline(Transform road, Bounds worldBounds)
    {
        if (!TryGetFarthestEndsXZ(worldBounds, out var start, out var end))
            return null;

        var length = HorizontalDistance(start, end);
        if (length < MinRoadLength)
            return null;

        var steps = Mathf.Max(2, Mathf.CeilToInt(length / WaypointSpacing) + 1);
        var points = new List<Vector3>(steps);
        for (var i = 0; i < steps; i++)
        {
            var t = i / (float)(steps - 1);
            var p = Vector3.Lerp(start, end, t);
            p.y = VehicleGroundSnap.SnapYNearReference(p.x, p.z, p.y);
            points.Add(p);
        }

        return points;
    }

    static bool TryGetFarthestEndsXZ(Bounds bounds, out Vector3 start, out Vector3 end)
    {
        start = default;
        end = default;
        var c = bounds.center;
        var e = bounds.extents;
        var bestDist = 0f;

        var corners = new Vector3[4];
        corners[0] = new Vector3(c.x - e.x, c.y, c.z - e.z);
        corners[1] = new Vector3(c.x + e.x, c.y, c.z - e.z);
        corners[2] = new Vector3(c.x - e.x, c.y, c.z + e.z);
        corners[3] = new Vector3(c.x + e.x, c.y, c.z + e.z);

        for (var i = 0; i < corners.Length; i++)
        {
            for (var j = i + 1; j < corners.Length; j++)
            {
                var d = HorizontalDistance(corners[i], corners[j]);
                if (d > bestDist)
                {
                    bestDist = d;
                    start = corners[i];
                    end = corners[j];
                }
            }
        }

        return bestDist >= MinRoadLength;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    /// <summary>
    /// Em cantos agudos, insere recuos antes/depois do cruzamento mantendo o ponto central (curva em L, sem diagonal).
    /// </summary>
    public static List<Vector3> ExpandPathWithCornerInsets(IReadOnlyList<Vector3> path, bool closedLoop, float insetMeters, float minTurnAngle)
    {
        var result = new List<Vector3>();
        if (path == null || path.Count < 2)
            return result;

        insetMeters = Mathf.Max(1.5f, insetMeters);
        var count = path.Count;

        for (var i = 0; i < count; i++)
        {
            var prevIdx = i - 1;
            var nextIdx = i + 1;
            if (prevIdx < 0)
                prevIdx = closedLoop ? count - 1 : -1;
            if (nextIdx >= count)
                nextIdx = closedLoop ? 0 : -1;

            if (prevIdx < 0 || nextIdx < 0)
            {
                AppendIfFar(result, path[i], 0.35f);
                continue;
            }

            var prev = path[prevIdx];
            var corner = path[i];
            var next = path[nextIdx];

            if (!TryGetCornerInsets(prev, corner, next, insetMeters, minTurnAngle, out var beforeCorner, out var afterCorner))
            {
                AppendIfFar(result, corner, 0.35f);
                continue;
            }

            AppendIfFar(result, beforeCorner, 0.35f);
            AppendIfFar(result, corner, 0.35f);
            AppendIfFar(result, afterCorner, 0.35f);
        }

        return result;
    }

    static void AppendIfFar(List<Vector3> list, Vector3 point, float minDist)
    {
        if (list.Count == 0 || HorizontalDistance(list[list.Count - 1], point) > minDist)
            list.Add(point);
    }

    public static bool TryGetCornerInsets(
        Vector3 prev,
        Vector3 corner,
        Vector3 next,
        float insetMeters,
        float minTurnAngle,
        out Vector3 beforeCorner,
        out Vector3 afterCorner)
    {
        beforeCorner = default;
        afterCorner = default;

        var inDir = corner - prev;
        inDir.y = 0f;
        var outDir = next - corner;
        outDir.y = 0f;
        if (inDir.sqrMagnitude < 0.01f || outDir.sqrMagnitude < 0.01f)
            return false;

        inDir.Normalize();
        outDir.Normalize();
        var angle = Vector3.Angle(inDir, outDir);
        if (angle < minTurnAngle)
            return false;

        var inLen = HorizontalDistance(prev, corner);
        var outLen = HorizontalDistance(corner, next);
        var inset = Mathf.Min(insetMeters, inLen * 0.45f, outLen * 0.45f);
        if (inset < 1f)
            return false;

        beforeCorner = corner - inDir * inset;
        beforeCorner.y = corner.y;
        afterCorner = corner + outDir * inset;
        afterCorner.y = corner.y;
        return true;
    }

    public static void OrderTransformsAsContinuousPath(List<Transform> waypoints)
    {
        if (waypoints == null || waypoints.Count < 2)
            return;

        var remaining = new List<Transform>(waypoints);
        var ordered = new List<Transform> { remaining[0] };
        remaining.RemoveAt(0);

        while (remaining.Count > 0)
        {
            var last = ordered[ordered.Count - 1].position;
            var bestIndex = 0;
            var bestDist = float.MaxValue;
            for (var i = 0; i < remaining.Count; i++)
            {
                var d = HorizontalDistance(last, remaining[i].position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIndex = i;
                }
            }

            ordered.Add(remaining[bestIndex]);
            remaining.RemoveAt(bestIndex);
        }

        for (var i = 0; i < ordered.Count; i++)
            waypoints[i] = ordered[i];
    }

    public static TrafficRoute CreateRouteFromPoints(
        Transform parent,
        string routeName,
        List<Vector3> points,
        bool loop,
        bool pingPong)
    {
        if (parent == null || points == null || points.Count < 2)
            return null;

        var routeGo = new GameObject(routeName);
        routeGo.transform.SetParent(parent, false);

        var route = routeGo.AddComponent<TrafficRoute>();
        route.loop = loop;
        route.pingPong = pingPong;
        route.speedKmh = 26f;

        var waypoints = new Transform[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var wp = new GameObject("Waypoint_" + i.ToString("00"));
            wp.transform.SetParent(routeGo.transform, true);
            wp.transform.position = points[i];
            waypoints[i] = wp.transform;
        }

        route.waypoints = waypoints;
        return route;
    }
}
