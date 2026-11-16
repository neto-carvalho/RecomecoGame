using Controller;
using UnityEngine;

/// <summary>Som de rodas e pulo/aterrizagem com skate montado.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerSkateAudio : MonoBehaviour
{
    SkateSoundLibrary _library;
    CharacterController _controller;
    CharacterMover _mover;
    PlayerSkateController _skate;
    FootstepAudio _footsteps;

    AudioSource _rollSource;
    AudioSource _sfxSource;

    float _rollTickTimer;
    bool _wasGrounded = true;
    bool _wasJumping;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _mover = GetComponent<CharacterMover>();
        _skate = GetComponent<PlayerSkateController>();
        _footsteps = GetComponent<FootstepAudio>();

        _rollSource = CreateSource("SkateRoll");
        _rollSource.loop = true;
        _rollSource.playOnAwake = false;

        _sfxSource = CreateSource("SkateSfx");
    }

    AudioSource CreateSource(string id)
    {
        var child = transform.Find(id);
        if (child == null)
        {
            var go = new GameObject(id);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        var src = child.GetComponent<AudioSource>();
        if (src == null)
            src = child.gameObject.AddComponent<AudioSource>();

        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 0.6f * Mathf.Max(0.15f, transform.lossyScale.y);
        src.maxDistance = 22f;
        src.dopplerLevel = 0f;
        return src;
    }

    void Update()
    {
        ResolveLibrary();
        if (_library == null || !_library.HasAnyAudio)
            return;

        if (_skate == null)
            _skate = GetComponent<PlayerSkateController>();

        if (_skate == null || !_skate.IsRiding)
        {
            StopRollImmediate();
            return;
        }

        if (_footsteps == null)
            _footsteps = GetComponent<FootstepAudio>();

        UpdateRoll();
        UpdateJumpSounds();
    }

    void ResolveLibrary()
    {
        if (_library != null && _library.HasAnyAudio)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.skateSoundLibrary != null)
        {
            _library = settings.skateSoundLibrary;
            return;
        }

        _library = Resources.Load<SkateSoundLibrary>("Recomeco/SkateSoundLibrary");
    }

    void UpdateRoll()
    {
        if (_controller == null)
            return;

        if (!_controller.isGrounded)
        {
            FadeOrStopRoll(0f);
            _rollTickTimer = 0f;
            return;
        }

        var velocity = _controller.velocity;
        velocity.y = 0f;
        var speed = velocity.magnitude;
        var minSp = _library.rollMinSpeed;
        var maxSp = _library.rollMaxSpeed;

        if (speed < minSp)
        {
            FadeOrStopRoll(0f);
            _rollTickTimer = 0f;
            return;
        }

        var t = Mathf.InverseLerp(minSp, maxSp, speed);
        var vol = _library.rollVolume * Mathf.Lerp(0.35f, 1f, t);
        var pitch = Mathf.Lerp(_library.rollPitchRange.x, _library.rollPitchRange.y, t);

        if (_library.rollLoop != null)
        {
            if (_rollSource.clip != _library.rollLoop)
            {
                _rollSource.clip = _library.rollLoop;
                _rollSource.loop = true;
            }

            _rollSource.volume = vol;
            _rollSource.pitch = pitch;
            if (!_rollSource.isPlaying)
                _rollSource.Play();
            return;
        }

        FadeOrStopRoll(0f);
        var interval = Mathf.Lerp(_library.rollTickMaxInterval, _library.rollTickMinInterval, t);
        _rollTickTimer -= Time.deltaTime;
        if (_rollTickTimer > 0f)
            return;

        var tick = _library.PickRandom(_library.rollTicks);
        if (tick != null)
            PlayOneShot(tick, vol, pitch);

        _rollTickTimer = interval;
    }

    void UpdateJumpSounds()
    {
        if (_controller == null || _mover == null)
            return;

        var jumping = _mover.IsJumping;
        var grounded = _controller.isGrounded;

        if (jumping && !_wasJumping)
        {
            var clip = _library.PickRandom(_library.jumpStart);
            if (clip != null)
                PlayOneShot(clip, _library.jumpStartVolume, Random.Range(0.95f, 1.05f));
        }

        if (grounded && !_wasGrounded)
        {
            var clip = _library.PickRandom(_library.jumpLand);
            if (clip != null)
                PlayOneShot(clip, _library.jumpLandVolume, Random.Range(0.92f, 1.08f));
        }

        _wasJumping = jumping;
        _wasGrounded = grounded;
    }

    void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || _sfxSource == null)
            return;

        _sfxSource.pitch = pitch;
        _sfxSource.PlayOneShot(clip, volume);
    }

    void FadeOrStopRoll(float targetVolume)
    {
        if (_rollSource == null)
            return;

        if (targetVolume <= 0.001f)
        {
            StopRollImmediate();
            return;
        }

        _rollSource.volume = targetVolume;
    }

    void StopRollImmediate()
    {
        if (_rollSource == null || !_rollSource.isPlaying)
            return;

        _rollSource.Stop();
        _rollSource.volume = 0f;
    }

    public void ResetState()
    {
        _wasGrounded = _controller != null && _controller.isGrounded;
        _wasJumping = _mover != null && _mover.IsJumping;
        _rollTickTimer = 0f;
        StopRollImmediate();
    }
}