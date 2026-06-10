using System.Collections.Generic;
using Controller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Minigame de venda na rua: arraste um item do inventário para o slot de oferta,
/// escolha a quantidade e pare a barra de precisão na zona verde.
/// Quanto maior a quantidade, mais rápida a barra (mais difícil).
/// </summary>
public class SellMinigameUI : MonoBehaviour
{
    // ----- Ajustes de dificuldade -----
    const float BaseSpeed = 0.7f;           // ciclos por segundo com 1 unidade
    const float SpeedPerExtraUnit = 0.2f;   // acréscimo por unidade extra
    const float MaxSpeed = 2.8f;
    const float BaseZoneHalfWidth = 0.11f;  // metade da zona verde (fração da barra)
    const float MinZoneHalfWidth = 0.06f;
    const float ZoneShrinkPerUnit = 0.004f;

    enum State { PickItem, Ready, Running, Finished }

    public static bool IsOpen => _instance != null;
    public static bool SuppressPauseThisFrame { get; private set; }

    static SellMinigameUI _instance;

    SidewalkNpcWalker _npc;
    GameObject _player;
    Inventory _inventory;

    State _state;
    ItemData _selectedItem;
    int _quantity = 1;
    int _availableCount;
    float _barTime;
    float _barSpeed;
    float _zoneHalfWidth;
    float _closeTimer;

    // UI
    Canvas _canvas;
    Image _offerIcon;
    TextMeshProUGUI _offerHint;
    TextMeshProUGUI _quantityLabel;
    TextMeshProUGUI _speedLabel;
    TextMeshProUGUI _feedback;
    GameObject _startButton;
    RectTransform _barMarker;
    RectTransform _barZone;
    GameObject _barRoot;

    CursorLockMode _prevLock;
    bool _prevCursorVisible;
    PlayerCamera _pausedCamera;

    public static void ForceCloseIfOpen()
    {
        if (_instance != null)
            _instance.Close();
    }

    public static void Open(SidewalkNpcWalker npc, GameObject player)
    {
        if (_instance != null || player == null)
            return;

        var go = new GameObject("SellMinigame");
        _instance = go.AddComponent<SellMinigameUI>();
        _instance.Initialize(npc, player);
    }

    void Initialize(SidewalkNpcWalker npc, GameObject player)
    {
        _npc = npc;
        _player = player;
        _inventory = player.GetComponent<Inventory>();
        if (_inventory == null)
            _inventory = FindFirstObjectByType<Inventory>();

        if (_npc != null)
            _npc.PauseForInteraction(player.transform);

        SetPlayerControlEnabled(false);

        _prevLock = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        BuildUi();
        _state = State.PickItem;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        if (_npc != null)
            _npc.ResumeFromInteraction();

        SetPlayerControlEnabled(true);
        Cursor.lockState = _prevLock;
        Cursor.visible = _prevCursorVisible;
    }

