using UnityEngine;

/// <summary>
/// Luz pontual no topo de um poste — intensidade segue o ciclo dia/noite.
/// </summary>
[DisallowMultipleComponent]
public class CityStreetLight : MonoBehaviour
{
    [Tooltip("Altura da lâmpada em relação à base do poste (metros).")]
    public float lampHeight = 3.4f;

    [Tooltip("Deslocamento fino da luz em relação ao topo do poste.")]
    public Vector3 lampLocalOffset = Vector3.zero;

    Light _light;
    float _cachedIntensity;
    float _cachedRange;

    public Light PointLight => _light;

    public void EnsureConfigured(RecomecoGameplaySettings settings)
    {
        var height = lampHeight;
        if (settings != null && settings.streetLightLampHeight > 0f)
            height = settings.streetLightLampHeight;

        if (_light == null)
            _light = GetComponentInChildren<Light>(true);

        if (_light == null)
        {
            var lampGo = new GameObject("StreetLampLight");
            lampGo.transform.SetParent(transform, false);
            lampGo.transform.localPosition = new Vector3(0f, height, 0f) + lampLocalOffset;
            _light = lampGo.AddComponent<Light>();
        }
        else
        {
            _light.transform.localPosition = new Vector3(0f, height, 0f) + lampLocalOffset;
        }

        var intensity = settings != null ? settings.streetLightIntensity : 1.15f;
        var range = settings != null ? settings.streetLightRange : 11f;
        var color = settings != null ? settings.streetLightColor : new Color(1f, 0.82f, 0.55f);

        _light.type = LightType.Point;
        _light.color = color;
        _light.shadows = settings != null && settings.streetLightShadows
            ? LightShadows.Soft
            : LightShadows.None;
        _light.renderMode = LightRenderMode.Auto;

        _cachedIntensity = intensity;
        _cachedRange = range;
        ApplyNightBlend(0f);
    }

    public void ApplyNightBlend(float nightBlend)
    {
        if (_light == null)
            return;

        var on = nightBlend > 0.04f;
        _light.enabled = on;
        if (!on)
            return;

        _light.intensity = _cachedIntensity * nightBlend;
        _light.range = _cachedRange;
    }

    public void SetForcedActive(bool active, float nightBlend)
    {
        if (_light == null)
            return;

        if (!active)
        {
            _light.enabled = false;
            return;
        }

        ApplyNightBlend(nightBlend);
    }
}
