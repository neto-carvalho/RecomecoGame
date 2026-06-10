using System.Collections;
using Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante câmera third-person e Animator do player em cenas sem setup completo (ex.: FerroVelho).
/// </summary>
public static class GameplaySceneRuntimeSetup
{
    static bool _sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneHook()
    {
        if (_sceneHookRegistered)
            return;
        _sceneHookRegistered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ScheduleSetup();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScheduleSetup();
    }

    static void ScheduleSetup()
    {
        if (!Application.isPlaying)
            return;

        var existing = GameObject.Find("_GameplaySceneSetup");
        if (existing != null)
            return;

        var go = new GameObject("_GameplaySceneSetup");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<GameplaySceneSetupRunner>();
    }

    public static void Run()
    {
        if (RecomecoSceneNames.IsMenuScene(SceneManager.GetActiveScene()))
            return;

        FerroVelhoWalkableGround.EnsureInActiveScene();
        GameplayHudBootstrap.Ensure();

        var player = FindPlayer();
        if (player == null)
            return;

        if (!string.IsNullOrEmpty(SceneTransitionState.PendingSpawnId))
            SceneTransitionState.TryApplyPendingSpawn();

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            settings.ApplyPlayerScaleForScene(player, SceneManager.GetActiveScene());

        if (FerroVelhoWalkableGround.IsFerroVelhoActive())
        {
            if (player.GetComponent<FerroVelhoPlayerGuard>() == null)
                player.AddComponent<FerroVelhoPlayerGuard>();
        }

        PlayerAppearanceSetup.Apply(player);
        EnsurePlayerComponents(player);
        EnsureGameplayCamera(player);
        PlayerAnimatorSetup.RefreshLocomotion(player);
        GameSession.ApplyToPlayer(player);
        GameplayHudBootstrap.WirePlayerInventory(player);
        SceneTransitionPlayerSetup.AfterSceneLoad(player);
    }

    static void EnsureGameplayCamera(GameObject player)
    {
        if (player == null)
            return;

        var playerCamera = PlayerScenePersistence.GetTravelingCamera();
        if (playerCamera == null)
            playerCamera = Object.FindFirstObjectByType<PlayerCamera>();

        if (playerCamera == null)
        {
            playerCamera = CreateFollowCamera(player.transform);
            if (playerCamera != null)
                PlayerScenePersistence.RegisterRuntimeCamera(playerCamera, player);
        }

        if (playerCamera != null)
            PlayerScenePersistence.WireCameraAfterLoad(player);
    }

    static GameObject FindPlayer()
    {
        var traveling = PlayerScenePersistence.ResolvePlayerInLoadedScene();
        if (traveling != null)
            return traveling;

        var input = Object.FindFirstObjectByType<MovePlayerInput>(FindObjectsInactive.Include);
        return input != null ? input.gameObject : null;
    }

    static void EnsurePlayerComponents(GameObject player)
    {
        if (player.GetComponent<PlayerLocomotionBootstrap>() == null)
            player.AddComponent<PlayerLocomotionBootstrap>();

        PlayerAnimatorSetup.Apply(player, RecomecoGameplaySettings.Instance);

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
        {
            var mover = player.GetComponent<CharacterMover>();
            if (mover != null)
                settings.ApplyToMover(mover, player.transform);
        }

        EnsureFootstepAudio(player, settings);
    }

    static void EnsureFootstepAudio(GameObject player, RecomecoGameplaySettings settings)
    {
        if (player.GetComponent<CharacterController>() == null)
            return;

        var library = settings != null ? settings.footstepLibrary : null;

        var footsteps = player.GetComponent<FootstepAudio>();
        if (footsteps == null)
        {
            if (library == null)
            {
                Debug.LogWarning(
                    "GameplaySceneRuntimeSetup: player sem FootstepAudio e sem footstepLibrary em " +
                    "RecomecoGameplaySettings — passos ficam sem som nesta cena.");
                return;
            }

            footsteps = player.AddComponent<FootstepAudio>();
        }

        if (library != null)
            footsteps.SetSurfaceLibrary(library);
    }

    static PlayerCamera CreateFollowCamera(Transform player)
    {
        var camGo = new GameObject("GameplayCamera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;

        if (camGo.GetComponent<AudioListener>() == null)
            camGo.AddComponent<AudioListener>();

        TryAddUrpCameraData(camGo);

        var thirdPerson = camGo.AddComponent<ThirdPersonCamera>();
        thirdPerson.SetPlayer(player);

        var scale = Mathf.Max(0.15f, player.lossyScale.y);
        camGo.transform.position = player.position + Vector3.up * (1.5f * scale) + Vector3.back * (4f * scale);
        camGo.transform.LookAt(player.position + Vector3.up * (1.2f * scale));

        return thirdPerson;
    }

    static void TryAddUrpCameraData(GameObject camGo)
    {
        var urpType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null && camGo.GetComponent(urpType) == null)
            camGo.AddComponent(urpType);
    }

    public static void WireCamera(GameObject player, PlayerCamera playerCamera)
    {
        if (playerCamera != null)
            playerCamera.SetPlayer(player.transform);

        var input = player.GetComponent<MovePlayerInput>();
        if (input != null)
            input.BindPlayerCamera(playerCamera);
    }

    static void SuppressStaticDemoCameras(Transform keepCamera)
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam == null || cam.transform == keepCamera || cam.transform.IsChildOf(keepCamera))
                continue;

            if (cam.GetComponent<PlayerCamera>() != null)
                continue;

            cam.enabled = false;
            var listener = cam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }
    }
}

/// <summary>Executa o setup após Awake/Start de todos os objetos da cena.</summary>
sealed class GameplaySceneSetupRunner : MonoBehaviour
{
    void Awake()
    {
        FerroVelhoWalkableGround.TrySetupActiveScene();
        GameplaySceneRuntimeSetup.Run();
    }

    IEnumerator Start()
    {
        yield return null;
        FerroVelhoWalkableGround.TrySetupActiveScene();
        GameplaySceneRuntimeSetup.Run();
        Destroy(gameObject);
    }
}