    void SetPlayerControlEnabled(bool enabled)
    {
        if (_player == null)
            return;

        var input = _player.GetComponent<MovePlayerInput>();
        if (input != null)
            input.enabled = enabled;

        var mover = _player.GetComponent<CharacterMover>();
        if (mover != null)
            mover.enabled = enabled;

        if (!enabled)
        {
            _pausedCamera = FindFirstObjectByType<PlayerCamera>();
            if (_pausedCamera != null)
                _pausedCamera.enabled = false;
        }
        else if (_pausedCamera != null)
        {
            _pausedCamera.enabled = true;
            _pausedCamera = null;
        }

        if (!enabled)
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

    void Update()
    {
        if (_state == State.Finished)
        {
            _closeTimer -= Time.deltaTime;
            if (_closeTimer <= 0f)
                Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SuppressPauseThisFrame = true;
            Close();
            return;
        }

        if (_state == State.Ready && Input.GetKeyDown(KeyCode.Space))
            StartBar();
        else if (_state == State.Running)
        {
            _barTime += Time.deltaTime * _barSpeed;
            var pos = Mathf.PingPong(_barTime, 1f);
            SetMarkerPosition(pos);

            if (Input.GetKeyDown(KeyCode.Space))
                ResolveAttempt(pos);
        }
    }

    void LateUpdate()
    {
        SuppressPauseThisFrame = false;
    }

    void Close()
    {
        Destroy(gameObject);
    }

    // ----- Lógica do jogo -----

    void SelectItem(ItemData item)
    {
        if (_state == State.Running || item == null)
            return;

        _selectedItem = item;
        _availableCount = _inventory != null ? _inventory.GetItemCount(item.itemName) : 0;
        _quantity = Mathf.Clamp(_quantity, 1, Mathf.Max(1, _availableCount));

        _offerIcon.sprite = item.icon;
        _offerIcon.enabled = item.icon != null;
        _offerHint.text = item.itemName + "\n" + MoneyManager.FormatBRL(item.unitSellPriceCents) + "/un";

        _state = State.Ready;
        _startButton.SetActive(true);
        RefreshQuantityUi();
        _feedback.text = "";
    }

    void ChangeQuantity(int delta)
    {
        if (_state != State.Ready)
            return;

        _quantity = Mathf.Clamp(_quantity + delta, 1, Mathf.Max(1, _availableCount));
        RefreshQuantityUi();
    }

    void RefreshQuantityUi()
    {
        _quantityLabel.text = "x" + _quantity + " / " + _availableCount;
        var speed = GetSpeedForQuantity(_quantity);
        _speedLabel.text = "Velocidade da barra: " + speed.ToString("0.0") + "x";
    }

    static float GetSpeedForQuantity(int quantity)
    {
        return Mathf.Min(MaxSpeed, BaseSpeed + SpeedPerExtraUnit * (quantity - 1));
    }

    void StartBar()
    {
        if (_selectedItem == null || _inventory == null)
            return;

        _availableCount = _inventory.GetItemCount(_selectedItem.itemName);
        if (_availableCount <= 0)
        {
            _feedback.text = "Você não tem esse item.";
            _state = State.PickItem;
            return;
        }

        _quantity = Mathf.Clamp(_quantity, 1, _availableCount);
        _barSpeed = GetSpeedForQuantity(_quantity);
        _zoneHalfWidth = Mathf.Max(MinZoneHalfWidth, BaseZoneHalfWidth - ZoneShrinkPerUnit * (_quantity - 1));
        _barTime = 0f;

        _barZone.anchorMin = new Vector2(0.5f - _zoneHalfWidth, 0f);
        _barZone.anchorMax = new Vector2(0.5f + _zoneHalfWidth, 1f);

        _barRoot.SetActive(true);
        _startButton.SetActive(false);
        _feedback.text = "Aperte ESPAÇO na zona verde!";
        _feedback.color = Color.white;
        _state = State.Running;
    }

    void ResolveAttempt(float pos)
    {
        var hit = Mathf.Abs(pos - 0.5f) <= _zoneHalfWidth;
        _state = State.Finished;

        if (hit)
        {
            var removed = _inventory.RemoveItem(_selectedItem.itemName, _quantity);
            var total = removed * _selectedItem.unitSellPriceCents;
            if (MoneyManager.instance != null)
                MoneyManager.instance.AddMoney(total);

            _feedback.text = "VENDIDO! " + removed + "x " + _selectedItem.itemName +
                             "  +" + MoneyManager.FormatBRL(total);
            _feedback.color = new Color(0.35f, 1f, 0.45f);
            _closeTimer = 1.4f;
        }
        else
        {
            _feedback.text = "Errou o tempo! O pedestre foi embora...";
            _feedback.color = new Color(1f, 0.4f, 0.35f);
            _closeTimer = 1.4f;
        }
    }

    void SetMarkerPosition(float normalized)
    {
        _barMarker.anchorMin = new Vector2(normalized, 0f);
        _barMarker.anchorMax = new Vector2(normalized, 1f);
        _barMarker.anchoredPosition = Vector2.zero;
    }

    // ----- Construção da UI -----

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("Canvas_SellMinigame");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 300;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = CreatePanel(canvasGo.transform, new Vector2(620f, 460f),
            new Color(0.09f, 0.09f, 0.12f, 0.96f));

        CreateText(panel, "VENDER NA RUA", 28f, new Vector2(0f, 200f), new Vector2(560f, 40f),
            TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        CreateText(panel, "Arraste (ou clique) um item para o slot de venda", 17f,
            new Vector2(0f, 166f), new Vector2(560f, 28f),
            TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));

        BuildItemRow(panel);
        BuildOfferAndQuantity(panel);
        BuildBar(panel);

        _feedback = CreateText(panel, "", 20f, new Vector2(0f, -178f), new Vector2(560f, 34f),
            TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

        CreateButton(panel, "X", new Vector2(282f, 202f), new Vector2(36f, 36f),
            new Color(0.45f, 0.15f, 0.15f, 1f), () => Close());
    }

    void BuildItemRow(RectTransform panel)
    {
        var sellable = GetSellableEntries();

        var row = new GameObject("Items");
        row.transform.SetParent(panel, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0f, 100f);
        rowRect.sizeDelta = new Vector2(560f, 90f);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (sellable.Count == 0)
        {
            CreateText(panel, "Inventário sem itens para vender — compre na Lojinha.", 18f,
                new Vector2(0f, 100f), new Vector2(560f, 40f),
                TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.4f));
            return;
        }

        foreach (var (item, count) in sellable)
        {
            var slot = new GameObject(item.itemName);
            slot.transform.SetParent(row.transform, false);
            var slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(78f, 78f);
            var bg = slot.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slot.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(7f, 7f);
            iconRect.offsetMax = new Vector2(-7f, -7f);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            CreateText(slotRect, count.ToString(), 16f, new Vector2(-6f, 6f), new Vector2(34f, 22f),
                TextAlignmentOptions.BottomRight, Color.white, FontStyles.Bold,
                anchor: new Vector2(1f, 0f));

            var drag = slot.AddComponent<SellMinigameDragItem>();
            drag.Setup(this, item);
        }
    }

