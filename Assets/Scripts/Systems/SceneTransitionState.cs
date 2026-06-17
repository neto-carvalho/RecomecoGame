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

        var spawnId = PendingSpawnId;
        var player = PlayerScenePersistence.ResolvePlayerInLoadedScene();
        if (player == null)
            return false;

        SceneSpawnPoint matched = null;
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp != null && sp.spawnId == spawnId)
            {
                matched = sp;
                break;
            }
        }

        if (matched == null)
            return false;

        FerroVelhoWalkableGround.EnsureInActiveScene();

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            settings.ApplyPlayerScaleForScene(player, SceneManager.GetActiveScene());

        var pos = SceneTransitionZone.ResolveSpawnOutsideZones(
            matched.transform.position + matched.positionOffset,
            matched.transform.rotation);
        SpawnGroundUtility.PlacePlayerOnGround(player, pos);
        player.transform.rotation = matched.transform.rotation;

        ConsumeSpawnId();
        GameSession.ApplyToPlayer(player);
        SceneTransitionPlayerSetup.AfterSceneLoad(player);
        return true;
    }
}
