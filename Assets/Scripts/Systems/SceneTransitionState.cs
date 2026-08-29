using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionState
{
    public static string PendingSpawnId { get; private set; }

    public static void SetNextSpawn(string spawnId)
    {
        PendingSpawnId = spawnId;
    }

    public static string ConsumeSpawnId()
    {
        var id = PendingSpawnId;
        PendingSpawnId = null;
        return id;
    }

    public static bool TryApplyPendingSpawn()
    {
        if (string.IsNullOrEmpty(PendingSpawnId))
            return false;

        return ApplySpawn(PendingSpawnId, consumePending: true);
    }

    public static bool TryApplySpawn(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            return false;

        return ApplySpawn(spawnId, consumePending: false);
    }

    static bool ApplySpawn(string spawnId, bool consumePending)
    {
        var player = PlayerScenePersistence.ResolvePlayerInLoadedScene();
        if (player == null)
            return false;

        if (!TryResolveSpawn(spawnId, out var position, out var rotation))
            return false;

        FerroVelhoWalkableGround.EnsureInActiveScene();

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            settings.ApplyPlayerScaleForScene(player, SceneManager.GetActiveScene());

        var pos = SceneTransitionZone.ResolveSpawnOutsideZones(position, rotation);
        SpawnGroundUtility.PlacePlayerOnGround(player, pos);
        player.transform.rotation = rotation;

        if (consumePending)
            ConsumeSpawnId();

        MoradiaInitialSpawnBootstrap.MarkApplied();
        GameSession.ApplyToPlayer(player);
        SceneTransitionPlayerSetup.AfterSceneLoad(player);
        return true;
    }

    public static bool TryResolveSpawn(string spawnId, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        if (string.IsNullOrEmpty(spawnId))
            return false;

        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp == null || sp.spawnId != spawnId)
                continue;

            position = sp.transform.position + sp.positionOffset;
            rotation = sp.transform.rotation;
            return true;
        }

        if (spawnId != RecomecoSceneNames.MoradiaInicial)
            return false;

        var moradia = GameObject.Find(StreetPropsSceneColliders.MoradiaRootName);
        if (moradia == null)
            return false;

        position = moradia.transform.position + moradia.transform.forward * 1.5f;
        rotation = moradia.transform.rotation;
        return true;
    }
}