    void BuildOfferAndQuantity(RectTransform panel)
    {
        var offer = new GameObject("OfferSlot");
        offer.transform.SetParent(panel, false);
        var offerRect = offer.AddComponent<RectTransform>();
        offerRect.anchoredPosition = new Vector2(-180f, 0f);
        offerRect.sizeDelta = new Vector2(96f, 96f);
        var offerBg = offer.AddComponent<Image>();
        offerBg.color = new Color(0.16f, 0.3f, 0.18f, 1f);
        offer.AddComponent<SellMinigameDropSlot>().Setup(this);

        var offerIconGo = new GameObject("Icon");
        offerIconGo.transform.SetParent(offer.transform, false);
        var offerIconRect = offerIconGo.AddComponent<RectTransform>();
        offerIconRect.anchorMin = Vector2.zero;
        offerIconRect.anchorMax = Vector2.one;
        offerIconRect.offsetMin = new Vector2(8f, 8f);
        offerIconRect.offsetMax = new Vector2(-8f, -8f);
        _offerIcon = offerIconGo.AddComponent<Image>();
        _offerIcon.enabled = false;
        _offerIcon.preserveAspect = true;
        _offerIcon.raycastTarget = false;

        _offerHint = CreateText(panel, "solte o\nitem aqui", 15f, new Vector2(-180f, -70f),
            new Vector2(140f, 44f), TextAlignmentOptions.Center, new Color(0.75f, 0.85f, 0.75f));

        CreateButton(panel, "-", new Vector2(-40f, 0f), new Vector2(44f, 44f),
            new Color(0.25f, 0.25f, 0.32f, 1f), () => ChangeQuantity(-1));
        _quantityLabel = CreateText(panel, "x1 / 0", 21f, new Vector2(50f, 0f), new Vector2(120f, 36f),
            TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        CreateButton(panel, "+", new Vector2(140f, 0f), new Vector2(44f, 44f),
            new Color(0.25f, 0.25f, 0.32f, 1f), () => ChangeQuantity(1));

        _speedLabel = CreateText(panel, "Velocidade da barra: -", 16f, new Vector2(50f, -42f),
            new Vector2(300f, 26f), TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.85f));

        _startButton = CreateButton(panel, "INICIAR VENDA (Espaço)", new Vector2(0f, -88f),
            new Vector2(300f, 46f), new Color(0.15f, 0.4f, 0.2f, 1f), StartBar);
        _startButton.SetActive(false);
    }

