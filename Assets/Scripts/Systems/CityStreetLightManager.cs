using UnityEngine;
using UnityEngine.SceneManagement;

public class CityStreetLightManager : MonoBehaviour
{
    public static CityStreetLightManager Instance { get; private set; }

    CityStreetLight[] _lights = System.Array.Empty<CityStreetLight>();
    float _lastRefreshTime = -999f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        if (GameplayDayNightCycle.Instance != null)
            GameplayDayNightCycle.Instance.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (GameplayDayNightCycle.Instance != null)
            GameplayDayNightCycle.Instance.Changed -= Refresh;
    }

    void LateUpdate()
    {
        if (Time.unscaledTime - _lastRefreshTime > 0.35f)
            Refresh();
    }

    public static void EnsureInCityScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && !settings.enableCityStreetLights)
            return;

        if (Instance != null)
        {
            Instance.RefreshRegistry();
            return;
        }

        var host = new GameObject("CityStreetLightManager");
        Instance = host.AddComponent<CityStreetLightManager>();
        Instance.RefreshRegistry();
    }

    public void RefreshRegistry()
    {
        _lights = FindObjectsByType<CityStreetLight>(FindObjectsSortMode.None);
        var settings = RecomecoGameplaySettings.Instance;
        foreach (var lamp in _lights)
        {
            if (lamp == null)
                continue;
            lamp.EnsureConfigured(settings);
        }

        Refresh();
    }

    void Refresh()
    {
        _lastRefreshTime = Time.unscaledTime;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && !settings.enableCityStreetLights)
        {
            SetAllOff();
            return;
        }

        var cycle = GameplayDayNightCycle.Instance;
        var blend = cycle != null ? cycle.VisualNightBlend : 0f;

        if (_lights == null || _lights.Length == 0)
            return;

        var maxActive = settings != null ? settings.streetLightMaxActiveNearPlayer : 28;
        if (maxActive <= 0 || _lights.Length <= maxActive)
        {
            foreach (var lamp in _lights)
            {
                if (lamp != null)
                    lamp.SetForcedActive(true, blend);
            }

            return;
        }

        var playerPos = ResolvePlayerPosition();
        var scored = new (CityStreetLight lamp, float dist)[_lights.Length];
        for (var i = 0; i < _lights.Length; i++)
        {
            var lamp = _lights[i];
            var dist = lamp != null
                ? Vector3.SqrMagnitude(lamp.transform.position - playerPos)
                : float.MaxValue;
            scored[i] = (lamp, dist);
        }

        System.Array.Sort(scored, (a, b) => a.dist.CompareTo(b.dist));

        for (var i = 0; i < scored.Length; i++)
        {
            var lamp = scored[i].lamp;
            if (lamp == null)
                continue;

            lamp.SetForcedActive(i < maxActive, blend);
        }
    }

    static Vector3 ResolvePlayerPosition()
    {
        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position : Vector3.zero;
    }

    void SetAllOff()
    {
        if (_lights == null)
            return;

        foreach (var lamp in _lights)
        {
            if (lamp != null)
                lamp.SetForcedActive(false, 0f);
        }
    }
}
