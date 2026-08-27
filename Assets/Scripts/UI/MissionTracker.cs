using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MissionTracker : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += (_, __) => EnsureTracker();
        EnsureTracker();
    }

    static void EnsureTracker()
    {
        if (!Application.isPlaying)
            return;

        var scene = SceneManager.GetActiveScene();
        if (RecomecoSceneNames.IsMenuScene(scene))
            return;

        if (Object.FindFirstObjectByType<MissionTracker>() != null)
            return;

        var host = new GameObject(nameof(MissionTracker));
        host.AddComponent<MissionTracker>();
    }

    void Update()
    {
        var scene = SceneManager.GetActiveScene();
        MissionProgress.EnsureStartedForScene(scene.name);

        if (!MissionProgress.IsActive)
            return;

        if (MissionProgress.Current != MissionId.CollectCans)
            return;

        MissionProgress.NotifyCollectProgress(ResolveLatinhaCount());
    }

    static int ResolveLatinhaCount()
    {
        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return 0;

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = Object.FindFirstObjectByType<Inventory>();
        if (inventory == null)
            return 0;

        return inventory.GetItemCount(MissionProgress.CanItemName);
    }
}
