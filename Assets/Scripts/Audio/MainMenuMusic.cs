using UnityEngine;

[DisallowMultipleComponent]
public class MainMenuMusic : MonoBehaviour
{
    const string DefaultResourcesPath = "Audio/musica_recomeco";

    [SerializeField] AudioClip menuMusic;
    [SerializeField, Range(0f, 1f)] float volume = 0.65f;

    AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 0f;
    }

    void Start()
    {
        var clip = menuMusic != null ? menuMusic : Resources.Load<AudioClip>(DefaultResourcesPath);
        if (clip == null)
        {
            Debug.LogWarning(
                "MainMenuMusic: coloque musica_recomeco.mp3 em Assets/Resources/Audio/.");
            return;
        }

        _source.clip = clip;
        _source.volume = volume;
        _source.Play();
    }

    void OnDestroy()
    {
        StopPlayback();
    }

    public static void StopIfPlaying()
    {
        var music = Object.FindFirstObjectByType<MainMenuMusic>();
        if (music != null)
            music.StopPlayback();
    }

    public void StopPlayback()
    {
        if (_source != null && _source.isPlaying)
            _source.Stop();
    }
}
