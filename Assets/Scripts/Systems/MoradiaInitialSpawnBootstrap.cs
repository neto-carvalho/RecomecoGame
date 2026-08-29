using UnityEngine;
using UnityEngine.SceneManagement;

public static class MoradiaInitialSpawnBootstrap
{
    static bool _appliedThisSceneLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _appliedThisSceneLoad = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _appliedThisSceneLoad = false;
    }

    public static void MarkApplied()
    {
        _appliedThisSceneLoad = true;
    }

    public static void TryApplyForDirectPlay()
    {
        if (_appliedThisSceneLoad)
            return;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
            return;

        if (!string.IsNullOrEmpty(SceneTransitionState.PendingSpawnId))
            return;

        if (PlayerScenePersistence.HasTravelingPlayer())
            return;

        if (!SceneTransitionState.TryApplySpawn(RecomecoSceneNames.MoradiaInicial))
            return;

        _appliedThisSceneLoad = true;
    }
}
