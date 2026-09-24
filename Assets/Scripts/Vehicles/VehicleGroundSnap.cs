using UnityEngine;

public static class VehicleGroundSnap
{
    const float RayStartHeight = 280f;
    const float MaxRoadHeight = 18f;
    const float MaxAboveReference = 2.8f;

    static LayerMask GroundMask => ~0;

    public static Vector3 Snap(Vector3 worldPosition)
    {
        worldPosition.y = SnapY(worldPosition.x, worldPosition.z, worldPosition.y);
        return worldPosition;
    }

    public static float SnapY(Vector3 worldPosition)
    {
        return SnapY(worldPosition.x, worldPosition.z, worldPosition.y);
    }

    public static float SnapY(float x, float z, float fallbackY)
    {
        return SnapYNearReference(x, z, fallbackY);
    }

    public static float SnapYNearReference(float x, float z, float referenceY)
    {
        var origin = new Vector3(x, RayStartHeight, z);
        var hits = Physics.RaycastAll(origin, Vector3.down, RayStartHeight + 40f, GroundMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return referenceY;

        var bestDelta = float.PositiveInfinity;
        var bestY = referenceY;

        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.normal.y < 0.55f)
                continue;

            if (IsVehicleCollider(hit.collider))
                continue;

            if (IsTreeOrFoliageCollider(hit.collider))
                continue;

            var y = hit.point.y;
            if (y > MaxRoadHeight)
                continue;

            if (y > referenceY + MaxAboveReference)
                continue;

            var delta = Mathf.Abs(y - referenceY);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestY = y;
            }
        }

        return bestY + 0.08f;
    }

    public static void SnapTransform(Transform t)
    {
        if (t == null)
            return;

        var pos = t.position;
        pos.y = SnapY(pos.x, pos.z, pos.y);
        t.position = pos;
    }

    static bool IsTreeOrFoliageCollider(Collider collider)
    {
        var t = collider.transform;
        while (t != null)
        {
            var n = t.name;
            if (!string.IsNullOrEmpty(n))
            {
                if (n.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (n.IndexOf("bush", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (n.IndexOf("foliage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            t = t.parent;
        }

        return false;
    }

    static bool IsVehicleCollider(Collider collider)
    {
        var root = collider.transform.root;
        if (root == null)
            return false;

        if (root.name == "Vehicles" || root.name == "ActiveCityTraffic")
            return true;

        return root.GetComponentInChildren<TrafficRouteFollower>(true) != null ||
               root.GetComponentInChildren<CityTrafficVehicle>(true) != null ||
               root.GetComponentInChildren<PlayerDrivableVehicle>(true) != null;
    }
}
