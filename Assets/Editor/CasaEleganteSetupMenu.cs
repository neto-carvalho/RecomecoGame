#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CasaEleganteSetupMenu
{
    const string MenuRoot = "Recomeco/Casas/";
    const string CidadeScenePath = "Assets/Scenes/Cidade.unity";
    const string InteriorScenePath = "Assets/Scenes/Interior Casa elegante (player).unity";
    const string GameplaySettingsPath = "Assets/Resources/RecomecoGameplaySettings.asset";

    const string DoorObjectName = "Porta_CasaElegante";
    const string BedInteractName = "Interacao_Salvar_Cama";
    const string StorageInteractName = "Interacao_Armario";

    [MenuItem(MenuRoot + "Configurar Casa elegante (porta, interior, cama, armário)")]
    static void SetupCasaElegante()
    {
        if (!File.Exists(InteriorScenePath))
        {
            EditorUtility.DisplayDialog("Casa elegante",
                "Cena não encontrada:\n" + InteriorScenePath,
                "OK");
            return;
        }

        AddInteriorToBuildSettings();

        var report = new StringBuilder();
        if (!SetupCidadeDoor(report))
        {
            EditorUtility.DisplayDialog("Casa elegante",
                "Não encontrei '" + RecomecoSceneNames.CasaEleganteRootName + "' na cena Cidade.\n\n" +
                "Abra a Cidade, confirme que a casa está na hierarquia e rode o menu de novo.",
                "OK");
            return;
        }

        SetupInteriorScene(report);

        EditorUtility.DisplayDialog("Casa elegante", report.ToString(), "OK");
    }

    [MenuItem(MenuRoot + "Só configurar porta da Casa elegante (Cidade)")]
    static void SetupCidadeDoorOnly()
    {
        var report = new StringBuilder();
        if (!SetupCidadeDoor(report))
        {
            EditorUtility.DisplayDialog("Casa elegante",
                "Não encontrei '" + RecomecoSceneNames.CasaEleganteRootName + "' na cena Cidade.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog("Casa elegante", report.ToString(), "OK");
    }

    static bool SetupCidadeDoor(StringBuilder report)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != CidadeScenePath)
        {
            if (!File.Exists(CidadeScenePath))
                return false;

            scene = EditorSceneManager.OpenScene(CidadeScenePath, OpenSceneMode.Single);
        }

        var casa = FindObjectByName(RecomecoSceneNames.CasaEleganteRootName);
        if (casa == null)
            return false;

        var door = FindObjectByName(DoorObjectName);
        if (door == null)
        {
            door = new GameObject(DoorObjectName);
            Undo.RegisterCreatedObjectUndo(door, "Create Casa Door");
            door.transform.SetPositionAndRotation(
                casa.transform.position + casa.transform.forward * 2.5f + Vector3.up * 1f,
                casa.transform.rotation);
        }
        else
        {
            Undo.RecordObject(door.transform, "Move Casa Door");
        }

        var box = door.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(door);

        box.isTrigger = true;
        box.size = new Vector3(2.5f, 2.5f, 2f);
        box.center = Vector3.zero;

        var interact = door.GetComponent<HouseDoorInteract>();
        if (interact == null)
            interact = Undo.AddComponent<HouseDoorInteract>(door);

        interact.housingId = PlayerHousingState.CasaElegante;
        interact.purchasePriceCents = 15000;
        interact.houseDisplayName = "Casa elegante";
        interact.interiorSceneName = RecomecoSceneNames.InteriorCasaElegante;
        interact.interiorSpawnId = RecomecoSceneNames.EntradaCasaElegante;

        EnsureSpawnInScene(
            RecomecoSceneNames.SaidaCasaElegante,
            casa.transform.position + casa.transform.forward * 3f,
            casa.transform.rotation);

        EditorSceneManager.SaveScene(scene);
        report.AppendLine("Cidade salva:");
        report.AppendLine("• " + DoorObjectName + " (compra + entrar)");
        report.AppendLine("• Spawn_" + RecomecoSceneNames.SaidaCasaElegante);
        report.AppendLine();
        return true;
    }

    [MenuItem(MenuRoot + "Ajustar interior Casa elegante (escala 0.2 + colisão)")]
    static void FixInteriorScaleAndCollidersMenu()
    {
        if (!File.Exists(InteriorScenePath))
        {
            EditorUtility.DisplayDialog("Casa elegante", "Cena interior não encontrada.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Single);
        var report = new StringBuilder();
        ApplyInteriorScale(report);
        RepositionInteriorMarkers(report);
        BakeInteriorColliders(report);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Casa elegante", report.ToString(), "OK");
    }

    [MenuItem(MenuRoot + "Gerar colisão no interior (Casa elegante)")]
    static void BakeInteriorCollidersMenu()
    {
        if (!File.Exists(InteriorScenePath))
        {
            EditorUtility.DisplayDialog("Casa elegante", "Cena interior não encontrada.", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Single);
        var report = new StringBuilder();
        BakeInteriorColliders(report);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Casa elegante", report.ToString(), "OK");
    }

    static void ApplyInteriorScale(StringBuilder report)
    {
        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(GameplaySettingsPath);
        var scale = settings != null ? settings.playerScaleCity : 0.2f;

        foreach (var rootName in InteriorSceneLayout.ContentRootNames)
        {
            var root = FindObjectByName(rootName);
            if (root == null)
            {
                report.AppendLine("• AVISO: '" + rootName + "' não encontrado.");
                continue;
            }

            Undo.RecordObject(root.transform, "Scale Interior Root");
            root.transform.localScale = Vector3.one * scale;
            report.AppendLine("• Escala " + rootName + ": " + scale.ToString("0.##"));
        }
    }

    static void RepositionInteriorMarkers(StringBuilder report)
    {
        if (!InteriorSceneLayout.TryComputeMarkerPositions(out var spawnPos, out var exitPos))
        {
            report.AppendLine("• AVISO: não foi possível calcular bounds do interior.");
            return;
        }

        EnsureSpawnInScene(RecomecoSceneNames.EntradaCasaElegante, spawnPos, Quaternion.identity);
        ParentSpawnToEntryRoot();

        var exit = FindObjectByName("Portal_SaidaCasaElegante");
        if (exit == null)
        {
            exit = new GameObject("Portal_SaidaCasaElegante");
            Undo.RegisterCreatedObjectUndo(exit, "Create Exit Portal");
            Undo.AddComponent<SceneTransitionZone>(exit);
            Undo.AddComponent<BoxCollider>(exit);
        }

        Undo.RecordObject(exit.transform, "Move Exit Portal");
        exit.transform.position = exitPos;

        var exitBox = exit.GetComponent<BoxCollider>();
        if (exitBox != null)
        {
            exitBox.isTrigger = true;
            exitBox.size = new Vector3(1.2f, 2f, 1.2f);
            exitBox.center = Vector3.zero;
        }

        var zone = exit.GetComponent<SceneTransitionZone>();
        if (zone != null)
        {
            zone.targetSceneName = RecomecoSceneNames.Cidade;
            zone.targetSpawnId = RecomecoSceneNames.SaidaCasaElegante;
            zone.messageNear = "Aperte E para sair";
        }

        var bed = FindObjectByName("Bed");
        var wardrobe = FindObjectByName("Wardrobe") ?? FindObjectByName("Wardrobe_01");
        SetupBedInteractMarker(bed, report);
        SetupStorageInteractMarker(wardrobe, report);

        report.AppendLine("• Spawn e portal reposicionados no chão do interior.");
    }

    static void BakeInteriorColliders(StringBuilder report)
    {
        var total = 0;

        foreach (var rootName in InteriorSceneLayout.ContentRootNames)
        {
            var root = FindObjectByName(rootName);
            if (root == null)
            {
                report.AppendLine("• AVISO: '" + rootName + "' não encontrado para colisão.");
                continue;
            }

            var count = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null)
                    continue;
                if (ShouldSkipForBake(filter.gameObject))
                    continue;

                var go = filter.gameObject;
                var collider = go.GetComponent<MeshCollider>();
                if (collider == null)
                    collider = Undo.AddComponent<MeshCollider>(go);

                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                collider.isTrigger = false;
                collider.enabled = true;
                go.isStatic = true;
                count++;
            }

            total += count;
            InteriorSurfaceFilter.DisableSolidCollidersOnCeilings(root.transform);
            report.AppendLine("• Colisão em " + rootName + ": " + count + " MeshColliders");
        }

        if (total == 0)
            report.AppendLine("• AVISO: nenhum MeshCollider criado.");
    }

    static bool ShouldSkipForBake(GameObject go)
    {
        if (go == null || go.CompareTag("Player"))
            return true;

        if (go.GetComponent<MeshRenderer>() == null)
            return true;

        if (go.GetComponent<SceneSpawnPoint>() != null)
            return true;

        var name = go.name;
        if (name.StartsWith("Interacao_") || name.StartsWith("Portal_") || name.StartsWith("Spawn_"))
            return true;

        if (InteriorSurfaceFilter.IsCeilingOrRoof(go))
            return true;

        foreach (var col in go.GetComponents<Collider>())
        {
            if (col != null && col.enabled && !col.isTrigger)
                return true;
        }

        return false;
    }

    static void SetupInteriorScene(StringBuilder report)
    {
        var scene = EditorSceneManager.OpenScene(InteriorScenePath, OpenSceneMode.Single);

        ApplyInteriorScale(report);
        RepositionInteriorMarkers(report);
        BakeInteriorColliders(report);

        EditorSceneManager.SaveScene(scene);
        report.AppendLine("Interior salvo (Room + PACK1_demo).");
        report.AppendLine();
    }

    static void SetupBedInteractMarker(GameObject bed, StringBuilder report)
    {
        RemoveComponentFromObject<BedSaveInteract>(bed);

        var marker = FindObjectByName(BedInteractName);
        if (marker == null)
        {
            marker = new GameObject(BedInteractName);
            Undo.RegisterCreatedObjectUndo(marker, "Create Bed Interact");
        }

        if (bed != null)
        {
            marker.transform.position = bed.transform.position + Vector3.up * 0.5f;
            marker.transform.rotation = bed.transform.rotation;
        }

        var save = marker.GetComponent<BedSaveInteract>();
        if (save == null)
            save = Undo.AddComponent<BedSaveInteract>(marker);

        save.saveSpawnId = RecomecoSceneNames.EntradaCasaElegante;
        save.interactDistance = 2.5f;
    }

    static void SetupStorageInteractMarker(GameObject wardrobe, StringBuilder report)
    {
        RemoveComponentFromObject<HomeStorage>(wardrobe);
        RemoveComponentFromObject<StorageCabinetInteract>(wardrobe);

        var marker = FindObjectByName(StorageInteractName);
        if (marker == null)
        {
            marker = new GameObject(StorageInteractName);
            Undo.RegisterCreatedObjectUndo(marker, "Create Storage Interact");
        }

        if (wardrobe != null)
        {
            marker.transform.position = wardrobe.transform.position + wardrobe.transform.forward * 0.8f;
            marker.transform.rotation = wardrobe.transform.rotation;
        }

        var storage = marker.GetComponent<HomeStorage>();
        if (storage == null)
            storage = Undo.AddComponent<HomeStorage>(marker);

        storage.storageId = "casa_elegante_armario";
        storage.slotCount = 12;

        if (marker.GetComponent<StorageCabinetInteract>() == null)
            Undo.AddComponent<StorageCabinetInteract>(marker);
    }

    static void RemoveComponentFromObject<T>(GameObject target) where T : Component
    {
        if (target == null)
            return;

        var comp = target.GetComponent<T>();
        if (comp != null)
            Undo.DestroyObjectImmediate(comp);
    }

    static GameObject FindObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        var direct = GameObject.Find(objectName);
        if (direct != null)
            return direct;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindDeepInChildren(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    static Transform FindDeepInChildren(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        for (var i = 0; i < parent.childCount; i++)
        {
            var found = FindDeepInChildren(parent.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    static void EnsureSpawnInScene(string spawnId, Vector3 position, Quaternion rotation)
    {
        SceneSpawnPoint existing = null;
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.spawnId != spawnId)
                continue;
            existing = sp;
            break;
        }

        if (existing != null)
        {
            Undo.RecordObject(existing.transform, "Move Spawn");
            existing.transform.SetPositionAndRotation(position, rotation);
            if (spawnId == RecomecoSceneNames.EntradaCasaElegante)
                ParentSpawnTransform(existing.transform);
            return;
        }

        var go = new GameObject("Spawn_" + spawnId);
        Undo.RegisterCreatedObjectUndo(go, "Create Spawn");
        go.transform.SetPositionAndRotation(position, rotation);
        if (spawnId == RecomecoSceneNames.EntradaCasaElegante)
            ParentSpawnTransform(go.transform);
        var point = Undo.AddComponent<SceneSpawnPoint>(go);
        point.spawnId = spawnId;
    }

    static void ParentSpawnToEntryRoot()
    {
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp == null || sp.spawnId != RecomecoSceneNames.EntradaCasaElegante)
                continue;
            ParentSpawnTransform(sp.transform);
            return;
        }
    }

    static void ParentSpawnTransform(Transform spawn)
    {
        if (spawn == null)
            return;

        var entry = FindObjectByName("PACK1_demo") ?? FindObjectByName("Room");
        if (entry == null || spawn.parent == entry.transform)
            return;

        Undo.RecordObject(spawn, "Parent Interior Spawn");
        spawn.SetParent(entry.transform, true);
    }

    static void AddInteriorToBuildSettings()
    {
        var paths = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path))
                paths.Add(s.path);
        }

        if (!paths.Contains(InteriorScenePath))
            paths.Add(InteriorScenePath);

        if (!paths.Contains(CidadeScenePath))
            paths.Add(CidadeScenePath);

        var scenes = new EditorBuildSettingsScene[paths.Count];
        for (var i = 0; i < paths.Count; i++)
            scenes[i] = new EditorBuildSettingsScene(paths[i], true);

        EditorBuildSettings.scenes = scenes;
    }
}
#endif
