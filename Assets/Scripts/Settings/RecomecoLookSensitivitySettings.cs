using System;
using UnityEngine;

/// <summary>
/// Sensibilidade da câmera (mouse). Persiste entre sessões via PlayerPrefs.
/// O slider 0–100% mapeia linearmente de <see cref="Min"/> a <see cref="Max"/>.
/// </summary>
public static class RecomecoLookSensitivitySettings
{
    const string PrefKey = "recomeco_look_sensitivity";

    /// <summary>Mínimo real no 0% do slider (antes era 0.2 — ainda “rápido” demais).</summary>
    public const float Min = 0.035f;
    public const float Max = 1f;
    public const float Default = 0.45f;

    public static event Action Changed;

    public static float Multiplier
    {
        get
        {
            var value = PlayerPrefs.GetFloat(PrefKey, Default);
            return Mathf.Clamp(value, Min, Max);
        }
        set
        {
            var clamped = Mathf.Clamp(value, Min, Max);
            if (Mathf.Approximately(clamped, Multiplier))
                return;

            PlayerPrefs.SetFloat(PrefKey, clamped);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static float Slider01
    {
        get => Mathf.InverseLerp(Min, Max, Multiplier);
        set => Multiplier = Mathf.Lerp(Min, Max, Mathf.Clamp01(value));
    }

    public static string FormatPercent()
    {
        return Mathf.RoundToInt(Slider01 * 100f) + "%";
    }
}