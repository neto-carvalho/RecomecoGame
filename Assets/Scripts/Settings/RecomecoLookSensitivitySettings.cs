using System;
using UnityEngine;

/// <summary>
/// Sensibilidade da câmera (mouse). Persiste entre sessões via PlayerPrefs.
/// </summary>
public static class RecomecoLookSensitivitySettings
{
    const string PrefKey = "recomeco_look_sensitivity";

    /// <summary>Multiplicador sobre a sensibilidade base da câmera (0.1 × 360°).</summary>
    public const float Min = 0.2f;
    public const float Max = 1.2f;
    public const float Default = 0.45f;

    public static event Action Changed;

    public static float Multiplier
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, Default), Min, Max);
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
        var display = Mathf.RoundToInt(Mathf.Lerp(20f, 120f, Slider01));
        return display + "%";
    }
}
