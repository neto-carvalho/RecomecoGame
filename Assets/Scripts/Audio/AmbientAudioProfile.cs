using UnityEngine;

[CreateAssetMenu(fileName = "AmbientAudioProfile", menuName = "Recomeco/Audio/Ambient Audio Profile")]
public sealed class AmbientAudioProfile : ScriptableObject
{
    [Header("Cidade (fundo global)")]
    [Tooltip("Ex.: City Ambience - Park - Spring (pássaros + cidade leve).")]
    public AudioClip cityAmbience;
    [Range(0f, 1f)] public float cityVolume = 0.22f;

    [Header("Natureza (terreno / parque)")]
    public AudioClip natureBirds;
    [Range(0f, 1f)] public float natureVolume = 0.35f;
    public AudioClip natureWind;
    [Range(0f, 1f)] public float windVolume = 0.12f;

    [Header("Água (lago / rio)")]
    public AudioClip waterStream;
    [Range(0f, 1f)] public float waterVolume = 0.28f;

    [Header("Transição")]
    public float blendSeconds = 2f;
}
