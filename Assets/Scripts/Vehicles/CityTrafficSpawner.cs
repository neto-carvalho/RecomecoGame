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
        var perRoute = Mathf.Clamp(Mathf.CeilToInt(maxTotal / (float)routes.Length), 2, 4);
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

                var follower = instance.GetComponent<TrafficRouteFollower>();
                if (follower == null)
                    follower = instance.AddComponent<TrafficRouteFollower>();

                var startWp = Random.Range(0, route.WaypointCount);
                var along = (i + 1f) / (perRoute + 1f);
                follower.BeginRoute(route, startWp, along);
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
