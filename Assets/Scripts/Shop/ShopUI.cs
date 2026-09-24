using System.Collections.Generic;
using Controller;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static bool IsOpen => _instance != null;

    static ShopUI _instance;

    ShopZone _shop;
    GameObject _player;
    Inventory _inventory;

    TextMeshProUGUI _moneyText;
    TextMeshProUGUI _feedbackText;
    TextMeshProUGUI _cartTotalText;
    TextMeshProUGUI _cartAfterText;
    Transform _catalogRoot;
    Transform _cartRoot;
    Transform _inventoryRoot;

    readonly Dictionary<int, int> _cart = new Dictionary<int, int>();

    CursorLockMode _prevLock;
    bool _prevCursorVisible;
    GameObject _ownedEventSystem;

    public static void ForceCloseIfOpen()
    {
        _instance?.Close();
    }

    public static void Open(ShopZone shop, GameObject player)
    {
        if (shop == null || player == null)
            return;

        if (_instance != null)
            ForceCloseIfOpen();

        GameplayPauseMenu.ForceCloseIfOpen();
        SellMinigameUI.ForceCloseIfOpen();

        var go = new GameObject("ShopUI");
        _instance = go.AddComponent<ShopUI>();
        _instance.Initialize(shop, player);
    }

    void Initialize(ShopZone shop, GameObject player)
    {
        _shop = shop;
        _player = player;
        _inventory = player.GetComponent<Inventory>();
        if (_inventory == null)
            _inventory = FindFirstObjectByType<Inventory>();

        SetPlayerControlEnabled(false);
        _prevLock = Cursor.lockState;
        _prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();
        BuildUi();
        RefreshAll();
    }

    void OnDestroy()
    {
        SetPlayerControlEnabled(true);
        if (_ownedEventSystem != null)
            Destroy(_ownedEventSystem);

        Cursor.lockState = _prevLock;
        Cursor.visible = _prevCursorVisible;

        if (_instance == this)
            _instance = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Close()
    {
        Destroy(gameObject);
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
    }

    void RefreshAll()
    {
        RefreshMoney();
        RebuildCatalog();
        RebuildCart();
        RebuildInventoryPanel();
    }

    void RefreshMoney()
    {
        if (_moneyText == null)
            return;

        var cents = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0;
        _moneyText.text = "Saldo: " + MoneyManager.FormatBRL(cents);
    }

    void ShowFeedback(string message)
    {
        if (_feedbackText != null)
            _feedbackText.text = message;
    }

    int GetCartTotalCents()
    {
        if (_shop == null || _shop.products == null)
            return 0;

        var total = 0;
        foreach (var pair in _cart)
        {
            if (pair.Value <= 0 || pair.Key < 0 || pair.Key >= _shop.products.Length)
                continue;

            var product = _shop.products[pair.Key];
            if (product == null)
                continue;

            total += product.packPriceCents * pair.Value;
        }

        return total;
    }

    void RebuildCatalog()
    {
        if (_catalogRoot == null || _shop == null)
            return;

        ClearChildren(_catalogRoot);

        var products = _shop.products;
        if (products == null || products.Length == 0)
        {
            CreateBodyLabel(_catalogRoot, "Nenhum produto configurado.", 16, FontStyles.Italic);
            return;
        }

        for (var i = 0; i < products.Length; i++)
        {
            var product = products[i];
            if (product == null || product.item == null)
                continue;

            CreateProductRow(i, product);
        }
    }

    void CreateProductRow(int productIndex, ShopZone.ShopProduct product)
    {
        var row = CreatePanel(_catalogRoot, new Color(0.12f, 0.12f, 0.14f, 0.92f), 108f);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(10, 10, 10, 10);
        h.spacing = 10f;
        h.childAlignment = TextAnchor.UpperLeft;
        h.childControlWidth = false;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;

        CreateIcon(row, product.item.icon, 56f);

        var textCol = new GameObject("Texts");
        textCol.transform.SetParent(row, false);
        var textLayout = textCol.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.minWidth = 160f;
        textLayout.preferredHeight = 88f;
        textLayout.minHeight = 88f;
        var v = textCol.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        CreateBodyLabel(textCol.transform, product.DisplayName, 19, FontStyles.Bold);

        var priceLine = MoneyManager.FormatBRL(product.packPriceCents);
        if (product.unitsPerPack > 1)
            priceLine += " · pacote " + product.unitsPerPack + " un";
        CreateBodyLabel(textCol.transform, priceLine, 15, FontStyles.Normal);

        var effect = product.item.GetEffectHint();
        if (!string.IsNullOrEmpty(effect))
            CreateBodyLabel(textCol.transform, effect, 14, FontStyles.Italic, new Color(0.75f, 0.9f, 1f));

        var btnCol = new GameObject("Actions");
        btnCol.transform.SetParent(row, false);
        var btnLe = btnCol.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 52f;
        btnLe.minWidth = 52f;
        var btnV = btnCol.AddComponent<VerticalLayoutGroup>();
        btnV.spacing = 6f;
        btnV.childAlignment = TextAnchor.MiddleCenter;

        var addBtn = CreateButton(btnCol.transform, "+", 44f, 40f);
        addBtn.onClick.AddListener(() =>
        {
            AddToCart(productIndex);
            ShowFeedback("Adicionado: " + product.DisplayName);
        });
    }

    void AddToCart(int productIndex)
    {
        if (_cart.TryGetValue(productIndex, out var qty))
            _cart[productIndex] = qty + 1;
        else
            _cart[productIndex] = 1;

        RebuildCart();
        RefreshMoney();
    }

    void RemoveFromCart(int productIndex)
    {
        if (!_cart.TryGetValue(productIndex, out var qty))
            return;

        qty--;
        if (qty <= 0)
            _cart.Remove(productIndex);
        else
            _cart[productIndex] = qty;

        RebuildCart();
        RefreshMoney();
    }

    void ClearCart()
    {
        _cart.Clear();
        RebuildCart();
        ShowFeedback("Carrinho limpo.");
    }

    void RebuildCart()
    {
        if (_cartRoot == null)
            return;

        ClearChildren(_cartRoot);

        if (_cart.Count == 0)
        {
            CreateBodyLabel(_cartRoot, "Toque em + no cardápio para adicionar itens.", 15, FontStyles.Italic);
        }
        else if (_shop != null && _shop.products != null)
        {
            foreach (var pair in _cart)
            {
                if (pair.Key < 0 || pair.Key >= _shop.products.Length)
                    continue;

                var product = _shop.products[pair.Key];
                if (product == null || product.item == null)
                    continue;

                CreateCartRow(pair.Key, product, pair.Value);
            }
        }

        var total = GetCartTotalCents();
        if (_cartTotalText != null)
            _cartTotalText.text = "Total: " + MoneyManager.FormatBRL(total);

        if (_cartAfterText != null)
        {
            var balance = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0;
            var after = balance - total;
            _cartAfterText.text = after >= 0
                ? "Saldo após compra: " + MoneyManager.FormatBRL(after)
                : "Faltam " + MoneyManager.FormatBRL(-after);
            _cartAfterText.color = after >= 0
                ? new Color(0.7f, 0.95f, 0.75f)
                : new Color(1f, 0.55f, 0.5f);
        }
    }

    void CreateCartRow(int productIndex, ShopZone.ShopProduct product, int quantity)
    {
        var row = CreatePanel(_cartRoot, new Color(0.1f, 0.11f, 0.13f, 0.95f), 64f);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(8, 8, 6, 6);
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;

        CreateIcon(row, product.item.icon, 44f);

        var textCol = new GameObject("Texts");
        textCol.transform.SetParent(row, false);
        var flex = textCol.AddComponent<LayoutElement>();
        flex.flexibleWidth = 1f;
        var v = textCol.AddComponent<VerticalLayoutGroup>();
        v.spacing = 2f;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;

        CreateBodyLabel(textCol.transform, product.DisplayName, 16, FontStyles.Bold);
        var sub = quantity + " × " + MoneyManager.FormatBRL(product.packPriceCents) + " = " +
                  MoneyManager.FormatBRL(product.packPriceCents * quantity);
        CreateBodyLabel(textCol.transform, sub, 14, FontStyles.Normal);

        var minusBtn = CreateButton(row, "−", 40f, 36f);
        minusBtn.onClick.AddListener(() => RemoveFromCart(productIndex));
    }

    void TryCheckout()
    {
        if (_shop == null)
        {
            ShowFeedback("Loja indisponível.");
            return;
        }

        if (_cart.Count == 0)
        {
            ShowFeedback("Adicione itens ao carrinho antes de pagar.");
            return;
        }

        if (!_shop.TryCheckout(_cart, _player, out var message))
        {
            ShowFeedback(message);
            return;
        }

        _cart.Clear();
        ShowFeedback(string.IsNullOrEmpty(message) ? string.Empty : message);
        RefreshAll();
    }

    void RebuildInventoryPanel()
    {
        if (_inventoryRoot == null)
            return;

        ClearChildren(_inventoryRoot);

        if (_inventory == null || _inventory.slots == null)
        {
            CreateBodyLabel(_inventoryRoot, "Sem inventário.", 16, FontStyles.Italic);
            return;
        }

        var any = false;
        for (var i = 0; i < _inventory.slots.Length; i++)
        {
            var slot = _inventory.slots[i];
            if (slot == null || slot.IsEmpty() || slot.item == null)
                continue;

            any = true;
            CreateInventoryRow(slot.item, slot.quantity);
        }

        if (!any)
            CreateBodyLabel(_inventoryRoot, "Inventário vazio.\nUse + no cardápio e Pagar.", 15, FontStyles.Italic);
    }

    void CreateInventoryRow(ItemData item, int quantity)
    {
        var row = CreatePanel(_inventoryRoot, new Color(0.1f, 0.1f, 0.12f, 0.9f), 72f);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(8, 8, 8, 8);
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;

        CreateIcon(row, item.icon, 44f);

        var textCol = new GameObject("Texts");
        textCol.transform.SetParent(row, false);
        var flex = textCol.AddComponent<LayoutElement>();
        flex.flexibleWidth = 1f;
        var v = textCol.AddComponent<VerticalLayoutGroup>();
        v.spacing = 2f;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;

        CreateBodyLabel(textCol.transform, item.itemName + "  ×" + quantity, 16, FontStyles.Bold);

        var detail = item.GetEffectHint();
        if (item.CanStreetSell && _shop != null && _shop.shopKind == ShopZone.ShopKind.Resell)
            detail = string.IsNullOrEmpty(detail)
                ? "Revenda: " + MoneyManager.FormatBRL(item.unitSellPriceCents) + "/un"
                : detail + " · revenda " + MoneyManager.FormatBRL(item.unitSellPriceCents) + "/un";
        if (!string.IsNullOrEmpty(detail))
            CreateBodyLabel(textCol.transform, detail, 14, FontStyles.Normal);

        if (item.CanConsume)
        {
            var useBtn = CreateButton(row, "Comer", 72f, 36f);
            useBtn.onClick.AddListener(() =>
            {
                if (ItemConsumption.TryConsumeFromInventory(_inventory, item, _player))
                {
                    ShowFeedback("Consumiu " + item.itemName + ".");
                    RefreshAll();
                }
                else
                    ShowFeedback("Não foi possível usar " + item.itemName + ".");
            });
        }
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("Canvas_Shop");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 280;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var dim = CreateFullScreenPanel(canvasGo.transform, new Color(0f, 0f, 0f, 0.55f));

        var panel = CreatePanel(dim, new Color(0.08f, 0.08f, 0.1f, 0.96f), 0f);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(1080f, 620f);

        var header = CreatePanel(panel, new Color(0.12f, 0.12f, 0.15f, 1f), 52f);
        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 52f);

        var title = CreateBodyLabel(header.transform, _shop != null ? _shop.shopTitle : "LOJA", 24, FontStyles.Bold);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(0.45f, 1f);
        titleRect.offsetMin = new Vector2(16f, 0f);
        titleRect.offsetMax = Vector2.zero;
        title.alignment = TextAlignmentOptions.MidlineLeft;

        _moneyText = CreateBodyLabel(header.transform, "Saldo: R$ 0,00", 20, FontStyles.Bold);
        var moneyRect = _moneyText.rectTransform;
        moneyRect.anchorMin = new Vector2(0.45f, 0f);
        moneyRect.anchorMax = new Vector2(1f, 1f);
        moneyRect.offsetMin = Vector2.zero;
        moneyRect.offsetMax = new Vector2(-118f, 0f);
        _moneyText.alignment = TextAlignmentOptions.MidlineRight;
        _moneyText.color = new Color(0.95f, 0.85f, 0.35f, 1f);

        var closeBtn = CreateButton(header.transform, "Fechar", 100f, 36f);
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-10f, 0f);
        closeBtn.onClick.AddListener(Close);

        var body = new GameObject("Body");
        body.transform.SetParent(panel, false);
        var bodyRect = body.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(12f, 52f);
        bodyRect.offsetMax = new Vector2(-12f, -56f);
        var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 10f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;

        CreateColumn(body.transform, _shop != null && _shop.shopKind == ShopZone.ShopKind.FastFood ? "Cardápio" : "Produtos",
            0.38f, out _catalogRoot);
        CreateCartColumn(body.transform);
        CreateColumn(body.transform, "Inventário", 0.32f, out _inventoryRoot);

        _feedbackText = CreateBodyLabel(panel, string.Empty, 15, FontStyles.Italic);
        var fbRect = _feedbackText.rectTransform;
        fbRect.anchorMin = new Vector2(0f, 0f);
        fbRect.anchorMax = new Vector2(1f, 0f);
        fbRect.pivot = new Vector2(0.5f, 0f);
        fbRect.anchoredPosition = new Vector2(0f, 6f);
        fbRect.sizeDelta = new Vector2(-24f, 24f);
        _feedbackText.alignment = TextAlignmentOptions.Center;
    }

    void CreateCartColumn(Transform parent)
    {
        var col = new GameObject("Carrinho");
        col.transform.SetParent(parent, false);
        var colLe = col.AddComponent<LayoutElement>();
        colLe.flexibleWidth = 0.3f;
        colLe.minWidth = 260f;

        var v = col.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6f;
        v.childControlHeight = true;
        v.childForceExpandHeight = false;

        CreateBodyLabel(col.transform, "Carrinho", 18, FontStyles.Bold);

        CreateScrollArea(col.transform, out _cartRoot);

        var footer = CreatePanel(col.transform, new Color(0.09f, 0.1f, 0.12f, 1f), 0f);
        var footerLe = footer.gameObject.AddComponent<LayoutElement>();
        footerLe.minHeight = 118f;
        footerLe.preferredHeight = 118f;
        var footerV = footer.gameObject.AddComponent<VerticalLayoutGroup>();
        footerV.padding = new RectOffset(10, 10, 8, 8);
        footerV.spacing = 6f;
        footerV.childControlHeight = true;
        footerV.childForceExpandHeight = false;

        _cartTotalText = CreateBodyLabel(footer, "Total: R$ 0,00", 20, FontStyles.Bold);
        _cartAfterText = CreateBodyLabel(footer, "Saldo após compra: —", 16, FontStyles.Normal);

        var btnRow = new GameObject("CartButtons");
        btnRow.transform.SetParent(footer, false);
        var btnRowLe = btnRow.AddComponent<LayoutElement>();
        btnRowLe.minHeight = 40f;
        var btnH = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnH.spacing = 8f;
        btnH.childControlWidth = true;
        btnH.childForceExpandWidth = true;

        var clearBtn = CreateButton(btnRow.transform, "Limpar", 0f, 36f);
        clearBtn.GetComponent<LayoutElement>().flexibleWidth = 1f;
        clearBtn.onClick.AddListener(ClearCart);

        var payBtn = CreateButton(btnRow.transform, "Pagar", 0f, 36f, new Color(0.15f, 0.62f, 0.35f));
        payBtn.GetComponent<LayoutElement>().flexibleWidth = 1f;
        payBtn.onClick.AddListener(TryCheckout);
    }

    void CreateColumn(Transform parent, string header, float widthWeight, out Transform scrollContent)
    {
        var col = new GameObject(header);
        col.transform.SetParent(parent, false);
        var colLe = col.AddComponent<LayoutElement>();
        colLe.flexibleWidth = widthWeight;
        colLe.minWidth = 240f;

        var v = col.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6f;
        v.childControlHeight = true;
        v.childForceExpandHeight = false;

        CreateBodyLabel(col.transform, header, 18, FontStyles.Bold);
        CreateScrollArea(col.transform, out scrollContent);
    }

    Transform CreateScrollArea(Transform parent, out Transform content)
    {
        var scrollGo = new GameObject("Scroll");
        scrollGo.transform.SetParent(parent, false);
        scrollGo.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 60f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.09f;
        scroll.elasticity = 0.04f;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.85f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewport.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var layout = contentGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var scrollbarGo = new GameObject("Scrollbar");
        scrollbarGo.transform.SetParent(scrollGo.transform, false);
        var sbRect = scrollbarGo.AddComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1f, 0f);
        sbRect.anchorMax = new Vector2(1f, 1f);
        sbRect.pivot = new Vector2(1f, 1f);
        sbRect.sizeDelta = new Vector2(14f, 0f);
        sbRect.anchoredPosition = Vector2.zero;
        var sbBg = scrollbarGo.AddComponent<Image>();
        sbBg.color = new Color(0.05f, 0.05f, 0.07f, 0.9f);

        var handleArea = new GameObject("Sliding Area");
        handleArea.transform.SetParent(scrollbarGo.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(2f, 2f);
        handleAreaRect.offsetMax = new Vector2(-2f, -2f);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(10f, 40f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.35f, 0.55f, 0.85f, 0.95f);

        var scrollbar = scrollbarGo.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scroll.viewport = vpRect;
        scroll.content = contentRect;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = 4f;

        content = contentGo.transform;
        return scrollGo.transform;
    }

    static Transform CreateFullScreenPanel(Transform parent, Color color)
    {
        var go = new GameObject("Dim");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go.transform;
    }

    static Transform CreatePanel(Transform parent, Color color, float height)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        if (height > 0f)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }

        go.AddComponent<Image>().color = color;
        return go.transform;
    }

    static Image CreateIcon(Transform parent, Sprite sprite, float size)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;
        le.minWidth = size;
        le.minHeight = size;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
        return img;
    }

    static TextMeshProUGUI CreateBodyLabel(Transform parent, string text, float size, FontStyles style,
        Color? color = null)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minHeight = size + 6f;

        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color ?? Color.white;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    static Button CreateButton(Transform parent, string label, float width, float height, Color? bg = null)
    {
        var go = new GameObject("Button");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        if (width > 0f)
        {
            le.preferredWidth = width;
            le.minWidth = width;
        }

        le.minHeight = height;
        le.preferredHeight = height;

        var img = go.AddComponent<Image>();
        img.color = bg ?? new Color(0.22f, 0.48f, 0.85f, 1f);
        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return btn;
    }

    static void ClearChildren(Transform root)
    {
        for (var i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        _ownedEventSystem = new GameObject("EventSystem");
        _ownedEventSystem.AddComponent<EventSystem>();
        _ownedEventSystem.AddComponent<StandaloneInputModule>();
    }
}
