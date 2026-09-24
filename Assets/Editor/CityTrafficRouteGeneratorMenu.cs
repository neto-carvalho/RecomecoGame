#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityTrafficRouteGeneratorMenu
{
    const string MenuRoot = "Recomeco/Cidade/";

    [MenuItem(MenuRoot + "Suavizar cantos da rota selecionada (evita atravessar prédios)")]
    static void FilletSelectedRouteCorners()
    {
        var route = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<TrafficRoute>()
            : null;
        if (route == null)
        {
            EditorUtility.DisplayDialog("Tráfego",
                "Selecione um objeto com TrafficRoute na Hierarchy.",
                "OK");
            return;
        }

        route.EnsureReady();
        if (route.waypoints == null || route.waypoints.Length < 3)
            return;

        var path = new List<Vector3>(route.waypoints.Length);
        foreach (var wp in route.waypoints)
            path.Add(wp != null ? wp.position : Vector3.zero);

        var expanded = CityTrafficRouteGenerator.ExpandPathWithCornerInsets(
            path, route.loop, insetMeters: 4.5f, minTurnAngle: 35f);
        if (expanded.Count < 2 || expanded.Count == path.Count)
        {
            EditorUtility.DisplayDialog("Tráfego",
                "Nenhum canto agudo encontrado para suavizar (ou a rota já está ok).\n" +
                "Cantos de 90° no centro do cruzamento costumam precisar disto.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(route.gameObject, "Suavizar cantos da rota");
        foreach (var wp in route.waypoints)
        {
            if (wp != null)
                Undo.DestroyObjectImmediate(wp.gameObject);
        }

        var waypoints = new Transform[expanded.Count];
        for (var i = 0; i < expanded.Count; i++)
        {
            var wp = new GameObject("Waypoint_" + i.ToString("00"));
            Undo.RegisterCreatedObjectUndo(wp, "Create waypoint");
            wp.transform.SetParent(route.transform, false);
            var p = expanded[i];
            p.y = VehicleGroundSnap.SnapYNearReference(p.x, p.z, p.y);
            wp.transform.position = p;
            waypoints[i] = wp.transform;
        }

        route.waypoints = waypoints;
        EditorUtility.SetDirty(route);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Tráfego",
            "Cantos em L (recuo + cruzamento + saída): " + path.Count + " → " + expanded.Count + " waypoints.\n" +
            "Se antes usou a versão antiga (só 2 pontos), rode de novo ou desfaça (Ctrl+Z).\nSalve (Ctrl+S).",
            "OK");
    }

    [MenuItem(MenuRoot + "Suavizar cantos em TODAS as rotas da cena")]
    static void FilletAllRouteCorners()
    {
        var routes = Object.FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None);
        var changed = 0;
        foreach (var route in routes)
        {
            if (route == null || route.waypoints == null || route.waypoints.Length < 3)
                continue;

            var path = new List<Vector3>(route.waypoints.Length);
            foreach (var wp in route.waypoints)
                path.Add(wp != null ? wp.position : Vector3.zero);

            var expanded = CityTrafficRouteGenerator.ExpandPathWithCornerInsets(
                path, route.loop, 4.5f, 35f);
            if (expanded.Count <= path.Count)
                continue;

            Undo.RegisterFullObjectHierarchyUndo(route.gameObject, "Suavizar cantos");
            foreach (var wp in route.waypoints)
            {
                if (wp != null)
                    Undo.DestroyObjectImmediate(wp.gameObject);
            }

            var waypoints = new Transform[expanded.Count];
            for (var i = 0; i < expanded.Count; i++)
            {
                var wp = new GameObject("Waypoint_" + i.ToString("00"));
                Undo.RegisterCreatedObjectUndo(wp, "Create waypoint");
                wp.transform.SetParent(route.transform, false);
                var p = expanded[i];
                p.y = VehicleGroundSnap.SnapYNearReference(p.x, p.z, p.y);
                wp.transform.position = p;
                waypoints[i] = wp.transform;
            }

            route.waypoints = waypoints;
            EditorUtility.SetDirty(route);
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Tráfego",
            changed > 0
                ? "Rotas atualizadas: " + changed + ".\nSalve a cena (Ctrl+S)."
                : "Nenhuma rota precisou de cantos extras.",
            "OK");
    }

    [MenuItem(MenuRoot + "Ordenar waypoints da rota selecionada (cruzamentos em sequência)")]
    static void OrderSelectedRouteWaypoints()
    {
        var route = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<TrafficRoute>()
            : null;
        if (route == null)
        {
            EditorUtility.DisplayDialog("Tráfego",
                "Selecione um objeto com TrafficRoute na Hierarchy (ex.: Rota_Rede_Norte_01).",
                "OK");
            return;
        }

        route.EnsureReady();
        if (route.waypoints == null || route.waypoints.Length < 2)
            return;

        var list = new List<Transform>(route.waypoints);
        CityTrafficRouteGenerator.OrderTransformsAsContinuousPath(list);
        route.waypoints = list.ToArray();
        EditorUtility.SetDirty(route);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Tráfego",
            "Waypoints reordenados do 00 ao fim, seguindo o caminho mais curto entre pontos.\n" +
            "Ajuste posições finas nos cruzamentos se precisar.",
            "OK");
    }

    [MenuItem(MenuRoot + "Gerar rotas em rede (loops pela cidade) — recomendado")]
    public static void GenerateNetworkLoopsFromRoads()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Tráfego", "Abra a cena Cidade.", "OK");
            return;
        }

        RemoveGeneratedRoutesOnly();

        var roads = CollectRoadSegments();
        if (roads.Count == 0)
        {
            EditorUtility.DisplayDialog("Tráfego",
                "Nenhum trecho em \"Road Objects\" (EasyRoads).\nVeja Ruas → rua1 → Road Objects.",
                "OK");
            return;
        }

        var loops = CityTrafficRoadNetwork.BuildRegionalLoopPaths(roads, out var skipped);
        if (loops.Count == 0)
        {
            EditorUtility.DisplayDialog("Tráfego",
                "Não foi possível montar loops.\nTrechos ignorados: " + skipped + "\n" +
                "Confira Road Objects e se as ruas estão dentro da área urbana.",
                "OK");
            return;
        }

        var routesRoot = EnsureTrafficRoutesRoot();
        var created = 0;
        for (var i = 0; i < loops.Count; i++)
        {
            var path = loops[i].Points;
            var closed = IsLoopClosed(path);
            var route = CityTrafficRouteGenerator.CreateRouteFromPoints(
                routesRoot,
                CityTrafficRouteGenerator.NetworkRoutePrefix + loops[i].Label,
                path,
                loop: closed,
                pingPong: false);

            if (route == null)
                continue;

            Undo.RegisterCreatedObjectUndo(route.gameObject, "Create network traffic route");
            created++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(
            "Tráfego",
            created + " circuito(s) regional(is) (N/S/L/O + extras).\n" +
            skipped + " trecho(s) ignorado(s).\n\n" +
            "Não arraste a rota inteira na Hierarchy — os carros seguem a ORDEM dos Waypoint_00, 01, 02…\n" +
            "Cruzamentos: o gerador liga trechos do EasyRoads automaticamente.\n" +
            "Salve (Ctrl+S) e dê Play.",
            "OK");
    }

    [MenuItem(MenuRoot + "Gerar rotas por trecho (vai e volta — legado)")]
    public static void GenerateFromRoadObjects()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Tráfego", "Abra a cena Cidade.", "OK");
            return;
        }

        RemoveGeneratedRoutesOnly();

        var roads = CollectRoadSegments();
        if (roads.Count == 0)
        {
            EditorUtility.DisplayDialog("Tráfego", "Nenhum trecho em Road Objects.", "OK");
            return;
        }

        var routesRoot = EnsureTrafficRoutesRoot();
        var created = 0;
        var skipped = 0;

        foreach (var road in roads)
        {
            if (!CityTrafficRouteGenerator.TryGetWorldBounds(road, out var bounds))
            {
                skipped++;
                continue;
            }

            if (!CityTrafficZone.ContainsHorizontal(bounds.center))
            {
                skipped++;
                continue;
            }

            var points = CityTrafficRouteGenerator.BuildCenterline(road, bounds);
            if (points == null || points.Count < 2)
            {
                skipped++;
                continue;
            }

            var start = points[0];
            var end = points[points.Count - 1];
            var closed = Vector3.Distance(
                new Vector3(start.x, 0f, start.z),
                new Vector3(end.x, 0f, end.z)) < 8f;

            var route = CityTrafficRouteGenerator.CreateRouteFromPoints(
                routesRoot,
                CityTrafficRouteGenerator.AutoRoutePrefix + SanitizeName(road.name),
                points,
                loop: closed,
                pingPong: !closed);

            if (route == null)
            {
                skipped++;
                continue;
            }

            Undo.RegisterCreatedObjectUndo(route.gameObject, "Create auto traffic route");
            created++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Tráfego", created + " rota(s) por trecho (legado).", "OK");
    }

    [MenuItem(MenuRoot + "Remover rotas geradas (Rota_Auto_* e Rota_Rede_*)")]
    static void RemoveGeneratedRoutesMenu()
    {
        var n = RemoveGeneratedRoutesOnly();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Tráfego", n + " rota(s) gerada(s) removida(s).", "OK");
    }

    public static int RemoveGeneratedRoutesOnly()
    {
        var removed = 0;
        foreach (var route in Object.FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None))
        {
            if (route == null || !CityTrafficRouteGenerator.IsGeneratedRouteName(route.name))
                continue;

            Undo.DestroyObjectImmediate(route.gameObject);
            removed++;
        }

        return removed;
    }

    static List<Transform> CollectRoadSegments()
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

            return list;
        }

        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r == null)
                continue;

            var n = r.gameObject.name;
            if (n.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (r.transform.parent != null &&
                r.transform.parent.name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                continue;
            }

            list.Add(r.transform);
        }

        return list;
    }

    static Transform EnsureTrafficRoutesRoot()
    {
        var root = GameObject.Find("TrafficRoutes");
        if (root != null)
            return root.transform;

        root = new GameObject("TrafficRoutes");
        Undo.RegisterCreatedObjectUndo(root, "Create TrafficRoutes");
        return root.transform;
    }

    static bool IsLoopClosed(List<Vector3> path)
    {
        if (path == null || path.Count < 3)
            return false;

        var a = path[0];
        var b = path[path.Count - 1];
        var dx = a.x - b.x;
        var dz = a.z - b.z;
        return dx * dx + dz * dz <= 28f * 28f;
    }

    static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Trecho";

        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name.Replace(' ', '_');
    }
}
#endif
