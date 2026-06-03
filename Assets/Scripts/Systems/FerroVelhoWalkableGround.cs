using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Piso físico invisível na FerroVelho (junkyard sem colliders no chão).
/// </summary>
public static class FerroVelhoWalkableGround
{
    const string GroundName = "Chao_FerroVelho";
    const string DefaultSpawnId = "EntradaFerroVelho";

    public static Vector3 Size { get; private set; } = new(140f, 2f, 140f);

    public static Vector3 DefaultSpawn { get; private set; } = new(201.9f, 8.29f, 0f);

    static Vector3 _groundCenter = new(201.9f, 7.29f, 0f);

    /// <summary>Centro do BoxCollider do chão invisível.</summary>
    public static Vector3 Center => _groundCenter;

    /// <summary>Topo do box collider do chão.</summary>
    public static float SurfaceY => _groundCenter.y + Size.y * 0.5f;

    public static bool IsFerroVelhoScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        if (scene.path.IndexOf("FerroVelho", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (scene.name.IndexOf("FerroVelho", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return HasJunkyardFerroVelhoLayout(scene);
    }

    public static bool IsFerroVelhoActive()
    {
        return IsFerroVelhoScene(SceneManager.GetActiveScene());
    }

    public static bool HasJunkyardFerroVelhoLayout(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        var hasProps = false;
        var hasBuildings = false;
        var hasGeometry = false;
        var hasFerroVelhoMarker = false;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Props")
                hasProps = true;
            if (root.name == "Buildings")
                hasBuildings = true;
            if (root.name == "Geometry")
                hasGeometry = true;
            if (root.name == "FerroVelho_Venda" || root.name == "Spawn_EntradaFerroVelho" ||
                root.name == GroundName)
                hasFerroVelhoMarker = true;
        }

        return hasFerroVelhoMarker || (hasGeometry && hasProps) || (hasProps && hasBuildings);
    }

    /// <summary>Lê Spawn_EntradaFerroVelho na cena para alinhar chão e spawn.</summary>
    public static void RefreshFromSceneMarkers()
    {
        SceneSpawnPoint entrada = null;
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp != null && sp.spawnId == DefaultSpawnId)
            {
                entrada = sp;
                break;
            }
        }

        if (entrada != null)
        {
            DefaultSpawn = entrada.transform.position + entrada.positionOffset;
            _groundCenter = new Vector3(DefaultSpawn.x, DefaultSpawn.y - 1f, DefaultSpawn.z);
            return;
        }

        var venda = GameObject.Find("FerroVelho_Venda");
        if (venda != null)
        {
            DefaultSpawn = venda.transform.position + new Vector3(-5f, 0f, -3f);
            _groundCenter = new Vector3(DefaultSpawn.x, DefaultSpawn.y - 1f, DefaultSpawn.z);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad()
    {
        TrySetupActiveScene();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsFerroVelhoScene(scene))
            return;

        FerroVelhoSceneGround.EnsureSceneGroundColliders(scene);
        FerroVelhoSceneColliders.EnsureColliders(scene);
        RefreshFromSceneMarkers();
        EnsureInScene(scene);
        if (!SceneTransitionState.TryApplyPendingSpawn())
            PlacePlayersOnGround();
    }

    public static void TrySetupActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!IsFerroVelhoScene(scene))
            return;

        FerroVelhoSceneGround.EnsureSceneGroundColliders(scene);
        FerroVelhoSceneColliders.EnsureColliders(scene);
        RefreshFromSceneMarkers();
        EnsureInScene(scene);

        if (SceneTransitionState.TryApplyPendingSpawn())
            return;

        if (!string.IsNullOrEmpty(SceneTransitionState.PendingSpawnId))
            return;

        ApplyScenePlayerScale();
        PlacePlayersOnGround();
    }

    public static void EnsureInActiveScene()
    {
        RefreshFromSceneMarkers();
        EnsureInScene(SceneManager.GetActiveScene());
    }

    public static void EnsureInScene(Scene scene)
    {
        if (!scene.IsValid() || !IsFerroVelhoScene(scene))
            return;

        FerroVelhoSceneGround.EnsureSceneGroundColliders(scene);
        RefreshFromSceneMarkers();

        if (FerroVelhoSceneGround.HasWalkableSceneGround(scene))
        {
            var fallback = GameObject.Find(GroundName);
            if (fallback != null)
                fallback.SetActive(false);
            return;
        }

        GameObject ground = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == GroundName)
            {
                ground = root;
                break;
            }
        }

        if (ground == null)
            ground = GameObject.Find(GroundName);

        if (ground == null)
        {
            CreateGroundObject();
            return;
        }

        ground.transform.position = _groundCenter;
        var box = ground.GetComponent<BoxCollider>();
        if (box != null)
            box.size = Size;
    }

    static void CreateGroundObject()
    {
        var ground = new GameObject(GroundName);
        ground.transform.position = _groundCenter;
        ground.isStatic = true;

        var box = ground.AddComponent<BoxCollider>();
        box.size = Size;
        box.center = Vector3.zero;
        box.isTrigger = false;
    }

    public static void ApplyScenePlayerScale()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null)
            return;

        var scene = SceneManager.GetActiveScene();
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (player != null)
                settings.ApplyPlayerScaleForScene(player, scene);
        }
    }

    public static void PlacePlayersOnGround()
    {
        RefreshFromSceneMarkers();
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var player in players)
        {
            if (player == null)
                continue;
            SpawnGroundUtility.PlacePlayerOnGround(player, DefaultSpawn);
        }
    }
}
