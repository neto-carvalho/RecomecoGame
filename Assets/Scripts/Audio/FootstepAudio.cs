using Controller;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class FootstepAudio : MonoBehaviour
{
    [Header("Superfície")]
    [SerializeField] bool useSurfaceDetection = true;
    [SerializeField] FootstepSurfaceLibrary surfaceLibrary;
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float surfaceRayUp = 0.35f;
    [SerializeField] float surfaceRayDown = 1.6f;
    [SerializeField] float surfaceRefreshInterval = 0.12f;

    [Header("Fallback (sem biblioteca ou superfície desconhecida)")]
    [SerializeField] AudioClip[] walkClips;
    [SerializeField] AudioClip[] runClips;

    [Header("Ritmo")]
    [SerializeField] float walkStepInterval = 0.48f;
    [SerializeField] float runStepInterval = 0.34f;
    [SerializeField] float minHorizontalSpeed = 0.12f;
    [SerializeField] float runSpeedThreshold = 2.2f;

    [Header("Áudio")]
    [SerializeField, Range(0f, 1f)] float volume = 0.55f;
    [SerializeField] Vector2 pitchRange = new(0.92f, 1.08f);
    [SerializeField] bool spatial3D = true;
    [SerializeField] float minDistance = 0.5f;
    [SerializeField] float maxDistance = 18f;

    [Header("Debug")]
    [SerializeField] bool showCurrentSurface;

    CharacterController _controller;
    CharacterMover _mover;
    AudioSource _source;
    float _stepTimer;
    float _surfaceRefreshTimer;
    FootstepSurfaceType _currentSurface = FootstepSurfaceType.Default;

    public FootstepSurfaceType CurrentSurface => _currentSurface;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _mover = GetComponent<CharacterMover>();
        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        ConfigureAudioSource();
    }

    void ConfigureAudioSource()
    {
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = spatial3D ? 1f : 0f;
        _source.rolloffMode = AudioRolloffMode.Linear;
        _source.minDistance = minDistance * Mathf.Max(0.15f, transform.lossyScale.y);
        _source.maxDistance = maxDistance;
        _source.dopplerLevel = 0f;
    }

    void Update()
    {
        if (_controller == null)
            return;

        if (!_controller.isGrounded)
        {
            _stepTimer = 0f;
            return;
        }

        if (useSurfaceDetection)
            UpdateSurfaceProbe();

        var hasFallback = walkClips != null && walkClips.Length > 0;
        var hasLibrary = surfaceLibrary != null;
        if (!hasFallback && !hasLibrary)
            return;

        var velocity = _controller.velocity;
        velocity.y = 0f;
        var speed = velocity.magnitude;
        if (speed < minHorizontalSpeed)
        {
            _stepTimer = 0f;
            return;
        }

        var running = IsRunning(speed);
        var interval = running ? runStepInterval : walkStepInterval;
        var scale = Mathf.Max(0.15f, transform.lossyScale.y);
        interval *= Mathf.Lerp(1.15f, 0.75f, Mathf.InverseLerp(0.15f, 1f, scale));

        _stepTimer -= Time.deltaTime;
        if (_stepTimer > 0f)
            return;

        PlayStep(running);
        _stepTimer = interval;
    }

    void UpdateSurfaceProbe()
    {
        _surfaceRefreshTimer -= Time.deltaTime;
        if (_surfaceRefreshTimer > 0f)
            return;

        _surfaceRefreshTimer = surfaceRefreshInterval;
        _currentSurface = ProbeSurfaceBelowFeet();
    }

    FootstepSurfaceType ProbeSurfaceBelowFeet()
    {
        var scaleY = Mathf.Max(0.15f, transform.lossyScale.y);
        var feetY = transform.position.y + _controller.center.y * scaleY - _controller.height * scaleY * 0.5f;
        var origin = new Vector3(transform.position.x, feetY + surfaceRayUp * scaleY, transform.position.z);
        var distance = surfaceRayUp * scaleY + surfaceRayDown;

        if (Physics.Raycast(origin, Vector3.down, out var hit, distance, groundLayers, QueryTriggerInteraction.Ignore))
            return FootstepSurfaceResolver.Resolve(in hit);

        return surfaceLibrary != null
            ? surfaceLibrary.DefaultSurface
            : FootstepSurfaceType.Default;
    }

    bool IsRunning(float horizontalSpeed)
    {
        if (_mover != null && _mover.Axis.sqrMagnitude > 0.01f)
            return _mover.IsRun;

        return horizontalSpeed >= runSpeedThreshold;
    }

    void PlayStep(bool running)
    {
        AudioClip[] clips = null;

        if (useSurfaceDetection && surfaceLibrary != null)
        {
            var surface = _currentSurface;
            if (surface == FootstepSurfaceType.Default)
                surface = surfaceLibrary.DefaultSurface;

            if (!surfaceLibrary.TryGetClips(surface, running, out clips) || clips == null || clips.Length == 0)
                surfaceLibrary.TryGetClips(surfaceLibrary.DefaultSurface, running, out clips);
        }

        if (clips == null || clips.Length == 0)
            clips = running ? runClips : walkClips;
        if (clips == null || clips.Length == 0)
            clips = walkClips;
        if (clips == null || clips.Length == 0)
            return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        _source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        _source.PlayOneShot(clip, volume);
    }

    public void SetClips(AudioClip[] walk, AudioClip[] run = null)
    {
        walkClips = walk;
        if (run != null && run.Length > 0)
            runClips = run;
        else if (runClips == null || runClips.Length == 0)
            runClips = walk;
    }

    public void SetSurfaceLibrary(FootstepSurfaceLibrary library)
    {
        surfaceLibrary = library;
        useSurfaceDetection = library != null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_source != null)
            ConfigureAudioSource();
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(FootstepAudio))]
public sealed class FootstepAudioEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var foot = (FootstepAudio)target;
        if (foot.CurrentSurface != FootstepSurfaceType.Default && Application.isPlaying)
            UnityEditor.EditorGUILayout.HelpBox($"Superfície actual: {foot.CurrentSurface}", UnityEditor.MessageType.None);
    }
}
#endif
