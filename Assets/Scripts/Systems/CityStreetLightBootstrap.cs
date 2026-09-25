using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityStreetLightBootstrap
{
    static bool _registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        if (_registered)
            return;

        _registered = true;
        SceneManager.sceneLoaded += (_, __) => TryEnsure();
        TryEnsure();
    }

    public static void TryEnsurePublic() => TryEnsure();

    static void TryEnsure()
    {
        if (!Application.isPlaying)
            return;

        var scene = SceneManager.GetActiveScene();
        if (scene.name != RecomecoSceneNames.Cidade)
            return;

        DiscoverExistingPosts();
        CityStreetLightManager.EnsureInCityScene();
    }

    static void DiscoverExistingPosts()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        foreach (var root in activeScene.GetRootGameObjects())
            DiscoverRecursive(root.transform);
    }

    static void DiscoverRecursive(Transform node)
    {
        if (node == null)
            return;

        if (IsStreetPostName(node.name) && node.GetComponent<CityStreetLight>() == null)
            node.gameObject.AddComponent<CityStreetLight>();

        for (var i = 0; i < node.childCount; i++)
            DiscoverRecursive(node.GetChild(i));
    }

    static bool IsStreetPostName(string objectName)
    {
        return CityStreetLightNaming.IsStreetPole(objectName);
    }
}
