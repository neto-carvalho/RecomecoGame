using System;
using System.Collections;
using Controller;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

public class PlayerFaintSequence : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }

    Action _onComplete;
    MovePlayerInput _input;
    CharacterMover _mover;
    CharacterController _controller;
    Animator _animator;
    RuntimeAnimatorController _locomotionController;
    PlayerCamera _camera;
    Behaviour _cameraControlBehaviour;
    Transform _cameraTransform;
    Vector3 _cameraStartPos;
    Quaternion _cameraStartRot;
    Vector3 _fallAnchorXZ;
    float _fallYaw;
    CanvasGroup _fadeGroup;
    FaintTitleOverlay _titleOverlay;

    PlayableGraph _playableGraph;
    AnimationClipPlayable _clipPlayable;
    AnimationClip _fallClip;
    float _fallPlaybackTime;
    bool _useClipPlayable;

    public static void Play(GameObject player, Action onComplete)
    {
        if (player == null)
        {
            onComplete?.Invoke();
            return;
        }

        var existing = player.GetComponent<PlayerFaintSequence>();
        if (existing != null)
            Destroy(existing);

        var sequence = player.AddComponent<PlayerFaintSequence>();
        sequence._onComplete = onComplete;
        sequence.StartCoroutine(sequence.Run());
    }

    IEnumerator Run()
    {
        IsPlaying = true;

        _input = GetComponent<MovePlayerInput>();
        _mover = GetComponent<CharacterMover>();
        _controller = GetComponent<CharacterController>();
        _animator = ResolveAnimator();

        _fallAnchorXZ = transform.position;
        _fallYaw = transform.rotation.eulerAngles.y;

        if (_input != null)
            _input.enabled = false;
        if (_mover != null)
            _mover.enabled = false;
        if (_controller != null)
            _controller.enabled = false;

        transform.rotation = Quaternion.Euler(0f, _fallYaw, 0f);

        ResolveCamera();
        BeginCameraOverride();

        var settings = RecomecoGameplaySettings.Instance;
        _titleOverlay = FaintTitleOverlay.Show(settings);

        var holdSeconds = settings != null ? settings.faintHoldAfterFallSeconds : 1.35f;
        var fadeOut = settings != null ? settings.faintFadeOutSeconds : 0.45f;
        var fallDuration = BeginFallAnimation(settings);
        fallDuration = Mathf.Max(fallDuration, 0.85f);
        if (_fallClip != null)
            fallDuration = _fallClip.length;

        var cameraRiseDuration = fallDuration;
        var elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            StepFallFrame();
            UpdateOverheadCamera(Mathf.Clamp01(elapsed / cameraRiseDuration));
            yield return null;
        }

        FreezeFallAnimation();
        AlignBodyToGround();
        UpdateOverheadCamera(1f);

        elapsed = 0f;
        while (elapsed < holdSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            AlignBodyToGround();
            UpdateOverheadCamera(1f);
            yield return null;
        }

        if (fadeOut > 0.01f)
            yield return FadeOut(fadeOut);

        RestoreLocomotion(settings);
        CleanupFade();
        FaintTitleOverlay.Hide(_titleOverlay);

        IsPlaying = false;
        var callback = _onComplete;
        _onComplete = null;
        Destroy(this);
        callback?.Invoke();
    }

    float BeginFallAnimation(RecomecoGameplaySettings settings)
    {
        ZeroLocomotionParameters();

        if (_animator == null)
            return 1.1f;

        _locomotionController = _animator.runtimeAnimatorController;
        _fallClip = ResolveFaintClip(settings);

        if (_fallClip != null && BeginClipPlayable(_fallClip))
            return _fallClip.length;

        return BeginControllerFallback(settings);
    }

    AnimationClip ResolveFaintClip(RecomecoGameplaySettings settings)
    {
        if (settings != null && settings.faintAnimationClip != null)
            return settings.faintAnimationClip;

        return Resources.Load<AnimationClip>("PlayerFaint/Death_Forward");
    }

    bool BeginClipPlayable(AnimationClip clip)
    {
        StopClipPlayable();

        _animator.applyRootMotion = false;
        _animator.speed = 1f;
        _animator.runtimeAnimatorController = null;
        _animator.Rebind();

        _playableGraph = PlayableGraph.Create("PlayerFaint");
        _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        _clipPlayable = AnimationClipPlayable.Create(_playableGraph, clip);
        _clipPlayable.SetApplyFootIK(false);
        _clipPlayable.SetDuration(clip.length);

        var output = AnimationPlayableOutput.Create(_playableGraph, "FaintOutput", _animator);
        output.SetSourcePlayable(_clipPlayable);

        _playableGraph.Play();
        _useClipPlayable = true;
        _fallPlaybackTime = 0f;

        _clipPlayable.SetTime(0f);
        _playableGraph.Evaluate(0f);
        return true;
    }

    float BeginControllerFallback(RecomecoGameplaySettings settings)
    {
        _useClipPlayable = false;
        var stateName = settings != null && !string.IsNullOrEmpty(settings.faintAnimationState)
            ? settings.faintAnimationState
            : "Death_Forward";

        var faintController = settings != null ? settings.faintAnimatorController : null;
        if (faintController != null)
            _animator.runtimeAnimatorController = faintController;

        _animator.applyRootMotion = false;
        _animator.speed = 1f;
        _animator.Play(stateName, 0, 0f);
        _animator.Update(0f);

        var info = _animator.GetCurrentAnimatorStateInfo(0);
        return info.length > 0.05f ? info.length : 1.25f;
    }

    void StepFallFrame()
    {
        transform.rotation = Quaternion.Euler(0f, _fallYaw, 0f);

        if (_useClipPlayable && _clipPlayable.IsValid())
        {
            _fallPlaybackTime += Time.unscaledDeltaTime;
            _clipPlayable.SetTime(_fallPlaybackTime);
            _playableGraph.Evaluate(0f);
        }
        else if (_animator != null)
        {
            _animator.Update(Time.unscaledDeltaTime);
        }

        AlignBodyToGround();
    }

    void FreezeFallAnimation()
    {
        if (_useClipPlayable && _clipPlayable.IsValid() && _fallClip != null)
        {
            var endTime = Mathf.Max(0f, _fallClip.length - 0.02f);
            _clipPlayable.SetTime(endTime);
            _playableGraph.Evaluate(0f);
            return;
        }

        if (_animator == null)
            return;

        _animator.speed = 0f;
        _animator.applyRootMotion = false;
    }

    void StopClipPlayable()
    {
        _useClipPlayable = false;
        if (_playableGraph.IsValid())
            _playableGraph.Destroy();
    }

    void AlignBodyToGround()
    {
        if (_controller != null)
            _controller.enabled = false;

        transform.position = new Vector3(_fallAnchorXZ.x, transform.position.y, _fallAnchorXZ.z);

        var scale = Mathf.Max(0.12f, transform.lossyScale.y);
        var settings = RecomecoGameplaySettings.Instance;
        var clearanceSetting = settings != null ? settings.faintGroundClearance : 0.12f;
        var clearance = Mathf.Max(0.006f, clearanceSetting * 0.06f * scale);

        var groundY = SampleGroundYAtAnchor();
        var lowestY = GetLowestContactY();
        transform.position += Vector3.up * (groundY + clearance - lowestY);
    }

    float GetLowestContactY()
    {
        var lowest = float.MaxValue;

        if (_animator != null && _animator.isHuman)
        {
            foreach (HumanBodyBones bone in ContactBones)
            {
                var boneTransform = _animator.GetBoneTransform(bone);
                if (boneTransform == null)
                    continue;

                lowest = Mathf.Min(lowest, boneTransform.position.y);
            }
        }

        if (lowest < float.MaxValue)
            return lowest;

        if (TryGetBodyBounds(out var bounds))
            return bounds.min.y;

        return transform.position.y;
    }

    static readonly HumanBodyBones[] ContactBones =
    {
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightFoot,
        HumanBodyBones.LeftToes,
        HumanBodyBones.RightToes,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand,
        HumanBodyBones.Hips,
    };

    float SampleGroundYAtAnchor()
    {
        var scale = Mathf.Max(0.12f, transform.lossyScale.y);
        var probe = new Vector3(_fallAnchorXZ.x, transform.position.y + scale, _fallAnchorXZ.z);
        var origin = probe + Vector3.up * (1.5f * scale);
        var hits = Physics.RaycastAll(origin, Vector3.down, 3f * scale + 2f, ~0, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
            return SpawnGroundUtility.GetGroundPosition(probe).y;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.normal.y < 0.35f)
                continue;

            return hit.point.y;
        }

        return SpawnGroundUtility.GetGroundPosition(probe).y;
    }

    bool TryGetBodyBounds(out Bounds bounds)
    {
        bounds = default;
        var has = false;

        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr == null || !smr.enabled)
                continue;

            if (!has)
            {
                bounds = smr.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(smr.bounds);
            }
        }

        if (has)
            return true;

        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!has)
            {
                bounds = renderer.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return has;
    }

    void SnapFeetFallback()
    {
        var ground = SpawnGroundUtility.GetGroundPosition(transform.position);
        var scale = Mathf.Max(0.12f, transform.lossyScale.y);
        if (_controller != null)
            ground.y += _controller.height * 0.5f - _controller.center.y + 0.02f * scale;
        transform.position = new Vector3(_fallAnchorXZ.x, ground.y, _fallAnchorXZ.z);
    }

    void BeginCameraOverride()
    {
        if (_camera == null)
            return;

        if (_camera is ThirdPersonCamera thirdPerson)
            thirdPerson.RefreshFollowTargets();

        _cameraTransform = _camera.transform;
        _cameraStartPos = _cameraTransform.position;
        _cameraStartRot = _cameraTransform.rotation;

        _cameraControlBehaviour = _camera is ThirdPersonCamera tp ? tp : (Behaviour)_camera;
        if (_cameraControlBehaviour != null)
            _cameraControlBehaviour.enabled = false;

        _camera.ResetFaintDistanceMultiplier();
        UpdateOverheadCamera(0f);
    }

    void UpdateOverheadCamera(float blend)
    {
        if (_cameraTransform == null)
            return;

        var settings = RecomecoGameplaySettings.Instance;
        var scale = Mathf.Max(0.12f, transform.lossyScale.y);
        var pitch = settings != null ? settings.faintOverheadPitch : 88f;
        var heightMul = settings != null ? settings.faintOverheadBodyHeightMultiplier : 3.4f;

        if (!TryGetBodyBounds(out var bounds))
            bounds = new Bounds(transform.position + Vector3.up * 0.2f * scale, Vector3.one * 0.4f * scale);

        var bodyHeight = Mathf.Max(bounds.size.y, 0.25f * scale);
        var height = bodyHeight * heightMul;

        var focus = bounds.center;
        var targetPos = focus + Vector3.up * height;
        var targetRot = Quaternion.Euler(pitch, _fallYaw, 0f);

        blend = SmoothStep(Mathf.Clamp01(blend));
        _cameraTransform.SetPositionAndRotation(
            Vector3.Lerp(_cameraStartPos, targetPos, blend),
            Quaternion.Slerp(_cameraStartRot, targetRot, blend));
    }

    void EndCameraOverride()
    {
        if (_cameraControlBehaviour != null)
            _cameraControlBehaviour.enabled = true;

        if (_camera != null)
            _camera.ResetFaintDistanceMultiplier();
    }

    void OnDestroy()
    {
        if (IsPlaying)
            IsPlaying = false;

        StopClipPlayable();
        FaintTitleOverlay.Hide(_titleOverlay);
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);

    void ResolveCamera()
    {
        if (_input != null)
        {
            _input.RefreshCameraBinding();
            _camera = _input.PlayerCamera;
        }

        if (_camera == null)
            _camera = FindFirstObjectByType<PlayerCamera>();
    }

    void ZeroLocomotionParameters()
    {
        if (_animator == null)
            return;

        _animator.SetFloat("Hor", 0f);
        _animator.SetFloat("Vert", 0f);
        _animator.SetFloat("State", 0f);
        _animator.SetBool("IsJump", false);
    }

    Animator ResolveAnimator()
    {
        Animator best = null;
        var bestScore = -1;
        foreach (var a in GetComponentsInChildren<Animator>(true))
        {
            var score = 0;
            if (a.isActiveAndEnabled)
                score += 4;
            if (a.avatar != null && a.avatar.isHuman)
                score += 8;
            if (a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                score += 10;
            if (score > bestScore)
            {
                bestScore = score;
                best = a;
            }
        }

        return best;
    }

    void RestoreLocomotion(RecomecoGameplaySettings settings)
    {
        EndCameraOverride();
        StopClipPlayable();

        if (_animator != null)
        {
            _animator.speed = 1f;
            _animator.applyRootMotion = false;
            PlayerAnimatorSetup.Apply(gameObject, settings);
            PlayerAnimatorSetup.RefreshLocomotion(gameObject);
        }

        transform.rotation = Quaternion.Euler(0f, _fallYaw, 0f);
        SnapFeetFallback();

        if (_controller != null)
            _controller.enabled = true;
        if (_mover != null)
            _mover.enabled = true;
        if (_input != null)
            _input.enabled = true;
    }

    IEnumerator FadeOut(float duration)
    {
        EnsureFadeOverlay();
        if (_fadeGroup == null)
            yield break;

        _fadeGroup.alpha = 0f;
        _fadeGroup.gameObject.SetActive(true);

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        _fadeGroup.alpha = 1f;
    }

    void EnsureFadeOverlay()
    {
        if (_fadeGroup != null)
            return;

        var canvasGo = new GameObject("FaintFadeOverlay");
        DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("Fade");
        imageGo.transform.SetParent(canvasGo.transform, false);
        var rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        _fadeGroup = canvasGo.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
    }

    void CleanupFade()
    {
        if (_fadeGroup == null)
            return;

        var root = _fadeGroup.gameObject;
        _fadeGroup = null;
        if (root != null)
            Destroy(root);
    }
}

