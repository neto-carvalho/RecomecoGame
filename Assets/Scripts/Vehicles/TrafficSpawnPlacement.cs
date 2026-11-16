using UnityEngine;

public static class TrafficSpawnPlacement
{
    public static void PlaceOnRoute(TrafficRoute route, float routeFraction01, out int waypointIndex, out float alongSegment01)
    {
        waypointIndex = 0;
        alongSegment01 = 0f;
        if (route == null)
            return;

        route.EnsureReady();
        var count = route.WaypointCount;
        if (count < 2)
            return;

        var total = 0f;
        for (var i = 0; i < count; i++)
        {
            var next = NextIndex(route, i, 1);
            if (next == i)
                continue;

            total += SegmentLength(route, i, next);
        }

        if (total < 0.1f)
            return;

        var target = Mathf.Clamp01(routeFraction01) * total;
        for (var i = 0; i < count; i++)
        {
            var next = NextIndex(route, i, 1);
            if (next == i)
                continue;

            var len = SegmentLength(route, i, next);
            if (target <= len)
            {
                waypointIndex = i;
                alongSegment01 = len > 0.001f ? target / len : 0f;
                return;
            }

            target -= len;
        }

        waypointIndex = count - 1;
        alongSegment01 = 0f;
    }

    static float SegmentLength(TrafficRoute route, int a, int b)
    {
        var p = route.GetWorldPoint(a);
        var q = route.GetWorldPoint(b);
        var dx = p.x - q.x;
        var dz = p.z - q.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static int NextIndex(TrafficRoute route, int from, int step)
    {
        var count = route.WaypointCount;
        var next = from + step;
        if (route.pingPong)
            return Mathf.Clamp(next, 0, count - 1);

        if (next >= count)
            return route.loop ? 0 : count - 1;
        if (next < 0)
            return route.loop ? count - 1 : 0;

        return next;
    }
}
