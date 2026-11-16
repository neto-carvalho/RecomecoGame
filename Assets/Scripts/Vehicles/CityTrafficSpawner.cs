using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityTrafficSpawner
{
    const string ActiveRootName = "ActiveCityTraffic";

    public static int SpawnOnRoutes(TrafficRoute[] routes, int maxTotal)
    {
        if (routes == null || routes.Length == 0 || maxTotal <= 0)
            return 0;

        var prefabs = CityTrafficPrefabCatalog.LoadSpawnPrefabs();
        if (prefabs.Count == 0)
        {
            Debug.LogError(
                "[Tráfego] Nenhum prefab em Vehicles_no_interior. " +
                "Use Recomeco → Cidade → Copiar prefabs de tráfego para Resources.");
            return 0;
        }

        ClearActivePool();
        var root = EnsureActiveRoot();

        var settings = RecomecoGameplaySettings.Instance;
        var perRouteCap = settings != null ? Mathf.Clamp(settings.trafficMaxCarsPerRoute, 1, 3) : 1;

        var validRoutes = 0;
        foreach (var route in routes)
        {
            if (route == null)
                continue;
            route.EnsureReady();
            if (route.WaypointCount >= 2 && CityTrafficZone.IsPlausibleRoute(route))
                validRoutes++;
        }

        if (validRoutes == 0)
            return 0;

        var perRoute = Mathf.Clamp(Mathf.CeilToInt(maxTotal / (float)validRoutes), 1, perRouteCap);
        var spawned = 0;

        foreach (var route in routes)
        {
            if (route == null)
                continue;

            route.EnsureReady();
            if (route.WaypointCount < 2 || !CityTrafficZone.IsPlausibleRoute(route))
                continue;

            for (var i = 0; i < perRoute && spawned < maxTotal; i++)
            {
                var prefab = prefabs[Random.Range(0, prefabs.Count)];
                var instance = Object.Instantiate(prefab, root);
                instance.name = "Traffic_" + route.name + "_" + (i + 1);

                VehicleColliderUtility.PrepareTrafficObject(instance);

                var follower = instance.GetComponent<TrafficRouteFollower>();
                if (follower == null)
                    follower = instance.AddComponent<TrafficRouteFollower>();

                var routeFraction = (i + 1f) / (perRoute + 1f);
                TrafficSpawnPlacement.PlaceOnRoute(route, routeFraction, out var startWp, out var along);
                follower.BeginRoute(route, startWp, along);
                VehicleGroundSnap.SnapTrafficWheelsToGround(instance.transform);

                if (!follower.IsDriving || instance.GetComponent<BoxCollider>() == null)
                {
                    Debug.LogWarning(
                        "[Tráfego] Removido spawn inválido em «" + route.name + "» (rota ou collider).",
                        instance);
                    Object.Destroy(instance);
                    continue;
                }

                spawned++;
            }
        }

        Debug.Log("[Tráfego] Spawn: " + spawned + " carro(s) visíveis nas rotas (prefabs no_interior).");
        return spawned;
    }

    public static void ClearActivePool()
    {
        var root = GameObject.Find(ActiveRootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (child != null)
                Object.Destroy(child.gameObject);
        }
    }

    static Transform EnsureActiveRoot()
    {
        var existing = GameObject.Find(ActiveRootName);
        if (existing != null)
            return existing.transform;

        var go = new GameObject(ActiveRootName);
        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
        return go.transform;
    }
}
