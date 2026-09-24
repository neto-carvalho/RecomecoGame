using System.Collections.Generic;
using UnityEngine;

public static class CityTrafficZone
{
    const float DefaultRadius = 110f;
    public const float NetworkRadius = 220f;
    const float MinWaypointY = -3f;
    const float MaxWaypointY = 16f;
    const float MaxSegmentLength = 120f;

    public static IReadOnlyList<Vector3> GetAnchors()
    {
        var list = new List<Vector3>(4);
        TryAddAnchor(list, "Lojinha");
        TryAddAnchor(list, "Spawn_EntradaCidade");
        TryAddAnchor(list, "Spawn_SaidaCasaElegante");
        return list;
    }

    static void TryAddAnchor(List<Vector3> list, string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
            return;

        var p = go.transform.position;
        foreach (var existing in list)
        {
            if ((existing - p).sqrMagnitude < 0.25f)
                return;
        }

        list.Add(p);
    }

    public static bool TryGetCenter(out Vector3 center)
    {
        return TryGetCompositeCenter(out center);
    }

    public static bool TryGetCompositeCenter(out Vector3 center)
    {
        var anchors = GetAnchors();
        if (anchors.Count == 0)
        {
            center = default;
            return false;
        }

        var sum = Vector3.zero;
        foreach (var a in anchors)
            sum += a;

        center = sum / anchors.Count;
        return true;
    }

    public static bool ContainsForNetwork(Vector3 world)
    {
        return ContainsHorizontal(world, NetworkRadius);
    }

    public static bool ContainsHorizontal(Vector3 world, float radius = DefaultRadius)
    {
        var anchors = GetAnchors();
        if (anchors.Count == 0)
            return true;

        foreach (var center in anchors)
        {
            var dx = world.x - center.x;
            var dz = world.z - center.z;
            if (dx * dx + dz * dz <= radius * radius)
                return true;
        }

        return false;
    }

    public static bool IsPlausibleRoute(TrafficRoute route, float radius = DefaultRadius)
    {
        if (route == null)
            return false;

        route.EnsureReady();
        if (route.WaypointCount < 2)
            return false;

        var useNetwork = route.name.StartsWith(CityTrafficRouteGenerator.NetworkRoutePrefix);

        for (var i = 0; i < route.WaypointCount; i++)
        {
            var p = route.GetWorldPoint(i);
            if (useNetwork ? !ContainsForNetwork(p) : !ContainsHorizontal(p, radius))
                return false;

            if (p.y < MinWaypointY || p.y > MaxWaypointY)
                return false;

            var next = route.GetWorldPoint(i + 1);
            var seg = Vector3.Distance(
                new Vector3(p.x, 0f, p.z),
                new Vector3(next.x, 0f, next.z));
            if (seg > MaxSegmentLength)
                return false;
        }

        return true;
    }
}
