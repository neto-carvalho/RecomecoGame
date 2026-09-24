#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityLivingSetupMenu
{
    const string MenuRoot = "Recomeco/Cidade/";

    [MenuItem(MenuRoot + "Configurar cidade viva (tráfego + rotas + NPCs)")]
    static void SetupLivingCity()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Cidade viva", "Abra a cena Cidade antes de usar este menu.", "OK");
            return;
        }

        var vehicles = GameObject.Find("Vehicles");
        if (vehicles != null)
        {
            vehicles.isStatic = false;
            var snapped = 0;
            foreach (Transform child in vehicles.transform)
            {
                VehicleGroundSnap.SnapTransform(child);
                snapped++;
            }

            Debug.Log("[Cidade viva] " + snapped + " veículos alinhados ao chão.");
        }

        EnsureTrafficRoutes();
        TrafficRoute.RealignAllRoutesInScene();
        FixVehiclesRootInScene();
        CartoonLowPolyCityNpcSpawnMenu.CreateSpawnMarkersInternal();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(
            "Cidade viva",
            "Configuração aplicada:\n" +
            "• Veículos alinhados ao chão (collider + tráfego no Play)\n" +
            "• Rotas de tráfego (TrafficRoutes)\n" +
            "• Marcadores de NPC na calçada\n\n" +
            "Salve a cena (Ctrl+S) e dê Play.\n\n" +
            "Tráfego em movimento: pasta ActiveCityTraffic (spawn automático nas rotas).\n" +
            "Ajustes: Recomeco → Abrir Gameplay Settings",
            "OK");
    }

    [MenuItem(MenuRoot + "Gerar rotas em rede (loops pela cidade) — recomendado")]
    static void GenerateRoutesFromRoads()
    {
        CityTrafficRouteGeneratorMenu.GenerateNetworkLoopsFromRoads();
    }

    [MenuItem(MenuRoot + "Recriar rotas de tráfego padrão (perto da Lojinha)")]
    static void RecreateDefaultRoutes()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Cidade", "Abra a cena Cidade.", "OK");
            return;
        }

        var old = GameObject.Find("TrafficRoutes");
        if (old != null)
            Undo.DestroyObjectImmediate(old);

        EnsureTrafficRoutes();
        TrafficRoute.RealignAllRoutesInScene();
        FixVehiclesRootInScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Cidade",
            "Rotas recriadas. Ajuste os waypoints (filhos) para o centro das ruas se precisar.\nSalve a cena (Ctrl+S).",
            "OK");
    }

    [MenuItem(MenuRoot + "Copiar prefabs de tráfego para Resources (build)")]
    static void CopyTrafficPrefabsToResources()
    {
        const string sourceFolder = "Assets/Flat_Style_Vehicles/Prefabs/Vehicles_no_interior";
        const string destFolder = "Assets/Resources/CityTraffic";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(destFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "CityTraffic");

        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { sourceFolder });
        var copied = 0;
        foreach (var guid in guids)
        {
            if (copied >= 12)
                break;

            var src = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = System.IO.Path.GetFileName(src);
            var dst = destFolder + "/" + fileName;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null)
            {
                copied++;
                continue;
            }

            if (AssetDatabase.CopyAsset(src, dst))
                copied++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Tráfego",
            copied + " prefab(s) em Assets/Resources/CityTraffic.\n" +
            "No Editor o Play já usa a pasta no_interior; isto é para builds.",
            "OK");
    }

    [MenuItem(MenuRoot + "Alinhar rotas e veículos ao chão")]
    static void RealignRoutesAndVehicles()
    {
        TrafficRoute.RealignAllRoutesInScene();
        FixVehiclesRootInScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Cidade", "Rotas e veículos realinhados ao chão.\nSalve a cena (Ctrl+S).", "OK");
    }

    [MenuItem(MenuRoot + "Remover rotas de tráfego fora da zona urbana")]
    static void RemoveOutOfZoneTrafficRoutes()
    {
        var removed = 0;
        foreach (var route in Object.FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None))
        {
            if (route == null)
                continue;

            if (CityTrafficZone.IsPlausibleRoute(route))
                continue;

            Undo.DestroyObjectImmediate(route.gameObject);
            removed++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Cidade",
            removed + " rota(s) removida(s) fora da zona (perto da Lojinha/Entrada).\nSalve a cena (Ctrl+S).",
            "OK");
    }

    static void FixVehiclesRootInScene()
    {
        var vehicles = GameObject.Find("Vehicles");
        if (vehicles == null)
            return;

        vehicles.isStatic = false;
        var lp = vehicles.transform.localPosition;
        if (Mathf.Abs(lp.y) > 0.05f)
        {
            var saved = new System.Collections.Generic.List<(Transform t, Vector3 p)>();
            foreach (Transform child in vehicles.transform)
                saved.Add((child, child.position));

            lp.y = 0f;
            vehicles.transform.localPosition = lp;

            foreach (var (t, p) in saved)
                t.position = p;
        }

        foreach (Transform child in vehicles.transform)
            VehicleGroundSnap.SnapTransform(child);
    }

    [MenuItem("Recomeco/Abrir Gameplay Settings")]
    static void OpenGameplaySettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(
            "Assets/Resources/RecomecoGameplaySettings.asset");
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Settings", "Arquivo não encontrado em Assets/Resources/RecomecoGameplaySettings.asset", "OK");
            return;
        }

        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    static void EnsureTrafficRoutes()
    {
        if (Object.FindFirstObjectByType<TrafficRoute>() != null)
            return;

        var root = GameObject.Find("TrafficRoutes");
        if (root == null)
        {
            root = new GameObject("TrafficRoutes");
            Undo.RegisterCreatedObjectUndo(root, "Create TrafficRoutes");
        }

        var anchor = GameObject.Find("Lojinha");
        var center = anchor != null ? anchor.transform.position : new Vector3(-8f, 0f, -24f);

        CreateRouteEditor(root.transform, "Rota_NS_Lojinha", center + new Vector3(-6f, 0f, -18f), new[]
        {
            Vector3.zero,
            new Vector3(0f, 0f, 8f),
            new Vector3(0f, 0f, 16f),
            new Vector3(0f, 0f, 24f),
            new Vector3(0f, 0f, 32f),
        });

        CreateRouteEditor(root.transform, "Rota_EW_Lojinha", center + new Vector3(-24f, 0f, -26f), new[]
        {
            Vector3.zero,
            new Vector3(8f, 0f, 0f),
            new Vector3(16f, 0f, 0f),
            new Vector3(24f, 0f, 0f),
            new Vector3(32f, 0f, 0f),
        });
    }

    static void CreateRouteEditor(Transform parent, string routeName, Vector3 startWorld, Vector3[] offsets)
    {
        var routeGo = new GameObject(routeName);
        Undo.RegisterCreatedObjectUndo(routeGo, "Create traffic route");
        routeGo.transform.SetParent(parent, false);

        var route = routeGo.AddComponent<TrafficRoute>();
        route.loop = true;
        route.speedKmh = 28f;

        var list = new Transform[offsets.Length];
        for (var i = 0; i < offsets.Length; i++)
        {
            var wp = new GameObject("Waypoint_" + i.ToString("00"));
            Undo.RegisterCreatedObjectUndo(wp, "Create waypoint");
            wp.transform.SetParent(routeGo.transform, true);
            var flat = startWorld + offsets[i];
            wp.transform.position = VehicleGroundSnap.Snap(new Vector3(flat.x, 0f, flat.z));
            list[i] = wp.transform;
        }

        route.waypoints = list;
    }
}
#endif
