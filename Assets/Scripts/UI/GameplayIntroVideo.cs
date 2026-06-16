using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Reproduz o vídeo de introdução após escolher Ferro Velho ou Cidade no menu.
/// Espaço pula; ao terminar carrega a cena de gameplay.
/// </summary>
public static class GameplayIntroVideo
{
    const string VideoFileName = "recomeco_intro.mp4";
    const string VideoFolder = "Intro";
    const string ResourcesVideoPath = "Video/recomeco_intro";

    static bool _isPlaying;

    public static bool IsPlaying => _isPlaying;

    public static void PlayThenLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (_isPlaying)
            return;

        var clip = ResolveVideoClip();
        var filePath = GetVideoFilePath();
        var hasFile = !string.IsNullOrEmpty(filePath) && File.Exists(filePath);

        if (clip == null && !hasFile)
        {
            Debug.LogWarning(
                "GameplayIntroVideo: vídeo não encontrado.\n" +
                "• Importe Assets/Resources/Video/recomeco_intro.mp4 no Unity\n" +
                "• Ou use Recomeco → Menu → Configurar vídeo intro\n" +
                "Carregando cena diretamente.");
            SceneManager.LoadScene(sceneName);
            return;
        }

        _isPlaying = true;
        var host = new GameObject(nameof(GameplayIntroVideoRunner));
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.AddComponent<GameplayIntroVideoRunner>().Begin(sceneName, clip, filePath);
    }

    static VideoClip ResolveVideoClip()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && settings.introVideoClip != null)
            return settings.introVideoClip;

        return Resources.Load<VideoClip>(ResourcesVideoPath);
    }

    internal static void MarkFinished()
    {
        _isPlaying = false;
    }

    static string GetVideoFilePath()
    {
        return Path.Combine(Application.streamingAssetsPath, VideoFolder, VideoFileName);
    }

    internal static string BuildFileUrl(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return null;

        absolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/');
        return new Uri(absolutePath).AbsoluteUri;
    }
}

sealed class GameplayIntroVideoRunner : MonoBehaviour
{
    const int OverlaySortOrder = 32760;
    const float PrepareTimeoutSeconds = 25f;

    readonly List<GameObject> _hiddenRoots = new();

    string _targetScene;
    VideoPlayer _player;
    RenderTexture _renderTexture;
    bool _finished;
    string _errorMessage;

    public void Begin(string targetScene, VideoClip clip, string filePath)
    {
        _targetScene = targetScene;
        StartCoroutine(PlayRoutine(clip, filePath));
    }

    IEnumerator PlayRoutine(VideoClip clip, string filePath)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        HideMenuUi();

        var canvasGo = new GameObject("IntroVideoCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

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

        _player = videoGo.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.waitForFirstFrame = true;
        _player.skipOnDrop = false;
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
            Debug.Log("GameplayIntroVideo: reproduzindo '" + clip.name + "' (" + clip.length.ToString("0.0") + "s).");
        }
        else
        {
            var url = GameplayIntroVideo.BuildFileUrl(filePath);
            _player.source = VideoSource.Url;
            _player.url = url;
            Debug.Log("GameplayIntroVideo: reproduzindo URL " + url);
        }

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
            Debug.LogError(
                "GameplayIntroVideo: falha ao preparar o vídeo" +
                (string.IsNullOrEmpty(_errorMessage) ? " (timeout)." : ": " + _errorMessage));
            FinishAndLoad();
            yield break;
        }

        _player.Play();

        while (!_finished)
        {
            if (WasSkipPressed())
                FinishAndLoad();
            yield return null;
        }
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
        hint.text = "Pressione Espaço para pular";
        hint.fontSize = 22f;
        hint.color = new Color(1f, 1f, 1f, 0.9f);
        hint.alignment = TextAlignmentOptions.Center;
        hint.raycastTarget = false;
    }

    static bool WasSkipPressed()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(KeyCode.Space);
    }

    void HideMenuUi()
    {
        _hiddenRoots.Clear();

        foreach (var menuCanvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (menuCanvas == null)
                continue;

            var root = menuCanvas.gameObject;
            if (root.name.Contains("IntroVideo"))
                continue;

            if (!root.activeSelf)
                continue;

            _hiddenRoots.Add(root);
            root.SetActive(false);
        }
    }

    void OnVideoFinished(VideoPlayer _)
    {
        FinishAndLoad();
    }

    void OnVideoError(VideoPlayer _, string message)
    {
        _errorMessage = message;
        Debug.LogError("GameplayIntroVideo: " + message);
    }

    void FinishAndLoad()
    {
        if (_finished)
            return;

        _finished = true;

        if (_player != null)
        {
            _player.loopPointReached -= OnVideoFinished;
            _player.errorReceived -= OnVideoError;
            if (_player.isPlaying)
                _player.Stop();
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        GameplayIntroVideo.MarkFinished();

        if (!string.IsNullOrEmpty(_targetScene) && Application.CanStreamedLevelBeLoaded(_targetScene))
            SceneManager.LoadScene(_targetScene);
        else
            Debug.LogError("GameplayIntroVideo: cena '" + _targetScene + "' indisponível.");

        Destroy(gameObject);
    }

    void OnDestroy()
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
        }

        GameplayIntroVideo.MarkFinished();
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
