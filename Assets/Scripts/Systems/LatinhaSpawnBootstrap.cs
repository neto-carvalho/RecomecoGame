using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante SpawnManager em cenas de gameplay quando não existe na Hierarchy.
/// </summary>
public static class LatinhaSpawnBootstrap
{
    static bool _hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        if (_hooked)
            return;
        _hooked = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryEnsure(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryEnsure(scene);
    }

    static void TryEnsure(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
            return;

        if (RecomecoSceneNames.IsMenuScene(scene))
            return;

        if (Object.FindFirstObjectByType<SpawnManager>() != null)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        var latinha = settings != null ? settings.latinhaPrefab : null;
        if (latinha == null)
            return;

        var center = Vector3.zero;
        var spawnPoint = GameObject.Find("Spawn_EntradaCidade");
        if (spawnPoint != null)
            center = spawnPoint.transform.position;
        else
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                center = player.transform.position;
        }

        var go = new GameObject("SpawnManager");
        go.transform.position = center;

        var manager = go.AddComponent<SpawnManager>();
        manager.latinhaPrefab = latinha;
        manager.heightAboveGround = 0.015f;
    }
}
