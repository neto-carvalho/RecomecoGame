using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Ciclo dia/noite acelerado; descanso na barraca só à noite, uma vez por noite.
/// </summary>
public class GameplayDayNightCycle : MonoBehaviour
{
    public static GameplayDayNightCycle Instance { get; private set; }

    static bool s_pendingNewGameStart;

    [SerializeField] float hourOfDay = 8f;
    [SerializeField] int dayNumber = 1;
    [SerializeField] bool sleptThisNight;

    Light _sun;
    Material _daySkybox;
    Material _nightSkybox;
    bool _usingNightSkybox;

    float _visualNightBlend;
    float _visualBlendVelocity;
    int _lastClockMinuteKey = -1;
    Color _defaultAmbientSky;
    Color _defaultAmbientEquator;
    Color _defaultAmbientGround;
    bool _ambientCached;

    public float HourOfDay => hourOfDay;
    public int DayNumber => dayNumber;
    public float VisualNightBlend => _visualNightBlend;
    public event Action Changed;

    public static void RequestNewGameStart()
    {
        s_pendingNewGameStart = true;
    }

    public static void Ensure()
    {
        if (Instance != null)
            return;

        var existing = FindFirstObjectByType<GameplayDayNightCycle>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var go = new GameObject("GameplayDayNightCycle");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameplayDayNightCycle>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CacheDaySkybox();

        if (s_pendingNewGameStart)
        {
            s_pendingNewGameStart = false;
            ApplyNewGameStart(RecomecoGameplaySettings.Instance);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (RecomecoSceneNames.IsMenuScene(SceneManager.GetActiveScene()))
            return;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings == null || settings.dayNightCycleMinutes <= 0f)
            return;

        var hoursPerSecond = 24f / (settings.dayNightCycleMinutes * 60f);
        var prevNight = IsNight(settings);
        hourOfDay += hoursPerSecond * Time.deltaTime;
        while (hourOfDay >= 24f)
        {
            hourOfDay -= 24f;
            dayNumber++;
        }

        var nowNight = IsNight(settings);
        if (prevNight && !nowNight)
            sleptThisNight = false;
        if (!prevNight && nowNight)
            sleptThisNight = false;

        var targetBlend = ComputeNightBlend(hourOfDay, settings);
        var smooth = settings.dayNightVisualSmoothTime > 0f ? settings.dayNightVisualSmoothTime : 0.01f;
        _visualNightBlend = Mathf.SmoothDamp(_visualNightBlend, targetBlend, ref _visualBlendVelocity, smooth);

        ApplyVisuals(settings);

        var minuteKey = Mathf.FloorToInt(hourOfDay * 60f);
        if (minuteKey != _lastClockMinuteKey)
        {
            _lastClockMinuteKey = minuteKey;
            NotifyChanged();
        }
    }

    public bool IsNight()
    {
        return IsNight(RecomecoGameplaySettings.Instance);
    }

    bool IsNight(RecomecoGameplaySettings settings)
    {
        if (settings == null)
            return hourOfDay >= 20f || hourOfDay < 6f;

        var start = settings.nightStartHour;
        var end = settings.nightEndHour;
        if (start > end)
            return hourOfDay >= start || hourOfDay < end;

        return hourOfDay >= start && hourOfDay < end;
    }

    public bool CanSleepInBarraca()
    {
        if (!IsNight())
            return false;

        if (sleptThisNight)
            RepairSleptFlagIfStillNight();

        return !sleptThisNight;
    }

    public bool CanSleepInBed()
    {
        return IsNight() && CanSleepInBarraca();
    }

    /// <summary>Save inconsistente: marcou sono mas o relógio ainda está de noite.</summary>
    void RepairSleptFlagIfStillNight()
    {
        if (!IsNight())
            return;

        sleptThisNight = false;
    }

    public string GetClockTimeText()
    {
        var h = Mathf.FloorToInt(hourOfDay);
        var m = Mathf.FloorToInt((hourOfDay - h) * 60f);
        return h.ToString("00") + ":" + m.ToString("00");
    }

    /// <summary>Janela em que dormir na barraca é permitido (noite do jogo).</summary>
    public static string GetBarracaSleepWindowText()
    {
        var settings = RecomecoGameplaySettings.Instance;
        var start = settings != null ? settings.nightStartHour : 20f;
        var end = settings != null ? settings.nightEndHour : 6f;
        return FormatHour(start) + " às " + FormatHour(end);
    }

    static string FormatHour(float hour)
    {
        hour = Mathf.Repeat(hour, 24f);
        var h = Mathf.FloorToInt(hour);
        var m = Mathf.FloorToInt((hour - h) * 60f);
        return h.ToString("00") + ":" + m.ToString("00");
    }

    public void RunBarracaSleepTransition(Action applySleepStats, Action afterFadeIn)
    {
        StartCoroutine(BarracaSleepTransitionRoutine(applySleepStats, afterFadeIn));
    }

