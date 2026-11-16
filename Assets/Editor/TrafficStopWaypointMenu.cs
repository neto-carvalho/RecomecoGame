#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TrafficStopWaypointMenu
{
    const string MenuPath = "Recomeco/Cidade/Paradas automáticas em cruzamentos";
    const string ClearPath = "Recomeco/Cidade/Remover paradas dos waypoints (só curva suave)";

    [MenuItem(MenuPath)]
    static void MarkIntersectionStops()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Tráfego", "Abra a cena Cidade.", "OK");
            return;
        }

        var routes = Object.FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None);
        var marked = 0;
        const float minAngle = 34f;

        foreach (var route in routes)
        {
            if (route == null)
                continue;

            route.EnsureReady();
            var count = route.WaypointCount;
            if (count < 3)
                continue;

            for (var i = 0; i < count; i++)
            {
                var prev = route.GetWorldPoint((i - 1 + count) % count);
                var here = route.GetWorldPoint(i);
                var next = route.GetWorldPoint((i + 1) % count);

                var a = Flat(here - prev);
                var b = Flat(next - here);
                if (a.sqrMagnitude < 0.04f || b.sqrMagnitude < 0.04f)
                    continue;

                a.Normalize();
                b.Normalize();
                if (Vector3.Angle(a, b) < minAngle)
                    continue;

                var wp = route.GetWaypoint(i);
                if (wp == null)
                    continue;

                var meta = wp.GetComponent<TrafficRouteWaypoint>();
                if (meta == null)
                    meta = Undo.AddComponent<TrafficRouteWaypoint>(wp.gameObject);

                meta.requireFullStop = true;
                meta.stopSeconds = UnityEngine.Random.Range(0.75f, 1.35f);
                meta.yieldToCrossTraffic = true;
                marked++;
            }
        }

        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("Tráfego",
            marked > 0
                ? marked + " waypoint(s) com parada em cruzamento.\nSalve a cena (Ctrl+S) e teste em Play."
                : "Nenhum cruzamento detectado (ângulo ≥ " + minAngle + "°). Ajuste as rotas em TrafficRoutes.",
            "OK");
    }

    [MenuItem(ClearPath)]
    static void ClearIntersectionStops()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Tráfego", "Abra a cena Cidade.", "OK");
            return;
        }

        var removed = 0;
        foreach (var meta in Object.FindObjectsByType<TrafficRouteWaypoint>(FindObjectsSortMode.None))
        {
            if (meta == null)
                continue;

            Undo.DestroyObjectImmediate(meta);
            removed++;
        }

        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("Tráfego",
            removed > 0
                ? removed + " parada(s) removidas. Curvas só desaceleram ao dar Play.\nSalve a cena (Ctrl+S)."
                : "Nenhum TrafficRouteWaypoint na cena.",
            "OK");
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
#endif
