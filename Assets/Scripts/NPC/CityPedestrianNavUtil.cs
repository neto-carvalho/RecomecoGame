using UnityEngine;
using UnityEngine.AI;

public static class CityPedestrianNavUtil
{
    public static bool IsNavMeshReadyNear(Vector3 position, float maxVerticalSearch = 4f)
    {
        return NavMesh.SamplePosition(position, out _, maxVerticalSearch, NavMesh.AllAreas);
    }

    public static bool TryGetRandomPoint(Vector3 center, float radius, out Vector3 point)
    {
        for (var i = 0; i < 32; i++)
        {
            var sample = center + Random.insideUnitSphere * radius;
            sample.y = center.y;
            if (NavMesh.SamplePosition(sample, out var hit, radius * 0.4f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = center;
        return false;
    }

    public static Vector3 ResolveCityCenter()
    {
        var lojinha = GameObject.Find("Lojinha");
        if (lojinha != null)
            return lojinha.transform.position;

        var spawns = GameObject.Find("SidewalkNpcSpawns");
        if (spawns != null)
            return spawns.transform.position;

        return Vector3.zero;
    }
}
