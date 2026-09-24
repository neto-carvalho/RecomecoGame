using UnityEngine;

[DisallowMultipleComponent]
public class TrafficRoute : MonoBehaviour
{
    public Transform[] waypoints;
    public bool loop = true;
    [Tooltip("Trecho reto: carro vai e volta entre waypoints (útil em ruas EasyRoads).")]
    public bool pingPong;
    public float speedKmh = 28f;

    public int WaypointCount
    {
        get
        {
            EnsureReady();
            return waypoints != null ? waypoints.Length : 0;
        }
    }

    void Awake()
    {
        EnsureReady();
    }

    public void EnsureReady()
    {
        if (waypoints == null || waypoints.Length == 0 || HasNullWaypoint())
            CollectWaypointsFromChildren();
    }

    bool HasNullWaypoint()
    {
        if (waypoints == null)
            return true;

        foreach (var wp in waypoints)
        {
            if (wp == null)
                return true;
        }

        return false;
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
            return null;

        index = ((index % waypoints.Length) + waypoints.Length) % waypoints.Length;
        return waypoints[index];
    }

    public Vector3 GetGroundPosition(int index)
    {
        return GetWorldPoint(index);
    }

    public Vector3 GetWorldPoint(int index)
    {
        var wp = GetWaypoint(index);
        if (wp == null)
            return Vector3.zero;

        return wp.position;
    }

    public void RealignWaypointsToGround()
    {
        EnsureReady();
        if (waypoints == null)
            return;

        foreach (var wp in waypoints)
        {
            if (wp == null)
                continue;

            var pos = wp.position;
            pos.y = VehicleGroundSnap.SnapY(pos.x, pos.z, pos.y);
            wp.position = pos;
        }
    }

    public static void RealignAllRoutesInScene()
    {
        foreach (var route in Object.FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None))
        {
            if (route == null)
                continue;

            route.RealignWaypointsToGround();
        }
    }

    public void CollectWaypointsFromChildren()
    {
        var list = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
        {
            if (child == null)
                continue;
            if (child.name.StartsWith("Waypoint"))
                list.Add(child);
        }

        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        waypoints = list.ToArray();
    }

    void OnValidate()
    {
        if (waypoints == null || waypoints.Length == 0)
            CollectWaypointsFromChildren();
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
        for (var i = 0; i < waypoints.Length; i++)
        {
            var a = waypoints[i];
            if (a == null)
                continue;

            var b = waypoints[(i + 1) % waypoints.Length];
            if (b != null)
                Gizmos.DrawLine(a.position, b.position);

            Gizmos.DrawSphere(a.position, 0.35f);
        }
    }
}
