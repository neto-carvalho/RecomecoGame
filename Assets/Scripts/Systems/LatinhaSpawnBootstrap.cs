using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        CleanupLatinhasOutsideCity(SceneManager.GetActiveScene());
        ScheduleEnsure(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanupLatinhasOutsideCity(scene);
        ScheduleEnsure(scene);
    }

    public static void EnsureInActiveScene()
    {
        EnsureInScene(SceneManager.GetActiveScene(), forceRecreate: true);
    }

    static void ScheduleEnsure(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
            return;

        if (RecomecoSceneNames.IsMenuScene(scene) || !RecomecoSceneNames.AllowsLatinhaSpawn(scene))
            return;

        var host = new GameObject(nameof(LatinhaSpawnBootstrapRunner));
        host.AddComponent<LatinhaSpawnBootstrapRunner>().Begin(scene);
    }

    static void EnsureInScene(Scene scene, bool forceRecreate = false)
    {
        if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded)
            return;

        if (RecomecoSceneNames.IsMenuScene(scene) || !RecomecoSceneNames.AllowsLatinhaSpawn(scene))
            return;

        var existing = Object.FindFirstObjectByType<SpawnManager>();
        if (existing != null)
        {
            if (!forceRecreate)
                return;

            Object.Destroy(existing.gameObject);
        }

        var settings = RecomecoGameplaySettings.Instance;
        var latinha = settings != null ? settings.latinhaPrefab : null;
        if (latinha == null)
        {
            Debug.LogWarning(
                "LatinhaSpawnBootstrap: latinhaPrefab não configurado em RecomecoGameplaySettings — " +
                "latinhas não serão spawnadas na Cidade.");
            return;
        }

        var go = new GameObject("SpawnManager");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = ResolveSpawnCenter(scene);

        var manager = go.AddComponent<SpawnManager>();
        manager.latinhaPrefab = latinha;
        manager.heightAboveGround = 0.015f;
    }

    static Vector3 ResolveSpawnCenter(Scene scene)
    {
        SceneSpawnPoint matched = null;
        foreach (var spawnPoint in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (spawnPoint == null || spawnPoint.gameObject.scene != scene)
                continue;
            if (spawnPoint.spawnId == "EntradaCidade")
            {
                matched = spawnPoint;
                break;
            }
        }

        if (matched != null)
            return matched.transform.position + matched.positionOffset;

        var spawnGo = GameObject.Find("Spawn_EntradaCidade");
        if (spawnGo != null)
            return spawnGo.transform.position;

        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player.transform.position;

        return Vector3.zero;
    }

    static void CleanupLatinhasOutsideCity(Scene scene)
    {
        if (RecomecoSceneNames.AllowsLatinhaSpawn(scene))
            return;

        foreach (var pickup in Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
        {
            if (pickup == null || pickup.item == null)
                continue;
            if (pickup.item.itemName != "Latinha")
                continue;

            Object.Destroy(pickup.gameObject);
        }

        foreach (var manager in Object.FindObjectsByType<SpawnManager>(FindObjectsSortMode.None))
        {
            if (manager != null)
                Object.Destroy(manager.gameObject);
        }
    }
}

sealed class LatinhaSpawnBootstrapRunner : MonoBehaviour
{
    Scene _scene;

    public void Begin(Scene scene)
    {
        _scene = scene;
    }

    IEnumerator Start()
    {
        yield return null;
        yield return null;

        if (!_scene.IsValid() || SceneManager.GetActiveScene() != _scene)
        {
            Destroy(gameObject);
            yield break;
        }

        LatinhaSpawnBootstrap.EnsureInActiveScene();
        Destroy(gameObject);
    }
}
