using Controller;
using UnityEngine;

/// <summary>
/// Mantém o mesmo Player (visual, câmera, inventário) ao mudar de cena.
/// </summary>
public static class PlayerScenePersistence
{
    static GameObject _travelingPlayer;
    static PlayerCamera _travelingCamera;
    static GameObject _travelingUiRoot;

    public static GameObject TravelingPlayer => _travelingPlayer;

    public static void PrepareForSceneLoad()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        _travelingPlayer = player;
        Object.DontDestroyOnLoad(player);

        if (player.GetComponent<CharacterGroundSnap>() == null)
            player.AddComponent<CharacterGroundSnap>();

        _travelingCamera = player.GetComponentInChildren<PlayerCamera>(true);
        if (_travelingCamera == null)
            _travelingCamera = Object.FindFirstObjectByType<PlayerCamera>();
        if (_travelingCamera == null && Camera.main != null)
            _travelingCamera = Camera.main.GetComponent<PlayerCamera>();

        if (_travelingCamera != null)
        {
            Object.DontDestroyOnLoad(_travelingCamera.gameObject);
            _travelingCamera.SetPlayer(player.transform);
        }

        PersistInteractionUI();
        PersistGameplayHud();
    }

    static void PersistGameplayHud()
    {
        var root = GameplayHudBootstrap.GetHudRoot();
        if (root == null)
        {
            var hud = Object.FindFirstObjectByType<HUDController>();
            if (hud == null)
                return;

            root = hud.transform.root.gameObject;
        }

        if (root.GetComponent<Canvas>() == null && root.GetComponentInChildren<Canvas>() == null)
            return;

        Object.DontDestroyOnLoad(root);
    }

    static void PersistInteractionUI()
    {
        if (_travelingUiRoot != null)
            return;

        var ui = Object.FindFirstObjectByType<InteractionUI>();
        if (ui == null)
            return;

        _travelingUiRoot = ui.transform.root.gameObject;
        Object.DontDestroyOnLoad(_travelingUiRoot);
        InteractionUI.Register(ui);
    }

    public static void RefreshTravelingReferences()
    {
        if (_travelingPlayer == null || !_travelingPlayer)
            _travelingPlayer = GameObject.FindGameObjectWithTag("Player");

        if (_travelingCamera == null || !_travelingCamera)
        {
            if (_travelingPlayer != null)
                _travelingCamera = _travelingPlayer.GetComponentInChildren<PlayerCamera>(true);
            if (_travelingCamera == null)
                _travelingCamera = Object.FindFirstObjectByType<PlayerCamera>();
        }
    }

    public static void EnsureTravelingCameraActive()
    {
        RefreshTravelingReferences();
        if (_travelingCamera == null)
            return;

        _travelingCamera.gameObject.SetActive(true);
        var unityCam = _travelingCamera.GetComponent<Camera>();
        if (unityCam != null)
            unityCam.enabled = true;

        var listener = _travelingCamera.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = true;
    }

    public static void WireInteractionUIAfterLoad()
    {
        if (_travelingUiRoot != null)
        {
            var ui = _travelingUiRoot.GetComponentInChildren<InteractionUI>(true);
            if (ui != null)
            {
                InteractionUI.Register(ui);
                return;
            }
        }

        InteractionUI.BindForActiveScene();
    }

    public static void ResetForMenuGameplayStart()
    {
        if (_travelingPlayer != null)
            Object.Destroy(_travelingPlayer);
        if (_travelingCamera != null)
            Object.Destroy(_travelingCamera.gameObject);

        _travelingPlayer = null;
        _travelingCamera = null;
    }

    public static void RegisterRuntimeCamera(PlayerCamera camera, GameObject player)
    {
        if (camera == null)
            return;

        _travelingCamera = camera;
        if (player != null)
            _travelingPlayer = player;
    }

    public static PlayerCamera GetTravelingCamera()
    {
        if (_travelingCamera != null)
            return _travelingCamera;

        return Object.FindFirstObjectByType<PlayerCamera>();
    }

    public static void WireCameraAfterLoad(GameObject player)
    {
        if (player == null)
            return;

        var input = player.GetComponent<MovePlayerInput>();
        if (input == null)
            return;

        var cam = GetTravelingCamera();
        if (cam == null)
        {
            input.RefreshCameraBinding();
            return;
        }

        cam.SetPlayer(player.transform);
        input.BindPlayerCamera(cam);
        EnsureTravelingCameraActive();
        EnsureSingleAudioListener(cam.gameObject);
        DisableSceneCameras(cam.transform);
    }

    public static GameObject ResolvePlayerInLoadedScene()
    {
        if (_travelingPlayer == null || !_travelingPlayer)
            return GameObject.FindGameObjectWithTag("Player");

        RemoveDuplicatePlayers(_travelingPlayer);
        return _travelingPlayer;
    }

    static void RemoveDuplicatePlayers(GameObject keep)
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var go in players)
        {
            if (go != null && go != keep)
                Object.Destroy(go);
        }
    }

    static void EnsureSingleAudioListener(GameObject keepListenerOn)
    {
        foreach (var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (listener == null)
                continue;
            listener.enabled = listener.gameObject == keepListenerOn;
        }
    }

    static void DisableSceneCameras(Transform keepCamera)
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam == null || cam.transform == keepCamera || cam.transform.IsChildOf(keepCamera))
                continue;

            cam.enabled = false;
            var listener = cam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;

            var playerCam = cam.GetComponent<PlayerCamera>();
            if (playerCam != null)
                cam.gameObject.SetActive(false);
        }
    }
}
