#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityStreetLightsSetupMenu
{
    const string MenuRoot = "Recomeco/Cidade/";
    const string LightPrefabPath = "Assets/CartoonLowPolyCityLite/Prefabs/Light_01.prefab";

    static readonly Vector2[] DefaultOffsets =
    {
        new(-28f, -34f), new(-20f, -34f), new(-12f, -34f), new(-4f, -34f), new(4f, -34f), new(12f, -34f),
        new(-28f, -26f), new(-12f, -26f), new(4f, -26f), new(20f, -26f),
        new(-24f, -18f), new(-8f, -18f), new(8f, -18f), new(24f, -18f),
        new(-20f, -10f), new(0f, -10f), new(20f, -10f),
        new(-16f, -2f), new(16f, -2f),
    };

    [MenuItem(MenuRoot + "Colocar postes com luz (ruas perto da Lojinha)")]
    static void PlaceStreetLightsNearLojinha()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Postes", "Abra a cena Cidade antes de usar este menu.", "OK");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Postes",
                "Prefab não encontrado:\n" + LightPrefabPath,
                "OK");
            return;
        }

        var root = GameObject.Find("CityStreetLights");
        if (root == null)
        {
            root = new GameObject("CityStreetLights");
            Undo.RegisterCreatedObjectUndo(root, "Create CityStreetLights");
        }

        var anchor = GameObject.Find("Lojinha");
        var center = anchor != null ? anchor.transform.position : new Vector3(-8f, 0f, -24f);
        center.y = 0f;

        var created = 0;
        foreach (var offset in DefaultOffsets)
        {
            var pos = center + new Vector3(offset.x, 0f, offset.y);
            pos = VehicleGroundSnap.Snap(pos);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Place street light");
            instance.transform.SetParent(root.transform, true);
            instance.transform.position = pos;
            instance.name = "Poste_Luz_" + created.ToString("00");

            if (instance.GetComponent<CityStreetLight>() == null)
                Undo.AddComponent<CityStreetLight>(instance);

            created++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Postes",
            created + " postes colocados em \"CityStreetLights\".\n\n" +
            "Salve a cena (Ctrl+S) e dê Play à noite — as luzes acendem com o ciclo dia/noite.\n\n" +
            "Ajustes: Recomeco → Abrir Gameplay Settings → Iluminação urbana.",
            "OK");
    }

    [MenuItem(MenuRoot + "Adicionar luz noturna nos postes Pole da cena")]
    static void TagExistingPosts()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
        {
            EditorUtility.DisplayDialog("Postes", "Abra a cena Cidade.", "OK");
            return;
        }

        var count = 0;
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (transform == null)
                continue;

            if (!CityStreetLightNaming.IsStreetPole(transform.name))
                continue;

            if (transform.GetComponent<CityStreetLight>() != null)
                continue;

            Undo.AddComponent<CityStreetLight>(transform.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog(
            "Postes",
            count + " poste(s) Pole marcados com CityStreetLight.\n\n" +
            "Salve a cena (Ctrl+S). No Play, as luzes acendem à noite automaticamente.",
            "OK");
    }
}
#endif
