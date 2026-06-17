using System.Collections.Generic;
using UnityEngine;

public sealed class AmbientAudioController : MonoBehaviour
{
    public static AmbientAudioController Instance { get; private set; }

    [SerializeField] AmbientAudioProfile profile;
    [SerializeField] bool playOnStart = true;

    static readonly List<AmbientZone> ActiveZones = new();

    AudioSource _city;
    AudioSource _natureBirds;
    AudioSource _natureWind;
    AudioSource _water;

    float _cityVol;
    float _natureVol;
    float _windVol;
    float _waterVol;

    float _targetCity;
    float _targetNature;
    float _targetWind;
    float _targetWater;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureSources();
        _targetCity = profile != null ? profile.cityVolume : 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (playOnStart && profile != null)
            StartAmbience();
    }

    void Update()
    {
        if (profile == null)
            return;

        var speed = profile.blendSeconds > 0.01f ? Time.deltaTime / profile.blendSeconds : 1f;
        _cityVol = Mathf.MoveTowards(_cityVol, _targetCity, speed);
        _natureVol = Mathf.MoveTowards(_natureVol, _targetNature, speed);
        _windVol = Mathf.MoveTowards(_windVol, _targetWind, speed);
        _waterVol = Mathf.MoveTowards(_waterVol, _targetWater, speed);

        ApplyVolume(_city, _cityVol);
        ApplyVolume(_natureBirds, _natureVol);
        ApplyVolume(_natureWind, _windVol);
        ApplyVolume(_water, _waterVol);
    }

    public void StartAmbience()
    {
        PlayLoop(_city, profile.cityAmbience);
        PlayLoop(_natureBirds, profile.natureBirds);
        PlayLoop(_natureWind, profile.natureWind);
        PlayLoop(_water, profile.waterStream);

        _cityVol = profile.cityVolume;
        _targetCity = profile.cityVolume;
        RecalculateTargets();
    }

    public static void NotifyEnter(AmbientZone zone)
    {
        if (zone == null || ActiveZones.Contains(zone))
            return;

        ActiveZones.Add(zone);
        Instance?.RecalculateTargets();
    }

    public static void NotifyExit(AmbientZone zone)
    {
        if (zone == null)
            return;

        ActiveZones.Remove(zone);
        Instance?.RecalculateTargets();
    }

    void RecalculateTargets()
    {
        if (profile == null)
            return;

        var nature = 0f;
        var water = 0f;

        foreach (var zone in ActiveZones)
        {
            if (zone.Kind == AmbientZone.ZoneKind.Lake)
            {
                water = Mathf.Max(water, zone.WaterBlend);
                nature = Mathf.Max(nature, zone.NatureBlend * 0.45f);
            }
            else
                nature = Mathf.Max(nature, zone.NatureBlend);
        }

        _targetNature = nature * profile.natureVolume;
        _targetWind = nature * profile.windVolume;
        _targetWater = water * profile.waterVolume;
        _targetCity = Mathf.Lerp(profile.cityVolume, profile.cityVolume * 0.35f, Mathf.Clamp01(nature + water * 0.5f));
    }

    void EnsureSources()
    {
        _city = GetOrAddSource("CityAmbience");
        _natureBirds = GetOrAddSource("NatureBirds");
        _natureWind = GetOrAddSource("NatureWind");
        _water = GetOrAddSource("WaterAmbience");

        foreach (var src in new[] { _city, _natureBirds, _natureWind, _water })
        {
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            src.dopplerLevel = 0f;
        }
    }

    AudioSource GetOrAddSource(string childName)
    {
        var t = transform.Find(childName);
        if (t != null && t.TryGetComponent<AudioSource>(out var existing))
            return existing;

        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go.AddComponent<AudioSource>();
    }

    static void PlayLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        source.clip = clip;
        source.volume = 0f;
        if (!source.isPlaying)
            source.Play();
    }

    static void ApplyVolume(AudioSource source, float volume)
    {
        if (source == null || !source.isPlaying)
            return;

        source.volume = volume;
    }

    public void SetProfile(AmbientAudioProfile newProfile)
    {
        profile = newProfile;
        if (isActiveAndEnabled && playOnStart)
            StartAmbience();
    }
}
