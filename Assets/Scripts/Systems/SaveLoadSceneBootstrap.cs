using System.Collections;
using Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante câmera e estado após carregar save (complementa GameplaySceneRuntimeSetup).
/// </summary>
public sealed class SaveLoadSceneBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ScheduleIfNeeded()
    {
        if (!SaveGameManager.HasPendingLoad)
            return;

        if (RecomecoSceneNames.IsMenuScene(SceneManager.GetActiveScene()))
            return;

        var host = new GameObject(nameof(SaveLoadSceneBootstrap));
        host.AddComponent<SaveLoadSceneBootstrap>();
    }

    IEnumerator Start()
    {
        GameplayScreenFade.ForceClear();

        for (var i = 0; i < 12; i++)
        {
            yield return null;

            GameplayPlayerSpawner.EnsureForActiveScene();
            GameplaySceneRuntimeSetup.Run();

            var player = GameObject.FindGameObjectWithTag("Player");
            var cam = Object.FindFirstObjectByType<PlayerCamera>(FindObjectsInactive.Include);
            if (player != null && cam != null)
                yield break;
        }

        Destroy(gameObject);
    }
}
