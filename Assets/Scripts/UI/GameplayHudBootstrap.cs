using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayHudBootstrap
{
    const string HudRootName = "Canvas_GameplayHud";
    const int InventorySlotCount = 12;

    static GameObject _persistentHudRoot;

    public static void Ensure()
    {
        if (RecomecoSceneNames.IsMenuScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))
            return;

        EnsureMoneyManager();
        EnsureHudCanvas();
    }

    public static void WirePlayerInventory(GameObject player)
    {
        if (player == null)
            return;

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            return;

        inventory.ReconnectUi();
    }

    static void EnsureMoneyManager()
    {
        if (MoneyManager.instance != null)
            return;

        var existing = Object.FindFirstObjectByType<MoneyManager>();
        if (existing != null)
            return;

        var go = new GameObject("MoneyManager");
        go.AddComponent<MoneyManager>();
        Object.DontDestroyOnLoad(go);
    }

    public static void ResetForMenu()
    {
        if (_persistentHudRoot != null)
        {
            Object.Destroy(_persistentHudRoot);
            _persistentHudRoot = null;
        }
        else
        {
            var existing = GameObject.Find(HudRootName);
            if (existing != null)
                Object.Destroy(existing);
        }
    }

    public static GameObject GetHudRoot()
    {
        if (_persistentHudRoot != null && !_persistentHudRoot)
            _persistentHudRoot = null;

        return _persistentHudRoot;
    }

    public static InventoryUI ResolveInventoryUi()
    {
        if (_persistentHudRoot != null && _persistentHudRoot)
        {
            var ui = _persistentHudRoot.GetComponentInChildren<InventoryUI>(true);
            if (ui != null)
                return ui;
        }

        foreach (var inv in Object.FindObjectsByType<InventoryUI>(FindObjectsSortMode.None))
        {
            if (inv != null && inv.gameObject.activeInHierarchy)
                return inv;
        }

        return null;
    }

    static void EnsureHudCanvas()
    {
        if (_persistentHudRoot != null && !_persistentHudRoot)
            _persistentHudRoot = null;

        if (_persistentHudRoot == null)
        {
            var existing = GameObject.Find(HudRootName);
            if (existing != null)
                _persistentHudRoot = existing;
        }

        if (_persistentHudRoot == null)
        {
            _persistentHudRoot = BuildHudCanvas();
            Object.DontDestroyOnLoad(_persistentHudRoot);
        }
        else
        {
            _persistentHudRoot.SetActive(true);
            EnsureInventoryOnCanvas(_persistentHudRoot);
            Object.DontDestroyOnLoad(_persistentHudRoot);
        }

        RegisterHud(_persistentHudRoot);
        EnsureMoneyDisplay(_persistentHudRoot);
        SuppressDuplicateGameplayUi();
    }

    static void EnsureMoneyDisplay(GameObject canvasRoot)
    {
        if (canvasRoot == null)
            return;

        var hudRoot = canvasRoot.transform.Find("HUD");
        if (hudRoot == null)
            return;

        if (hudRoot.Find("MoneyRow") != null)
        {
            var existingController = hudRoot.GetComponent<HUDController>();
            if (existingController == null)
                existingController = hudRoot.gameObject.AddComponent<HUDController>();
            ApplyMoneyRowLayout(hudRoot.Find("MoneyRow"), existingController);
            return;
        }

        var legacyText = hudRoot.GetComponent<TextMeshProUGUI>();
        if (legacyText != null)
        {
            if (Application.isPlaying)
                Object.Destroy(legacyText);
            else
                Object.DestroyImmediate(legacyText);
        }

        var hudController = hudRoot.GetComponent<HUDController>();
        if (hudController == null)
            hudController = hudRoot.gameObject.AddComponent<HUDController>();

        BuildMoneyRow(hudRoot, hudController);
    }

    static Sprite LoadMoneyIconSprite()
    {
        return Resources.Load<Sprite>("UI/icone_dinheiro");
    }

    static void BuildMoneyRow(Transform hudRoot, HUDController controller)
    {
        var rowGo = new GameObject("MoneyRow");
        rowGo.transform.SetParent(hudRoot, false);
        var rowRect = rowGo.AddComponent<RectTransform>();
        rowRect.anchorMin = Vector2.zero;
        rowRect.anchorMax = Vector2.one;
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        var layout = rowGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var iconGo = new GameObject("MoneyIcon");
        iconGo.transform.SetParent(rowGo.transform, false);
        iconGo.AddComponent<RectTransform>();
        var iconLayout = iconGo.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 96f;
        iconLayout.preferredHeight = 96f;
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.color = Color.white;

        var amountGo = new GameObject("MoneyAmount");
        amountGo.transform.SetParent(rowGo.transform, false);
        amountGo.AddComponent<RectTransform>();
        var amountLayout = amountGo.AddComponent<LayoutElement>();
        amountLayout.preferredWidth = 300f;
        amountLayout.preferredHeight = 56f;
        var amountText = amountGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            amountText.font = TMP_Settings.defaultFontAsset;
        amountText.fontSize = 34f;
        amountText.color = Color.white;
        amountText.alignment = TextAlignmentOptions.MidlineLeft;
        amountText.raycastTarget = false;
        amountText.text = MoneyManager.FormatBRL(0);

        ApplyMoneyRowLayout(rowGo.transform, controller, iconImage, amountText);
    }

    static void ApplyMoneyRowLayout(Transform moneyRow, HUDController controller, Image iconImage = null, TextMeshProUGUI amountText = null)
    {
        if (moneyRow == null || controller == null)
            return;

        if (moneyRow.parent is RectTransform hudRect)
            hudRect.sizeDelta = new Vector2(420f, 96f);

        var iconTransform = moneyRow.Find("MoneyIcon");
        if (iconTransform != null)
        {
            var iconLayout = iconTransform.GetComponent<LayoutElement>() ?? iconTransform.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 96f;
            iconLayout.preferredHeight = 96f;
            iconImage ??= iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.color = Color.white;
            }
        }

        var amountTransform = moneyRow.Find("MoneyAmount");
        if (amountTransform != null)
        {
            var amountLayout = amountTransform.GetComponent<LayoutElement>() ?? amountTransform.gameObject.AddComponent<LayoutElement>();
            amountLayout.preferredWidth = 300f;
            amountLayout.preferredHeight = 56f;
            amountText ??= amountTransform.GetComponent<TextMeshProUGUI>();
            if (amountText != null)
            {
                if (TMP_Settings.defaultFontAsset != null && amountText.font == null)
                    amountText.font = TMP_Settings.defaultFontAsset;
                amountText.fontSize = 34f;
                amountText.color = Color.white;
                amountText.alignment = TextAlignmentOptions.MidlineLeft;
                amountText.raycastTarget = false;
            }
        }

        controller.ConfigureMoneyDisplay(amountText, iconImage, LoadMoneyIconSprite());
    }

    static void SuppressDuplicateGameplayUi()
    {
        if (_persistentHudRoot == null)
            return;

        SuppressComponentsOutsidePersistentHud<HUDController>();
        SuppressComponentsOutsidePersistentHud<InventoryUI>();
    }

    static void SuppressComponentsOutsidePersistentHud<T>() where T : Component
    {
        foreach (var component in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (component == null || IsUnderPersistentHud(component.transform))
                continue;

            component.gameObject.SetActive(false);
        }
    }

    static bool IsUnderPersistentHud(Transform transform)
    {
        if (_persistentHudRoot == null || transform == null)
            return false;

        return transform.root.gameObject == _persistentHudRoot;
    }

    static void RegisterHud(GameObject root)
    {
        var interaction = root.GetComponentInChildren<InteractionUI>(true);
        if (interaction != null)
            InteractionUI.Register(interaction);
    }

    static void EnsureInventoryOnCanvas(GameObject canvasRoot)
    {
        if (canvasRoot.GetComponentInChildren<InventoryUI>(true) != null)
            return;

        BuildInventoryUi(canvasRoot.transform);
    }

    static GameObject BuildHudCanvas()
    {
        var canvasGo = new GameObject(HudRootName);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(canvasGo.transform, false);
        var hudRect = hudGo.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(24f, -20f);
        hudRect.sizeDelta = new Vector2(420f, 96f);

        var hudController = hudGo.AddComponent<HUDController>();
        BuildMoneyRow(hudGo.transform, hudController);

        var interactionGo = new GameObject("InteractionText");
        interactionGo.transform.SetParent(canvasGo.transform, false);
        var interactionRect = interactionGo.AddComponent<RectTransform>();
        interactionRect.anchorMin = new Vector2(0.5f, 0f);
        interactionRect.anchorMax = new Vector2(0.5f, 0f);
        interactionRect.pivot = new Vector2(0.5f, 0f);
        interactionRect.anchoredPosition = new Vector2(0f, 72f);
        interactionRect.sizeDelta = new Vector2(640f, 56f);

        var interactionText = interactionGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            interactionText.font = TMP_Settings.defaultFontAsset;
        interactionText.fontSize = 24f;
        interactionText.color = Color.white;
        interactionText.alignment = TextAlignmentOptions.Center;
        interactionText.raycastTarget = false;
        interactionGo.SetActive(false);

        var interactionUi = canvasGo.AddComponent<InteractionUI>();
        interactionUi.interactionText = interactionText;
        interactionUi.interactionTextObject = interactionGo;

        BuildInventoryUi(canvasGo.transform);
        return canvasGo;
    }

    static void BuildInventoryUi(Transform canvasRoot)
    {
        var inventoryUiGo = new GameObject("InventoryUI");
        inventoryUiGo.transform.SetParent(canvasRoot, false);
        var uiRect = inventoryUiGo.AddComponent<RectTransform>();
        uiRect.anchorMin = new Vector2(0.5f, 0.5f);
        uiRect.anchorMax = new Vector2(0.5f, 0.5f);
        uiRect.pivot = new Vector2(0.5f, 0.5f);
        uiRect.anchoredPosition = Vector2.zero;
        uiRect.sizeDelta = new Vector2(100f, 100f);

        var panelGo = new GameObject("InventoryPanel");
        panelGo.transform.SetParent(inventoryUiGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(360f, 280f);

        var panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.12f, 0.88f);

        var grid = panelGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80f, 80f);
        grid.spacing = new Vector2(5f, 5f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        for (var i = 0; i < InventorySlotCount; i++)
            CreateInventorySlot(panelGo.transform, i == 0 ? "Slot" : "Slot (" + i + ")");

        panelGo.SetActive(false);

        var inventoryUi = inventoryUiGo.AddComponent<InventoryUI>();
        inventoryUi.inventoryPanel = panelGo;
    }

    static void CreateInventorySlot(Transform parent, string slotName)
    {
        var slotGo = new GameObject(slotName);
        slotGo.transform.SetParent(parent, false);

        var slotRect = slotGo.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(80f, 80f);

        var slotBg = slotGo.AddComponent<Image>();
        slotBg.color = new Color(0.18f, 0.18f, 0.18f, 0.9f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        StretchWithPadding(iconRect, 10f);
        var icon = iconGo.AddComponent<Image>();
        icon.enabled = false;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var qtyGo = new GameObject("Quantity");
        qtyGo.transform.SetParent(slotGo.transform, false);
        var qtyRect = qtyGo.AddComponent<RectTransform>();
        qtyRect.anchorMin = new Vector2(1f, 0f);
        qtyRect.anchorMax = new Vector2(1f, 0f);
        qtyRect.pivot = new Vector2(1f, 0f);
        qtyRect.anchoredPosition = new Vector2(-4f, 4f);
        qtyRect.sizeDelta = new Vector2(36f, 24f);

        var qtyText = qtyGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            qtyText.font = TMP_Settings.defaultFontAsset;
        qtyText.fontSize = 18f;
        qtyText.color = Color.white;
        qtyText.fontStyle = FontStyles.Bold;
        qtyText.alignment = TextAlignmentOptions.BottomRight;
        qtyText.raycastTarget = false;

        var slotUi = slotGo.AddComponent<SlotUI>();
        slotUi.icon = icon;
        slotUi.quantityText = qtyText;
    }

    static void StretchWithPadding(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }
}