sealed class FaintTitleOverlay
{
    GameObject _root;
    CanvasGroup _group;

    public static FaintTitleOverlay Show(RecomecoGameplaySettings settings)
    {
        var overlay = new FaintTitleOverlay();
        overlay.Create(settings);
        return overlay;
    }

    public static void Hide(FaintTitleOverlay overlay)
    {
        overlay?.Destroy();
    }

    void Create(RecomecoGameplaySettings settings)
    {
        var fontSize = settings != null ? settings.faintTitleFontSize : 76f;

        _root = new GameObject("FaintTitleOverlay");
        UnityEngine.Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 480;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        _group = _root.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;

        var textGo = new GameObject("Title");
        textGo.transform.SetParent(_root.transform, false);
        var rect = textGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 40f);
        rect.sizeDelta = new Vector2(1200f, 200f);

        var text = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.text = "Você desmaiou";
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.78f, 0.12f, 0.14f, 1f);
        text.characterSpacing = 4f;
        text.outlineWidth = 0.35f;
        text.outlineColor = new Color(0.05f, 0.02f, 0.02f, 0.95f);
        text.raycastTarget = false;

        _root.AddComponent<FaintTitleFadeIn>().Begin(_group);
    }

    void Destroy()
    {
        if (_root != null)
            UnityEngine.Object.Destroy(_root);
        _root = null;
    }
}

sealed class FaintTitleFadeIn : MonoBehaviour
{
    CanvasGroup _group;
    float _t;

    public void Begin(CanvasGroup group)
    {
        _group = group;
        _t = 0f;
    }

    void Update()
    {
        if (_group == null)
        {
            Destroy(this);
            return;
        }

        _t += Time.unscaledDeltaTime;
        _group.alpha = Mathf.Clamp01(_t / 0.35f);
        if (_group.alpha >= 1f)
            enabled = false;
    }
}
