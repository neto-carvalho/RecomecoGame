#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AmbientAudioSetupMenu
{
    const string MenuRoot = "Recomeco/Áudio/";
    const string PackFolder = "Assets/Gregor Quendel - Free General Ambience Sounds";
    const string ProfilePath = "Assets/Audio/Ambient/AmbientAudioProfile.asset";

    [MenuItem(MenuRoot + "Configurar ambiente (Gregor Quendel)")]
    static void SetupAmbience()
    {
        EnsureFolder("Assets/Audio/Ambient");

        var profile = AssetDatabase.LoadAssetAtPath<AmbientAudioProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<AmbientAudioProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        profile.cityAmbience = LoadClip("City Ambience - Park - Spring");
        if (profile.cityAmbience == null)
            profile.cityAmbience = LoadClip("City Ambience - Traffic - Street - Cars and tram");

        profile.natureBirds = LoadClip("Nature - Birds");
        profile.natureWind = LoadClip("Wind - Autumn wind");
        profile.waterStream = LoadClip("Water Stream - III");
        if (profile.waterStream == null)
            profile.waterStream = LoadClip("Water Stream - I");

        profile.cityVolume = 0.22f;
        profile.natureVolume = 0.35f;
        profile.windVolume = 0.12f;
        profile.waterVolume = 0.28f;
        profile.blendSeconds = 2f;
        EditorUtility.SetDirty(profile);

        var controllerGo = GameObject.Find("AmbientAudio");
        if (controllerGo == null)
        {
            controllerGo = new GameObject("AmbientAudio");
            Undo.RegisterCreatedObjectUndo(controllerGo, "Create AmbientAudio");
        }

        var controller = controllerGo.GetComponent<AmbientAudioController>();
        if (controller == null)
            controller = Undo.AddComponent<AmbientAudioController>(controllerGo);

        var so = new SerializedObject(controller);
        so.FindProperty("profile").objectReferenceValue = profile;
        so.FindProperty("playOnStart").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        var zones = SetupNatureAndLakeZones();

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Ambiente",
            "Perfil: " + ProfilePath + "\n\n" +
            "• Cidade: Park Spring (fundo)\n" +
            "• Natureza: pássaros + vento\n" +
            "• Lago: Water Stream\n\n" +
            $"Zonas criadas/actualizadas: {zones}\n\n" +
            "Dá Play. Ajusta volumes no AmbientAudioProfile.\n" +
            "Clips longos: Import Settings → Streaming (recomendado).",
            "OK");

        Selection.activeGameObject = controllerGo;
    }

    static int SetupNatureAndLakeZones()
    {
        var count = 0;

        foreach (var terrain in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            var n = terrain.gameObject.name.ToLowerInvariant();
            if (!n.Contains("nature") && !n.Contains("mato") && !n.Contains("lago"))
                continue;

            if (EnsureZoneOnTerrain(terrain, AmbientZone.ZoneKind.Nature))
                count++;
        }

        var lake = GameObject.Find("Lake_Water");
        if (lake != null && EnsureLakeZone(lake.transform))
            count++;

        return count;
    }

    static bool EnsureZoneOnTerrain(Terrain terrain, AmbientZone.ZoneKind kind)
    {
        var data = terrain.terrainData;
        var size = data.size;

        var zoneTf = terrain.transform.Find("AmbientZone_Nature");
        if (zoneTf == null)
        {
            var created = new GameObject("AmbientZone_Nature");
            Undo.RegisterCreatedObjectUndo(created, "Nature ambient zone");
            created.transform.SetParent(terrain.transform, false);
            zoneTf = created.transform;
        }

        var box = zoneTf.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(zoneTf.gameObject);

        box.isTrigger = true;
        box.center = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        box.size = new Vector3(size.x, Mathf.Max(12f, size.y), size.z);

        var zone = zoneTf.GetComponent<AmbientZone>();
        if (zone == null)
            zone = Undo.AddComponent<AmbientZone>(zoneTf.gameObject);

        var zso = new SerializedObject(zone);
        zso.FindProperty("kind").enumValueIndex = (int)kind;
        zso.FindProperty("natureBlend").floatValue = 1f;
        zso.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static bool EnsureLakeZone(Transform lake)
    {
        var root = lake.parent != null ? lake.parent : lake;
        var zoneGo = root.Find("AmbientZone_Lake");
        if (zoneGo == null)
        {
            var created = new GameObject("AmbientZone_Lake");
            Undo.RegisterCreatedObjectUndo(created, "Lake ambient zone");
            created.transform.SetParent(root, false);
            created.transform.position = lake.position;
            zoneGo = created.transform;
        }

        var sphere = zoneGo.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = Undo.AddComponent<SphereCollider>(zoneGo.gameObject);

        sphere.isTrigger = true;
        sphere.radius = 28f;
        sphere.center = Vector3.zero;

        var zone = zoneGo.GetComponent<AmbientZone>();
        if (zone == null)
            zone = Undo.AddComponent<AmbientZone>(zoneGo.gameObject);

        var zso = new SerializedObject(zone);
        zso.FindProperty("kind").enumValueIndex = (int)AmbientZone.ZoneKind.Lake;
        zso.FindProperty("waterBlend").floatValue = 1f;
        zso.FindProperty("natureBlend").floatValue = 0.5f;
        zso.ApplyModifiedPropertiesWithoutUndo();

        zoneGo.position = lake.position;
        return true;
    }

    static AudioClip LoadClip(string namePart)
    {
        if (!AssetDatabase.IsValidFolder(PackFolder))
            return null;

        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { PackFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains(namePart))
                continue;

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        return null;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
