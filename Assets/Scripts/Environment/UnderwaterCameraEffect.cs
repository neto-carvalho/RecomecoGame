using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Efeito subaquático na câmara (filtro azul URP Volume + névoa global). Ligado por <see cref="LakeWaterZone"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class UnderwaterCameraEffect : MonoBehaviour
{
    [SerializeField] private Color underwaterTint = new(0.45f, 0.72f, 0.95f, 1f);
    [SerializeField] private Color underwaterFog = new(0.05f, 0.25f, 0.45f, 1f);
    [SerializeField] private float fogDensity = 0.045f;
    [SerializeField] private float blendSpeed = 4f;

    Volume _volume;
    ColorAdjustments _colorAdjust;
    Camera _camera;

    bool _savedFog;
    bool _savedFogEnabled;
    Color _savedFogColor;
    FogMode _savedFogMode;
    float _savedFogDensity;
    Color _savedBackground;

    float _currentBlend;
    float _targetBlend;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        EnsureVolume();
        SaveFogDefaults();
    }

    void OnDestroy()
    {
        RestoreFog();
        if (_volume != null)
            _volume.weight = 0f;
        if (_camera != null)
            _camera.backgroundColor = _savedBackground;
    }

    void Update()
    {
        _currentBlend = Mathf.MoveTowards(_currentBlend, _targetBlend, blendSpeed * Time.deltaTime);

        if (_volume != null)
            _volume.weight = _currentBlend;

        ApplyUnderwaterVisuals(_currentBlend);
    }

    public void SetSubmergedAmount(float amount01)
    {
        _targetBlend = Mathf.Clamp01(amount01);
    }

    void EnsureVolume()
    {
        _volume = GetComponent<Volume>();
        if (_volume == null)
            _volume = gameObject.AddComponent<Volume>();

        _volume.isGlobal = true;
        _volume.priority = 50;
        _volume.weight = 0f;

        if (_volume.profile == null)
            _volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        if (!_volume.profile.TryGet(out _colorAdjust))
        {
            _colorAdjust = _volume.profile.Add<ColorAdjustments>(true);
            _colorAdjust.colorFilter.overrideState = true;
            _colorAdjust.colorFilter.value = Color.white;
        }
    }

    void SaveFogDefaults()
    {
        _savedFog = true;
        _savedFogEnabled = RenderSettings.fog;
        _savedFogColor = RenderSettings.fogColor;
        _savedFogMode = RenderSettings.fogMode;
        _savedFogDensity = RenderSettings.fogDensity;
        if (_camera != null)
            _savedBackground = _camera.backgroundColor;
    }

    void ApplyUnderwaterVisuals(float blend)
    {
        if (blend <= 0.001f)
        {
            if (_savedFog)
            {
                RenderSettings.fog = _savedFogEnabled;
                RenderSettings.fogColor = _savedFogColor;
                RenderSettings.fogMode = _savedFogMode;
                RenderSettings.fogDensity = _savedFogDensity;
            }

            if (_camera != null)
                _camera.backgroundColor = _savedBackground;

            if (_colorAdjust != null)
                _colorAdjust.colorFilter.value = Color.white;

            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.Lerp(_savedFogColor, underwaterFog, blend);
        RenderSettings.fogDensity = Mathf.Lerp(_savedFogDensity, fogDensity, blend);

        if (_colorAdjust != null)
            _colorAdjust.colorFilter.value = Color.Lerp(Color.white, underwaterTint, blend);

        if (_camera != null)
            _camera.backgroundColor = Color.Lerp(_savedBackground, underwaterFog, blend * 0.85f);
    }

    void RestoreFog()
    {
        if (!_savedFog)
            return;
        RenderSettings.fog = _savedFogEnabled;
        RenderSettings.fogColor = _savedFogColor;
        RenderSettings.fogMode = _savedFogMode;
        RenderSettings.fogDensity = _savedFogDensity;
    }
}