    void BuildBar(RectTransform panel)
    {
        _barRoot = new GameObject("PrecisionBar");
        _barRoot.transform.SetParent(panel, false);
        var barRect = _barRoot.AddComponent<RectTransform>();
        barRect.anchoredPosition = new Vector2(0f, -140f);
        barRect.sizeDelta = new Vector2(460f, 28f);
        var barBg = _barRoot.AddComponent<Image>();
        barBg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var zoneGo = new GameObject("TargetZone");
        zoneGo.transform.SetParent(_barRoot.transform, false);
        _barZone = zoneGo.AddComponent<RectTransform>();
        _barZone.anchorMin = new Vector2(0.39f, 0f);
        _barZone.anchorMax = new Vector2(0.61f, 1f);
        _barZone.offsetMin = Vector2.zero;
        _barZone.offsetMax = Vector2.zero;
        var zoneImg = zoneGo.AddComponent<Image>();
        zoneImg.color = new Color(0.2f, 0.75f, 0.3f, 0.9f);
        zoneImg.raycastTarget = false;

        var markerGo = new GameObject("Marker");
        markerGo.transform.SetParent(_barRoot.transform, false);
        _barMarker = markerGo.AddComponent<RectTransform>();
        _barMarker.sizeDelta = new Vector2(7f, 8f);
        var markerImg = markerGo.AddComponent<Image>();
        markerImg.color = Color.white;
        markerImg.raycastTarget = false;
        SetMarkerPosition(0f);

        _barRoot.SetActive(false);
    }

    List<(ItemData item, int count)> GetSellableEntries()
    {
        var result = new List<(ItemData, int)>();
        if (_inventory == null || _inventory.slots == null)
            return result;

        var seen = new HashSet<ItemData>();
        foreach (var slot in _inventory.slots)
        {
            if (slot == null || slot.IsEmpty() || slot.item == null)
                continue;
            if (slot.item.unitSellPriceCents <= 0 || !seen.Add(slot.item))
                continue;

            result.Add((slot.item, _inventory.GetItemCount(slot.item.itemName)));
            if (result.Count >= 6)
                break;
        }

        return result;
    }

    // ----- Helpers de UI -----

    static RectTransform CreatePanel(Transform parent, Vector2 size, Color color)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return rect;
    }

    static TextMeshProUGUI CreateText(
        RectTransform parent, string text, float size, Vector2 pos, Vector2 dims,
        TextAlignmentOptions align, Color color, FontStyles style = FontStyles.Normal,
        Vector2? anchor = null)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        if (anchor.HasValue)
        {
            rect.anchorMin = anchor.Value;
            rect.anchorMax = anchor.Value;
            rect.pivot = anchor.Value;
        }
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

    GameObject CreateButton(
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

        CreateText(rect, label, Mathf.Min(20f, size.y * 0.45f), Vector2.zero, size,
            TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        return go;
    }

    // ----- Drag & drop -----

    /// <summary>Ícone arrastável da fileira de itens.</summary>
    class SellMinigameDragItem : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        SellMinigameUI _owner;
        ItemData _item;
        GameObject _ghost;

        public void Setup(SellMinigameUI owner, ItemData item)
        {
            _owner = owner;
            _item = item;
        }

        public ItemData Item => _item;

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner.SelectItem(_item);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _ghost = new GameObject("DragGhost");
            _ghost.transform.SetParent(_owner._canvas.transform, false);
            var rect = _ghost.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64f, 64f);
            var img = _ghost.AddComponent<Image>();
            img.sprite = _item.icon;
            img.enabled = _item.icon != null;
            img.preserveAspect = true;
            img.raycastTarget = false;
            _ghost.transform.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null)
                _ghost.transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null)
                Destroy(_ghost);
        }
    }

    /// <summary>Slot de oferta: recebe o item arrastado.</summary>
    class SellMinigameDropSlot : MonoBehaviour, IDropHandler
    {
        SellMinigameUI _owner;

        public void Setup(SellMinigameUI owner)
        {
            _owner = owner;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            var dragged = eventData.pointerDrag.GetComponent<SellMinigameDragItem>();
            if (dragged != null)
                _owner.SelectItem(dragged.Item);
        }
    }
}
