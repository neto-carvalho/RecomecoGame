using Controller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayPauseMenu : MonoBehaviour
{
    const int PauseUiLayoutVersion = 3;

    public static GameplayPauseMenu Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance._isOpen;

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

        if (SellMinigameUI.SuppressPauseThisFrame || SellMinigameUI.IsOpen || ShopUI.IsOpen)
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

    void OnSaveGameClicked()
    {
        if (!SaveGameManager.CanSaveProgress())
        {
            GameplayTipBannerUI.Show(
                "Salvar só depois de comprar a casa. De noite, durma na cama (também salva).",
                5f);
            return;
        }

        if (SaveGameManager.SaveWithPlayerFeedback())
            Resume();
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
        {
            var ver = _panelRoot.GetComponent<PauseMenuUiVersion>();
            if (ver != null && ver.version >= PauseUiLayoutVersion)
                return;

            Destroy(_panelRoot);
            _panelRoot = null;
        }

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
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(420f, 0f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.09f, 0.09f, 0.12f, 0.96f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 32, 32);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddPauseLabel(panel.transform, "PAUSADO", 34, 48, Color.white, FontStyles.Bold);
        AddPauseLabel(panel.transform, "ESC — continuar", 16, 28, new Color(0.75f, 0.75f, 0.8f));

        AddPauseSpacer(panel.transform, 6f);
        LookSensitivityOptionsUI.AddPauseSensitivityRow(panel.transform);
        AddPauseSpacer(panel.transform, 10f);

        CreatePauseButton(panel.transform, "CONTINUAR", new Color(0.15f, 0.4f, 0.2f, 1f), Resume);
        CreatePauseButton(panel.transform, "SALVAR JOGO", new Color(0.18f, 0.28f, 0.45f, 1f), OnSaveGameClicked);
        CreatePauseButton(panel.transform, "MENU INICIAL", new Color(0.45f, 0.15f, 0.15f, 1f), OnReturnToMenuClicked);

        _panelRoot = canvasGo;
        var uiVer = canvasGo.AddComponent<PauseMenuUiVersion>();
        uiVer.version = PauseUiLayoutVersion;
        _panelRoot.SetActive(false);
    }

    static void AddPauseSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
    }

    static void AddPauseLabel(
        Transform parent, string text, float fontSize, float height, Color color,
        FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Text_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
    }

    static void CreatePauseButton(
        Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 52f;
        le.preferredHeight = 52f;

        var img = go.AddComponent<Image>();
        img.color = color;
        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