    public void RunSafeBedSleepTransition(Action applySleepStats, Action afterFadeIn)
    {
        StartCoroutine(SafeBedSleepTransitionRoutine(applySleepStats, afterFadeIn));
    }

    IEnumerator BarracaSleepTransitionRoutine(Action applySleepStats, Action afterFadeIn)
    {
        var settings = RecomecoGameplaySettings.Instance;
        var fadeOut = settings != null ? settings.sleepFadeOutSeconds : 0.55f;
        var fadeIn = settings != null ? settings.sleepFadeInSeconds : 0.9f;

        yield return GameplayScreenFade.FadeOut(fadeOut);

        if (GameplaySleepVideo.HasVideo())
            yield return GameplaySleepVideo.PlaySleepTransition();
        else
            yield return null;

        applySleepStats?.Invoke();
        sleptThisNight = true;
        hourOfDay = settings != null ? settings.nightEndHour : 6f;
        _visualNightBlend = ComputeNightBlend(hourOfDay, settings);
        _visualBlendVelocity = 0f;
        ApplyVisuals(settings);
        NotifyChanged();

        yield return GameplayScreenFade.FadeIn(fadeIn);
        afterFadeIn?.Invoke();
    }

    IEnumerator SafeBedSleepTransitionRoutine(Action applySleepStats, Action afterFadeIn)
    {
        var settings = RecomecoGameplaySettings.Instance;
        var fadeOut = settings != null ? settings.sleepFadeOutSeconds : 0.55f;
        var fadeIn = settings != null ? settings.sleepFadeInSeconds : 0.9f;

        yield return GameplayScreenFade.FadeOut(fadeOut);
        applySleepStats?.Invoke();
        sleptThisNight = true;
        hourOfDay = settings != null ? settings.nightEndHour : 6f;
        _visualNightBlend = ComputeNightBlend(hourOfDay, settings);
        _visualBlendVelocity = 0f;
        ApplyVisuals(settings);
        NotifyChanged();
        yield return GameplayScreenFade.FadeIn(fadeIn);
        afterFadeIn?.Invoke();
    }

    public void CompleteBarracaSleep()
    {
        var settings = RecomecoGameplaySettings.Instance;
        sleptThisNight = true;
        hourOfDay = settings != null ? settings.nightEndHour : 6f;
        _visualNightBlend = ComputeNightBlend(hourOfDay, settings);
        _visualBlendVelocity = 0f;
        ApplyVisuals(settings);
        NotifyChanged();
    }

    public void ApplyNewGameStart(RecomecoGameplaySettings settings)
    {
        hourOfDay = settings != null ? settings.newGameStartHour : 19f;
        dayNumber = 1;
        sleptThisNight = false;
        _visualNightBlend = ComputeNightBlend(hourOfDay, settings);
        _visualBlendVelocity = 0f;
        ApplyVisuals(settings);
        NotifyChanged();
    }

    public GameplayDayNightSnapshot ExportSnapshot()
    {
        return new GameplayDayNightSnapshot
        {
            hourOfDay = hourOfDay,
            dayNumber = dayNumber,
            sleptThisNight = sleptThisNight,
        };
    }

    public void ImportSnapshot(GameplayDayNightSnapshot snapshot)
    {
        if (snapshot.dayNumber <= 0 && snapshot.hourOfDay <= 0.01f)
        {
            ApplyNewGameStart(RecomecoGameplaySettings.Instance);
            return;
        }

        hourOfDay = Mathf.Repeat(snapshot.hourOfDay, 24f);
        dayNumber = Mathf.Max(1, snapshot.dayNumber);
        sleptThisNight = snapshot.sleptThisNight;
        if (sleptThisNight && IsNight(RecomecoGameplaySettings.Instance))
            sleptThisNight = false;
        var settings = RecomecoGameplaySettings.Instance;
        _visualNightBlend = ComputeNightBlend(hourOfDay, settings);
        _visualBlendVelocity = 0f;
        ApplyVisuals(settings);
        NotifyChanged();
    }

    public static void ResetForMenu()
    {
        s_pendingNewGameStart = false;
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }

    void CacheDaySkybox()
    {
        if (_daySkybox == null && RenderSettings.skybox != null)
            _daySkybox = RenderSettings.skybox;
    }

    Material ResolveNightSkybox(RecomecoGameplaySettings settings)
    {
        if (_nightSkybox != null)
            return _nightSkybox;

        if (settings != null && settings.nightSkyboxMaterial != null)
            _nightSkybox = settings.nightSkyboxMaterial;

        return _nightSkybox;
    }

    void CacheAmbientDefaults()
    {
        if (_ambientCached)
            return;

        _defaultAmbientSky = RenderSettings.ambientSkyColor;
        _defaultAmbientEquator = RenderSettings.ambientEquatorColor;
        _defaultAmbientGround = RenderSettings.ambientGroundColor;
        _ambientCached = true;
    }

