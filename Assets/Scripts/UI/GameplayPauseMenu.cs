using Controller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de pausa (ESC) durante gameplay: continuar ou voltar ao menu inicial.
/// </summary>
public class GameplayPauseMenu : MonoBehaviour
{
    public static GameplayPauseMenu Instance { get; private set; }

    bool _isOpen;
    GameObject _panelRoot;

    GameObject _player;
    MovePlayerInput _playerInput;
    CharacterMover _playerMover;
    PlayerCamera _playerCamera;

    CursorLockMode _prevLock;
    bool _prevCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExists()
    {
        if (Instance != null)
            return;

        var go = new GameObject(nameof(GameplayPauseMenu));
        DontDestroyOnLoad(go);
        go.AddComponent<GameplayPauseMenu>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (RecomecoSceneNames.IsMenuScene(scene))
            ForceCloseIfOpen();
    }

    void Update()
    {
        if (RecomecoSceneNames.IsMenuScene(SceneManager.GetActiveScene()))
            return;

        if (SellMinigameUI.SuppressPauseThisFrame || SellMinigameUI.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    public static void ForceCloseIfOpen()
    {
        if (Instance == null)
            return;

        Instance.ForceClose();
    }

    void Toggle()
    {
        if (!_isOpen && (SellMinigameUI.IsOpen || SellMinigameUI.SuppressPauseThisFrame))
            return;

        if (_isOpen)
            Resume();
        else
            Open();
    }

    void Open()
    {
        if (_isOpen)
            return;

        _isOpen = true;
        Time.timeScale = 0f;

        CachePlayer();
        SetPlayerControlEnabled(false);

        _prevLock = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        BuildUiIfNeeded();
        _panelRoot.SetActive(true);
    }

    void Resume()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        Time.timeScale = 1f;

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        SetPlayerControlEnabled(true);
        Cursor.lockState = _prevLock;
        Cursor.visible = _prevCursorVisible;
    }

    void ForceClose()
    {
        if (!_isOpen)
            return;

        _isOpen = false;
        Time.timeScale = 1f;

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        SetPlayerControlEnabled(true);
    }

    void CachePlayer()
    {
        _player = PlayerScenePersistence.TravelingPlayer;
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player");

        if (_player == null)
            return;

        _playerInput = _player.GetComponent<MovePlayerInput>();
        _playerMover = _player.GetComponent<CharacterMover>();
        _playerCamera = FindFirstObjectByType<PlayerCamera>();
    }

    void SetPlayerControlEnabled(bool enabled)
    {
        if (_playerInput != null)
            _playerInput.enabled = enabled;
        if (_playerMover != null)
            _playerMover.enabled = enabled;
        if (_playerCamera != null)
            _playerCamera.enabled = enabled;

        if (!enabled && _player != null)
        {
            var animator = _player.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Float &&
                        (p.name == "Hor" || p.name == "Vert"))
                        animator.SetFloat(p.name, 0f);
                }
            }
        }
    }

    void OnReturnToMenuClicked()
    {
        GameplayReturnToMenu.GoToMainMenu();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void BuildUiIfNeeded()
    {
        if (_panelRoot != null)
            return;

        var canvasGo = new GameObject("Canvas_PauseMenu");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(canvasGo.transform, false);
        var overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImg.raycastTarget = true;

        var panel = new GameObject("Panel");
        panel.transform.SetParent(overlay.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(420f, 300f);
        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.09f, 0.09f, 0.12f, 0.96f);

        CreateText(panelRect, "PAUSADO", 32f, new Vector2(0f, 96f), new Vector2(380f, 44f),
            TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        CreateText(panelRect, "ESC — continuar", 16f, new Vector2(0f, 58f), new Vector2(380f, 28f),
            TextAlignmentOptions.Center, new Color(0.75f, 0.75f, 0.8f));

        CreateButton(panelRect, "CONTINUAR", new Vector2(0f, 4f), new Vector2(300f, 52f),
            new Color(0.15f, 0.4f, 0.2f, 1f), Resume);
        CreateButton(panelRect, "MENU INICIAL", new Vector2(0f, -64f), new Vector2(300f, 52f),
            new Color(0.45f, 0.15f, 0.15f, 1f), OnReturnToMenuClicked);

        _panelRoot = canvasGo;
        _panelRoot.SetActive(false);
    }

    static TextMeshProUGUI CreateText(
        RectTransform parent, string text, float size, Vector2 pos, Vector2 dims,
        TextAlignmentOptions align, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = dims;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    void CreateButton(
        RectTransform parent, string label, Vector2 pos, Vector2 size, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = color;
        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        CreateText(rect, label, 22f, Vector2.zero, size,
            TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
    }
}
