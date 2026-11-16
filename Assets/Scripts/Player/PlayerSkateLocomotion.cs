using Controller;
using UnityEngine;

/// <summary>
/// Parâmetros do Animator durante skate (ride, remar, pulo). Requer clips + Character_Skate.controller.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class PlayerSkateLocomotion : MonoBehaviour
{
    CharacterMover _mover;
    Animator _animator;
    bool _wasMoving;
    float _pushCooldown;
    float _mountGraceUntil;
    float _coastPushTimer;
    Vector3 _hipsBaseLocal;
    bool _hipsBaseCaptured;
    bool _wasJumping;

    void Awake()
    {
        _mover = GetComponent<CharacterMover>();
        enabled = false;
    }

    public void PrepareMount()
    {
        _wasMoving = _mover != null && _mover.IsMoving;
        _pushCooldown = 0f;

        var settings = RecomecoGameplaySettings.Instance;
        var grace = settings != null ? settings.skateMountPushDelaySeconds : 0.45f;
        _coastPushTimer = settings != null ? settings.skateCoastPushIntervalSeconds : 3.5f;
        _wasJumping = false;
        _mountGraceUntil = Time.time + Mathf.Max(0f, grace);

        _hipsBaseCaptured = false;
        ResolveAnimator();
        CaptureHipsBaseline();
    }

    void OnEnable()
    {
        PrepareMount();
    }

    void LateUpdate()
    {
        if (!enabled || _mover == null)
            return;

        var skate = GetComponent<PlayerSkateController>();
        if (skate == null || !skate.IsRiding)
            return;

        if (_animator == null)
            ResolveAnimator();
        if (_animator == null)
            return;

        if (!_hipsBaseCaptured)
            CaptureHipsBaseline();

        if (_animator.applyRootMotion)
            _animator.applyRootMotion = false;

        var settings = RecomecoGameplaySettings.Instance;
        var horId = settings != null ? settings.skateHorParam : "Hor";
        var vertId = settings != null ? settings.skateVertParam : "Vert";
        var speedId = settings != null ? settings.skateSpeedParam : "Speed";
        var jumpId = settings != null ? settings.skateJumpParam : "IsJump";
        var pushId = settings != null ? settings.skatePushTrigger : "Push";

        var axis = _mover.Axis;
        var moving = _mover.IsMoving && axis.y > 0.05f;
        var speed = moving ? Mathf.Clamp01(axis.magnitude) * (_mover.IsRun ? 1f : 0.55f) : 0f;

        const float blend = 12f;
        var dt = blend * Time.deltaTime;
        _animator.SetFloat(horId, Mathf.MoveTowards(_animator.GetFloat(horId), 0f, dt));
        _animator.SetFloat(vertId, Mathf.MoveTowards(_animator.GetFloat(vertId), axis.y, dt));
        _animator.SetFloat(speedId, Mathf.MoveTowards(_animator.GetFloat(speedId), speed, dt));
        var jumping = _mover.IsJumping;
        _animator.SetBool(jumpId, jumping);

        if (jumping && !_wasJumping)
            InterruptPushForJump(settings, pushId);

        _pushCooldown -= Time.deltaTime;

        if (!jumping && moving)
        {
            if (!_wasMoving)
                TryPush(pushId, settings);

            var interval = settings != null ? settings.skateCoastPushIntervalSeconds : 3.5f;
            if (interval > 0.05f && _wasMoving)
            {
                _coastPushTimer -= Time.deltaTime;
                if (_coastPushTimer <= 0f)
                {
                    TryPush(pushId, settings);
                    _coastPushTimer = interval;
                }
            }
        }
        else if (!moving)
            _coastPushTimer = 0f;

        _wasMoving = moving;
        _wasJumping = jumping;

        if (settings == null || settings.skateLockHipsHorizontal)
            LockHipsHorizontal();
    }

    void TryPush(string pushId, RecomecoGameplaySettings settings)
    {
        if (_mover != null && _mover.IsJumping)
            return;

        if (_pushCooldown > 0f || Time.time < _mountGraceUntil || string.IsNullOrEmpty(pushId))
            return;

        _animator.SetTrigger(pushId);
        _pushCooldown = settings != null ? settings.skatePushCooldownSeconds : 0.85f;
    }

    void InterruptPushForJump(RecomecoGameplaySettings settings, string pushId)
    {
        if (_animator == null)
            return;

        if (!string.IsNullOrEmpty(pushId))
            _animator.ResetTrigger(pushId);

        var state = settings != null ? settings.skateJumpStateName : "Jump";
        if (string.IsNullOrEmpty(state))
            state = "Jump";

        _animator.CrossFadeInFixedTime(state, 0.07f, 0, 0f);

        var cooldown = settings != null ? settings.skatePushCooldownSeconds : 0.85f;
        _pushCooldown = Mathf.Max(_pushCooldown, cooldown);
        _coastPushTimer = settings != null ? settings.skateCoastPushIntervalSeconds : 3.5f;
    }

    void CaptureHipsBaseline()
    {
        if (_animator == null)
            return;

        var hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
            return;

        _hipsBaseLocal = hips.localPosition;
        _hipsBaseCaptured = true;
    }

    void LockHipsHorizontal()
    {
        if (_animator == null || !_hipsBaseCaptured)
            return;

        var hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hips == null)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        var lift = settings != null ? settings.skateHipsVerticalOffset : 0.06f;

        var p = hips.localPosition;
        p.x = _hipsBaseLocal.x;
        p.z = _hipsBaseLocal.z;
        p.y = _hipsBaseLocal.y + lift;
        hips.localPosition = p;
    }

    void ResolveAnimator()
    {
        foreach (var a in GetComponentsInChildren<Animator>(true))
        {
            if (a == null || !a.isActiveAndEnabled)
                continue;
            if (a.avatar != null && a.avatar.isHuman)
            {
                _animator = a;
                a.applyRootMotion = false;
                return;
            }
        }
    }
}