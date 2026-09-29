using Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante um Player jogável na cena após carregar save (ex.: interior sem prefab embutido).
/// </summary>
public static class GameplayPlayerSpawner
{
    const string ResourcesPrefabPath = "Recomeco/GameplayPlayer";

    public static bool EnsureForActiveScene()
    {
        if (RecomecoSceneNames.IsMenuScene(SceneManager.GetActiveScene()))
            return false;

        if (PlayerScenePersistence.HasTravelingPlayer())
            return true;

        if (GameObject.FindGameObjectWithTag("Player") != null)
            return true;

        var prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
        if (prefab == null)
        {
            Debug.LogError(
                "GameplayPlayerSpawner: prefab não encontrado em Resources/" + ResourcesPrefabPath + ".");
            return false;
        }

        var instance = Object.Instantiate(prefab);
        instance.name = "Player";
        instance.tag = "Player";

        StripLegacyCameras(instance);
        DisableLegacyMovement(instance);

        if (instance.GetComponent<PlayerLocomotionBootstrap>() == null)
            instance.AddComponent<PlayerLocomotionBootstrap>();

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            settings.ApplyPlayerScaleForScene(instance, SceneManager.GetActiveScene());

        PlayerAppearanceSetup.Apply(instance);

        PlayerScenePersistence.RegisterTravelingPlayer(instance);
        return true;
    }

    static void StripLegacyCameras(GameObject player)
    {
        foreach (var cam in player.GetComponentsInChildren<Camera>(true))
        {
            if (cam != null && cam.gameObject != player)
                Object.Destroy(cam.gameObject);
        }
    }

    static void DisableLegacyMovement(GameObject player)
    {
        var legacy = player.GetComponent<PlayerMovement>();
        if (legacy != null)
            legacy.enabled = false;
    }
}
