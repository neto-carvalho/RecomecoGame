#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityStreetLightsSetupMenu
{
    const string MenuRoot = "Recomeco/Cidade/";
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
