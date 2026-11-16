using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projeta pontos no eixo central dos trechos EasyRoads / malha de rua.
/// </summary>
public static class RoadCenterSnap
{
    const float MaxSnapDistanceMeters = 28f;
    const float MinRoadLength = 6f;

    static List<RoadAxis> s_roadCache;
    static double s_cacheTime;

    readonly struct RoadAxis
    {
        public readonly Vector3 Start;
        public readonly Vector3 End;
        public readonly Vector3 Dir;

        public RoadAxis(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
            var d = end - start;
            d.y = 0f;
            Dir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }
    }

    public static void InvalidateCache()
    {
        s_roadCache = null;
    }

    public static Vector3 SnapToRoadCenter(Vector3 world, Vector3 directionHint)
    {
        EnsureRoadCache();
        if (s_roadCache == null || s_roadCache.Count == 0)
            return world;

        directionHint = Flatten(directionHint);
        if (directionHint.sqrMagnitude > 0.01f)
            directionHint.Normalize();

        var bestDist = float.MaxValue;
        var bestPoint = world;
        var hintValid = directionHint.sqrMagnitude > 0.01f;

        foreach (var axis in s_roadCache)
        {
            if (!TryProjectOnAxis(world, axis, out var projected, out var lateralDist))
                continue;

            var score = lateralDist;
            if (hintValid && Vector3.Dot(axis.Dir, directionHint) < 0.25f)
                score += 6f;

            if (score >= bestDist)
                continue;

            bestDist = score;
            bestPoint = projected;
        }

        if (bestDist > MaxSnapDistanceMeters)
            return world;

        bestPoint.y = VehicleGroundSnap.SnapLowestRoadY(bestPoint.x, bestPoint.z, world.y);
        return bestPoint;
    }

    public static void SnapTrafficRouteToRoadCenters(TrafficRoute route)
    {
        if (route == null)
            return;

        route.EnsureReady();
        if (route.waypoints == null || route.waypoints.Length == 0)
            return;

        EnsureRoadCache();
        var count = route.waypoints.Length;
        for (var i = 0; i < count; i++)
        {
            var wp = route.waypoints[i];
            if (wp == null)
                continue;

            var prev = wp.position;
            var next = wp.position;
            if (i > 0 && route.waypoints[i - 1] != null)
                prev = route.waypoints[i - 1].position;
            if (i < count - 1 && route.waypoints[i + 1] != null)
                next = route.waypoints[i + 1].position;

            var hint = Flatten(next - prev);
            if (hint.sqrMagnitude < 0.01f && i > 0 && route.waypoints[i - 1] != null)
                hint = Flatten(wp.position - route.waypoints[i - 1].position);

            var centered = SnapToRoadCenter(wp.position, hint);
            wp.position = centered;
        }
    }

    public static void SnapAllRoutesInScene()
    {
        InvalidateCache();
        foreach (var route in Object.FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None))
        {
            if (route != null)
                SnapTrafficRouteToRoadCenters(route);
        }
    }

    static bool TryProjectOnAxis(Vector3 world, RoadAxis axis, out Vector3 projected, out float lateralDistance)
    {
        projected = world;
        lateralDistance = float.MaxValue;

        var a = Flatten(axis.Start);
        var b = Flatten(axis.End);
        var seg = b - a;
        var len = seg.magnitude;
        if (len < MinRoadLength)
            return false;

        var dir = seg / len;
        var along = Vector3.Dot(Flatten(world) - a, dir);
        if (along < -2f || along > len + 2f)
            return false;

        along = Mathf.Clamp(along, 0f, len);
        projected = a + dir * along;
        projected.y = world.y;
        lateralDistance = HorizontalDistance(Flatten(world), projected);
        return true;
    }

    public static bool TryGetCenterlineEnds(Transform road, out Vector3 start, out Vector3 end)
    {
        start = default;
        end = default;
        if (road == null || !CityTrafficRouteGenerator.TryGetWorldBounds(road, out var bounds))
            return false;

        var forward = Flatten(road.forward);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        var perp = Vector3.Cross(Vector3.up, forward);
        var halfLen = Mathf.Abs(Vector3.Dot(forward, bounds.extents));
        if (halfLen < MinRoadLength * 0.5f)
            halfLen = Mathf.Max(bounds.extents.x, bounds.extents.z);

        var center = bounds.center;
        start = center - forward * halfLen;
        end = center + forward * halfLen;
        start.y = center.y;
        end.y = center.y;
        return HorizontalDistance(start, end) >= MinRoadLength;
    }

    static void EnsureRoadCache()
    {
        if (s_roadCache != null && Application.isPlaying && Time.timeAsDouble - s_cacheTime < 2.0)
            return;

        if (s_roadCache != null && s_roadCache.Count > 0 && !Application.isPlaying)
            return;

        s_roadCache = new List<RoadAxis>();
        foreach (var road in CollectRoadRoots())
        {
            if (!TryGetCenterlineEnds(road, out var a, out var b))
                continue;

            a.y = VehicleGroundSnap.SnapLowestRoadY(a.x, a.z, a.y);
            b.y = VehicleGroundSnap.SnapLowestRoadY(b.x, b.z, b.y);
            s_roadCache.Add(new RoadAxis(a, b));
        }

        s_cacheTime = Time.timeAsDouble;
    }

    static List<Transform> CollectRoadRoots()
    {
        var list = new List<Transform>();
        var roadObjects = GameObject.Find("Road Objects");
        if (roadObjects != null)
        {
            foreach (Transform child in roadObjects.transform)
            {
                if (child != null)
                    list.Add(child);
            }

            if (list.Count > 0)
                return list;
        }

        var ruas = GameObject.Find("Ruas");
        if (ruas != null)
        {
            foreach (Transform child in ruas.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || child == ruas.transform)
                    continue;

                var n = child.name;
                if (n.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && CityTrafficRouteGenerator.TryGetWorldBounds(child, out _))
                    list.Add(child);
            }
        }

        var road = GameObject.Find("Road");
        if (road != null)
        {
            foreach (Transform child in road.transform)
            {
                if (child != null && CityTrafficRouteGenerator.TryGetWorldBounds(child, out _))
                    list.Add(child);
            }
        }

        return list;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