    void ApplyVisuals(RecomecoGameplaySettings settings)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return;

        if (InteriorSceneColliders.IsInteriorScene(scene))
            return;

        CacheDaySkybox();
        CacheAmbientDefaults();

        var blend = Mathf.Clamp01(_visualNightBlend);
        ApplySunlight(settings, blend);
        ApplySkybox(settings, blend);
        ApplyAmbientAndFog(settings, blend);
    }

    void ApplySunlight(RecomecoGameplaySettings settings, float nightBlend)
    {
        if (_sun == null || !_sun.gameObject.activeInHierarchy)
            _sun = RenderSettings.sun;

        if (_sun == null)
        {
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light != null && light.type == LightType.Directional)
                {
                    _sun = light;
                    break;
                }
            }
        }

        if (_sun == null)
            return;

        var dayPitch = SunPitchForHour(hourOfDay, settings);
        var nightPitch = 78f;
        var pitch = Mathf.Lerp(dayPitch, nightPitch, nightBlend);
        _sun.transform.rotation = Quaternion.Euler(pitch, 170f, 0f);

        var dayIntensity = settings != null ? settings.daySunIntensity : 1.05f;
        var nightIntensity = settings != null ? settings.nightSunIntensity : 0.03f;
        _sun.intensity = Mathf.Lerp(dayIntensity, nightIntensity, nightBlend);

        var dayColor = new Color(1f, 0.96f, 0.88f);
        var nightColor = new Color(0.45f, 0.55f, 0.85f);
        _sun.color = Color.Lerp(dayColor, nightColor, nightBlend);
    }

    static float SunPitchForHour(float hour, RecomecoGameplaySettings settings)
    {
        var rise = settings != null ? settings.nightEndHour : 6f;
        var set = settings != null ? settings.nightStartHour : 20f;
        if (hour >= rise && hour < set)
        {
            var t = (hour - rise) / Mathf.Max(0.01f, set - rise);
            return Mathf.Lerp(-5f, 58f, Mathf.Sin(t * Mathf.PI));
        }

        return 8f;
    }

    void ApplySkybox(RecomecoGameplaySettings settings, float nightBlend)
    {
        var night = ResolveNightSkybox(settings);
        if (_daySkybox == null && night == null)
            return;

        var useNight = nightBlend >= 0.52f;
        if (useNight && night != null)
        {
            if (!_usingNightSkybox || RenderSettings.skybox != night)
            {
                RenderSettings.skybox = night;
                _usingNightSkybox = true;
            }
        }
        else if (_daySkybox != null)
        {
            if (_usingNightSkybox || RenderSettings.skybox != _daySkybox)
            {
                RenderSettings.skybox = _daySkybox;
                _usingNightSkybox = false;
            }
        }

        DynamicGI.UpdateEnvironment();
    }

    void ApplyAmbientAndFog(RecomecoGameplaySettings settings, float nightBlend)
    {
        var nightSky = new Color(0.02f, 0.04f, 0.12f);
        var nightEquator = new Color(0.04f, 0.05f, 0.14f);
        var nightGround = new Color(0.03f, 0.03f, 0.06f);

        RenderSettings.ambientSkyColor = Color.Lerp(_defaultAmbientSky, nightSky, nightBlend);
        RenderSettings.ambientEquatorColor = Color.Lerp(_defaultAmbientEquator, nightEquator, nightBlend);
        RenderSettings.ambientGroundColor = Color.Lerp(_defaultAmbientGround, nightGround, nightBlend);
        RenderSettings.ambientIntensity = Mathf.Lerp(1f, 0.35f, nightBlend);

        RenderSettings.fog = nightBlend > 0.08f;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = Color.Lerp(new Color(0.75f, 0.82f, 0.92f), new Color(0.03f, 0.05f, 0.12f), nightBlend);
        RenderSettings.fogDensity = Mathf.Lerp(0f, 0.018f, nightBlend);
    }

    public static float ComputeNightBlend(float hour, RecomecoGameplaySettings settings)
    {
        var rise = settings != null ? settings.nightEndHour : 6f;
        var set = settings != null ? settings.nightStartHour : 20f;
        var twilight = settings != null ? Mathf.Max(0.35f, settings.twilightHours) : 2f;

        var duskStart = set - twilight;
        var dawnEnd = rise + twilight;

        if (hour >= dawnEnd && hour < duskStart)
            return 0f;

        if (hour >= set || hour < rise)
            return 1f;

        if (hour >= duskStart && hour < set)
        {
            var t = (hour - duskStart) / twilight;
            return Smooth01(t);
        }

        if (hour >= rise && hour < dawnEnd)
        {
            var t = (hour - rise) / twilight;
            return 1f - Smooth01(t);
        }

        return 0f;
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    void NotifyChanged() => Changed?.Invoke();
}

[Serializable]
public struct GameplayDayNightSnapshot
{
    public float hourOfDay;
    public int dayNumber;
    public bool sleptThisNight;
}
