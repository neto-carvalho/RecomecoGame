using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Vídeo fullscreen ao dormir na barraca (transição de descanso).
/// </summary>
public static class GameplaySleepVideo
{
    const string ResourcesVideoPath = "Video/carneirinhos_pulando";
    const string StreamingFileName = "carneirinhos_pulando.mp4";
    const string StreamingFolder = "Sleep";

    public static bool HasVideo()
    {
        return ResolveVideoClip() != null || HasStreamingFile();
    }

    public static VideoClip ResolveVideoClip()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.precariousSleepVideoClip != null)
            return settings.precariousSleepVideoClip;

        return Resources.Load<VideoClip>(ResourcesVideoPath);
    }

    static bool HasStreamingFile()
    {
        var path = GetStreamingPath();
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    static string GetStreamingPath()
    {
        return Path.Combine(Application.streamingAssetsPath, StreamingFolder, StreamingFileName);
    }

    public static IEnumerator PlaySleepTransition()
    {
        var clip = ResolveVideoClip();
        var filePath = clip == null && HasStreamingFile() ? GetStreamingPath() : null;
        if (clip == null && string.IsNullOrEmpty(filePath))
            yield break;

        var host = new GameObject(nameof(GameplaySleepVideoRunner));
        var runner = host.AddComponent<GameplaySleepVideoRunner>();
        yield return runner.Run(clip, filePath);
        UnityEngine.Object.Destroy(host);
    }
}

sealed class GameplaySleepVideoRunner : MonoBehaviour
{
    const int OverlaySortOrder = 32755;
    const float PrepareTimeoutSeconds = 20f;

    VideoPlayer _player;
    RenderTexture _renderTexture;
    CanvasGroup _canvasGroup;
    bool _finished;
    string _errorMessage;

    public IEnumerator Run(VideoClip clip, string filePath)
    {
        LockPlayer(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        var settings = RecomecoGameplaySettings.Instance;
        var fadeInVideo = settings != null ? settings.sleepVideoFadeInSeconds : 0.45f;
        var fadeOutVideo = settings != null ? settings.sleepVideoFadeOutSeconds : 0.5f;
        var allowSkip = settings == null || settings.allowSkipSleepVideo;

        var canvasGo = new GameObject("SleepVideoCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        var backdropGo = new GameObject("Backdrop");
        backdropGo.transform.SetParent(canvasGo.transform, false);
        StretchFull(backdropGo.AddComponent<RectTransform>());
        var backdrop = backdropGo.AddComponent<Image>();
        backdrop.color = Color.black;
        backdrop.raycastTarget = false;

        _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        var videoGo = new GameObject("Video");
        videoGo.transform.SetParent(canvasGo.transform, false);
        StretchFull(videoGo.AddComponent<RectTransform>());
        var rawImage = videoGo.AddComponent<RawImage>();
        rawImage.texture = _renderTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;

        var audioSource = videoGo.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = settings != null ? settings.sleepVideoVolume : 0.85f;

        _player = videoGo.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.waitForFirstFrame = true;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.targetTexture = _renderTexture;
        _player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _player.SetTargetAudioSource(0, audioSource);
        _player.loopPointReached += OnVideoFinished;
        _player.errorReceived += OnVideoError;

        if (clip != null)
        {
            _player.source = VideoSource.VideoClip;
            _player.clip = clip;
        }
        else
        {
            _player.source = VideoSource.Url;
            _player.url = BuildFileUrl(filePath);
        }

        if (allowSkip)
            BuildSkipHint(canvasGo.transform);

        var prepared = false;
        _player.prepareCompleted += _ => prepared = true;
        _player.Prepare();

        var elapsed = 0f;
        while (!prepared && string.IsNullOrEmpty(_errorMessage) && elapsed < PrepareTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!prepared)
        {
            Debug.LogWarning("GameplaySleepVideo: não foi possível preparar o vídeo.");
            Cleanup(canvasGo);
            LockPlayer(false);
            yield break;
        }

        _player.Play();

        elapsed = 0f;
        while (elapsed < fadeInVideo)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInVideo);
            yield return null;
        }

        _canvasGroup.alpha = 1f;

        while (!_finished)
        {
            if (allowSkip && WasSkipPressed())
            {
                _finished = true;
                break;
            }

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeOutVideo)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutVideo);
            yield return null;
        }

        _canvasGroup.alpha = 0f;

        if (_player != null && _player.isPlaying)
            _player.Stop();

        Cleanup(canvasGo);
        LockPlayer(false);
    }

    void Cleanup(GameObject canvasGo)
    {
        if (_player != null)
        {
            _player.loopPointReached -= OnVideoFinished;
            _player.errorReceived -= OnVideoError;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (canvasGo != null)
            Destroy(canvasGo);
    }

    static void LockPlayer(bool locked)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        var mover = player.GetComponent<Controller.CharacterMover>();
        if (mover != null)
            mover.enabled = !locked;

        var needs = player.GetComponent<PlayerNeeds>();
        if (needs != null && locked)
            needs.SetFaintLocked(true);
        else if (needs != null)
            needs.SetFaintLocked(false);
    }

    static void BuildSkipHint(Transform parent)
    {
        var hintGo = new GameObject("SkipHint");
        hintGo.transform.SetParent(parent, false);
        var hintRect = hintGo.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 28f);
        hintRect.sizeDelta = new Vector2(900f, 40f);

        var hint = hintGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            hint.font = TMP_Settings.defaultFontAsset;
        hint.text = "Espaço para pular";
        hint.fontSize = 22f;
        hint.color = new Color(1f, 1f, 1f, 0.85f);
        hint.alignment = TextAlignmentOptions.Center;
        hint.raycastTarget = false;
    }

    static bool WasSkipPressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.Space);
    }

    void OnVideoFinished(VideoPlayer _) => _finished = true;

    void OnVideoError(VideoPlayer _, string message)
    {
        _errorMessage = message;
        Debug.LogWarning("GameplaySleepVideo: " + message);
        _finished = true;
    }

    static string BuildFileUrl(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return null;

        absolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/');
        return new Uri(absolutePath).AbsoluteUri;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
