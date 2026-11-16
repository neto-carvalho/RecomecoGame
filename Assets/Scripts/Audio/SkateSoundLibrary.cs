using UnityEngine;

[CreateAssetMenu(fileName = "SkateSoundLibrary", menuName = "Recomeco/Audio/Skate Sound Library")]
public sealed class SkateSoundLibrary : ScriptableObject
{
    [Header("Rodas")]
    [Tooltip("Loop contínuo (opcional). Sem isso, usa rollTicks em ritmo pela velocidade.")]
    public AudioClip rollLoop;

    [Tooltip("Tique de roda no asfalto (fallback ou variação).")]
    public AudioClip[] rollTicks;

    [Header("Pulo")]
    public AudioClip[] jumpStart;
    public AudioClip[] jumpLand;

    [Header("Volumes")]
    [Range(0f, 1f)] public float rollVolume = 0.42f;
    [Range(0f, 1f)] public float jumpStartVolume = 0.55f;
    [Range(0f, 1f)] public float jumpLandVolume = 0.5f;

    [Header("Ritmo das rodas (sem loop)")]
    public float rollTickMinInterval = 0.12f;
    public float rollTickMaxInterval = 0.38f;
    public float rollMinSpeed = 0.35f;
    public float rollMaxSpeed = 9f;
    public Vector2 rollPitchRange = new(0.88f, 1.18f);

    public bool HasAnyAudio =>
        rollLoop != null ||
        (rollTicks != null && rollTicks.Length > 0) ||
        (jumpStart != null && jumpStart.Length > 0) ||
        (jumpLand != null && jumpLand.Length > 0);

    public AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;
        return clips[Random.Range(0, clips.Length)];
    }
}