using UnityEngine;

/// <summary>Curva Catmull-Rom entre waypoints — cantos arredondados em vez de vértice seco.</summary>
public static class TrafficRouteSpline
{
    public static bool TryEvaluate(
        TrafficRoute route,
        int waypointIndex,
        int stepDirection,
        float alongMeters,
        float segmentLength,
        out Vector3 position,
        out Vector3 tangent)
    {
        position = default;
        tangent = Vector3.forward;

        if (route == null || segmentLength < 0.05f)
            return false;

        var dest = ResolveDestination(route, waypointIndex, stepDirection);
        var t = Mathf.Clamp01(alongMeters / segmentLength);

        var p0 = Flatten(GetWaypoint(route, waypointIndex - stepDirection, stepDirection));
        var p1 = Flatten(route.GetWorldPoint(waypointIndex));
        var p2 = Flatten(route.GetWorldPoint(dest));
        var p3 = Flatten(GetWaypoint(route, dest + stepDirection, stepDirection));

        position = CatmullRom(p0, p1, p2, p3, t);
        tangent = CatmullRomDerivative(p0, p1, p2, p3, t);
        tangent.y = 0f;
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = (p2 - p1).normalized;
        else
            tangent.Normalize();

        return true;
    }

    static Vector3 GetWaypoint(TrafficRoute route, int index, int stepDirection)
    {
        var count = route.WaypointCount;
        if (count == 0)
            return Vector3.zero;

        if (route.loop)
        {
            index = ((index % count) + count) % count;
            return route.GetWorldPoint(index);
        }

        if (index < 0)
            return route.GetWorldPoint(0);
        if (index >= count)
            return route.GetWorldPoint(count - 1);

        return route.GetWorldPoint(index);
    }

    static int ResolveDestination(TrafficRoute route, int from, int step)
    {
        var count = route.WaypointCount;
        if (count == 0)
            return 0;

        var next = from + step;
        if (route.pingPong)
            return Mathf.Clamp(next, 0, count - 1);

        if (next >= count)
            return route.loop ? 0 : count - 1;
        if (next < 0)
            return route.loop ? count - 1 : 0;

        return next;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5f * (
            2f * p1
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    static Vector3 CatmullRomDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var t2 = t * t;
        return 0.5f * (
            (-p0 + p2)
            + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t
            + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
