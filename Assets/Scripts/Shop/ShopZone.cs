using System.Collections.Generic;
using UnityEngine;

public class ShopZone : MonoBehaviour, IInteractionPromptOwner
{
    public enum ShopKind
    {
        [Tooltip("Pacotes para revender na rua (Lojinha).")]
        Resell,

        [Tooltip("Porções prontas para comer (FOOD4U).")]
        FastFood,
    }

    [System.Serializable]
    public class ShopProduct
    {
        [Tooltip("Item que o jogador recebe (uma unidade por slot do pacote)")]
        public ItemData item;

        [Tooltip("Quantas unidades vêm no pacote (FOOD4U: use 1)")]
        public int unitsPerPack = 10;

        [Tooltip("Preço do pacote na loja, em CENTAVOS (500 = R$ 5,00)")]
        public int packPriceCents = 500;

        [Tooltip("Nome do pacote mostrado na loja (ex.: Pote de paçoca). Vazio = nome do item.")]
        public string packLabel;

        public string DisplayName =>
            !string.IsNullOrEmpty(packLabel) ? packLabel : (item != null ? item.itemName : "?");
    }

    [Tooltip("Lojinha = revenda; FOOD4U = comida.")]
    public ShopKind shopKind = ShopKind.Resell;

    [Tooltip("Distância máxima para abrir a loja (ou raio da zona de trigger).")]
    public float interactDistance = 6f;

    [Tooltip("Ponto na porta/balcão. Vazio = usa este objeto.")]
    public Transform interactPoint;

    [Tooltip("Ignora diferença de altura (melhor para prédios).")]
    public bool useHorizontalInteractRange = true;

    [Tooltip("Se true, usa o collider de trigger (filho ShopInteractVolume ou no mesmo objeto).")]
    public bool useInteractTrigger = true;

    [Tooltip("Título mostrado na interface")]
    public string shopTitle = "LOJINHA";

    public KeyCode interactKey = KeyCode.E;

    public ShopProduct[] products;

    bool _playerInRange;
    int _playerTriggerCount;
    Transform _autoInteractPoint;

    public bool IsInteractionPromptActive() => _playerInRange && isActiveAndEnabled && !ShopUI.IsOpen;

    void OnDisable()
    {
        SetInRange(false);
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
    }

    void Start()
    {
        if (shopKind == ShopKind.Resell && interactDistance < 6f)
            interactDistance = 7f;

        EnsureAutoInteractPoint();
        if (useInteractTrigger)
            EnsureInteractTriggerVolume();
    }

    public void RegisterTrigger(Collider other, bool entered)
    {
        if (!useInteractTrigger || other == null || !IsPlayerCollider(other))
            return;

        if (entered)
            _playerTriggerCount++;
        else
            _playerTriggerCount = Mathf.Max(0, _playerTriggerCount - 1);
    }

    void LateUpdate()
    {
        if (ShopUI.IsOpen)
            return;

        var player = InteractionProximity.GetPlayer();
        var inRange = player != null && IsPlayerInInteractRange(player.transform);

        if (inRange != _playerInRange)
            SetInRange(inRange);

        if (!_playerInRange || player == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            InteractionUI.HideMessage(this);
            ShopUI.Open(this, player);
            return;
        }

        ShowPrompt();
    }

    void SetInRange(bool inRange)
    {
        _playerInRange = inRange;
        if (!inRange)
            InteractionUI.HideMessage(this);
    }

    void ShowPrompt()
    {
        var verb = shopKind == ShopKind.FastFood ? "Abrir lanchonete" : "Abrir loja";
        InteractionUI.ShowMessage(verb + " (" + shopTitle + ") — " + interactKey, this);
    }

    Vector3 GetInteractWorldPosition()
    {
        if (interactPoint != null)
            return interactPoint.position;

        if (_autoInteractPoint != null)
            return _autoInteractPoint.position;

        return transform.position;
    }

    bool IsPlayerInInteractRange(Transform player)
    {
        if (useInteractTrigger && _playerTriggerCount > 0)
            return true;

        var anchor = GetInteractWorldPosition();
        if (useHorizontalInteractRange)
            return InteractionProximity.IsWithinHorizontalRange(anchor, interactDistance, player, scaleWithPlayer: true);

        return InteractionProximity.IsWithinRange(anchor, interactDistance, player, scaleWithPlayer: true);
    }

    void EnsureAutoInteractPoint()
    {
        if (interactPoint != null)
            return;

        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var go = new GameObject("ShopInteract_Auto");
        go.transform.SetParent(transform, true);
        go.transform.position = new Vector3(bounds.center.x, bounds.min.y + 1.2f, bounds.center.z);
        _autoInteractPoint = go.transform;
    }

    void EnsureInteractTriggerVolume()
    {
        var existing = transform.Find("ShopInteractVolume");
        if (existing != null)
        {
            WireTriggerRelay(existing);
            return;
        }

        var volumeGo = new GameObject("ShopInteractVolume");
        volumeGo.transform.SetParent(transform, false);
        volumeGo.transform.localPosition = shopKind == ShopKind.Resell
            ? new Vector3(0f, 1.5f, 4f)
            : new Vector3(0f, 1.2f, 0f);

        var box = volumeGo.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = shopKind == ShopKind.Resell
            ? new Vector3(6f, 3f, 6f)
            : new Vector3(4f, 2.5f, 4f);

        WireTriggerRelay(volumeGo.transform);
    }

    static void WireTriggerRelay(Transform volumeTransform)
    {
        if (volumeTransform == null)
            return;

        var relay = volumeTransform.GetComponent<ShopZoneTriggerRelay>();
        if (relay == null)
            relay = volumeTransform.gameObject.AddComponent<ShopZoneTriggerRelay>();

        relay.shop = volumeTransform.GetComponentInParent<ShopZone>();
    }

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        var root = other.transform.root;
        if (root.CompareTag("Player"))
            return true;

        var player = InteractionProximity.GetPlayer();
        return player != null && root.gameObject == player;
    }

    public bool TryPurchase(ShopProduct product, GameObject player, out string message, bool consumeOne = false)
    {
        message = string.Empty;
        if (product == null || product.item == null)
        {
            message = "Produto inválido.";
            return false;
        }

        if (MoneyManager.instance == null)
        {
            message = "MoneyManager não encontrado.";
            return false;
        }

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            message = "Inventário não encontrado.";
            return false;
        }

        if (MoneyManager.instance.GetMoney() < product.packPriceCents)
        {
            message = "Dinheiro insuficiente para " + product.DisplayName + ".";
            return false;
        }

        if (!consumeOne || !product.item.CanConsume)
        {
            if (GetInventoryCapacity(inventory, product.item) < product.unitsPerPack)
            {
                message = "Inventário sem espaço.";
                return false;
            }
        }

        MoneyManager.instance.RemoveMoney(product.packPriceCents);

        if (consumeOne && product.item.CanConsume)
        {
            if (!ItemConsumption.ApplyEffects(player, product.item))
            {
                MoneyManager.instance.AddMoney(product.packPriceCents);
                message = "Este item não pode ser consumido.";
                return false;
            }

            if (shopKind == ShopKind.Resell)
                MissionProgress.NotifyShopPurchase();

            message = "Consumiu " + product.DisplayName + " por " +
                      MoneyManager.FormatBRL(product.packPriceCents) + ".";
            return true;
        }

        for (var i = 0; i < product.unitsPerPack; i++)
            inventory.AddItem(product.item);

        if (shopKind == ShopKind.Resell)
            MissionProgress.NotifyShopPurchase();

        message = "Comprou " + product.DisplayName + " (" + product.unitsPerPack + " un) por " +
                  MoneyManager.FormatBRL(product.packPriceCents) + ".";
        return true;
    }

    public bool TryCheckout(IReadOnlyDictionary<int, int> quantityByProductIndex, GameObject player, out string message)
    {
        message = string.Empty;
        if (quantityByProductIndex == null || quantityByProductIndex.Count == 0)
        {
            message = "Carrinho vazio.";
            return false;
        }

        if (products == null || products.Length == 0)
        {
            message = "Nenhum produto na loja.";
            return false;
        }

        if (MoneyManager.instance == null)
        {
            message = "MoneyManager não encontrado.";
            return false;
        }

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            message = "Inventário não encontrado.";
            return false;
        }

        var totalCents = 0;
        var unitsByItem = new Dictionary<ItemData, int>();

        foreach (var pair in quantityByProductIndex)
        {
            if (pair.Value <= 0)
                continue;

            if (pair.Key < 0 || pair.Key >= products.Length)
            {
                message = "Produto inválido no carrinho.";
                return false;
            }

            var product = products[pair.Key];
            if (product == null || product.item == null)
            {
                message = "Produto inválido no carrinho.";
                return false;
            }

            totalCents += product.packPriceCents * pair.Value;
            var units = product.unitsPerPack * pair.Value;
            if (unitsByItem.TryGetValue(product.item, out var existing))
                unitsByItem[product.item] = existing + units;
            else
                unitsByItem[product.item] = units;
        }

        if (totalCents <= 0)
        {
            message = "Carrinho vazio.";
            return false;
        }

        if (MoneyManager.instance.GetMoney() < totalCents)
        {
            message = "Dinheiro insuficiente. Total: " + MoneyManager.FormatBRL(totalCents) + ".";
            return false;
        }

        foreach (var pair in unitsByItem)
        {
            if (GetInventoryCapacity(inventory, pair.Key) < pair.Value)
            {
                message = "Inventário sem espaço para " + pair.Key.itemName + ".";
                return false;
            }
        }

        MoneyManager.instance.RemoveMoney(totalCents);

        foreach (var pair in quantityByProductIndex)
        {
            if (pair.Value <= 0 || pair.Key < 0 || pair.Key >= products.Length)
                continue;

            var product = products[pair.Key];
            for (var q = 0; q < pair.Value; q++)
            {
                for (var u = 0; u < product.unitsPerPack; u++)
                {
                    if (inventory.AddItem(product.item))
                        continue;

                    MoneyManager.instance.AddMoney(totalCents);
                    message = "Inventário cheio — nada foi cobrado.";
                    return false;
                }
            }
        }

        if (shopKind == ShopKind.Resell)
            MissionProgress.NotifyShopPurchase();

        inventory.RefreshAllSlots();
        message = string.Empty;
        return true;
    }

    public static int GetInventoryCapacity(Inventory inventory, ItemData item)
    {
        if (inventory.slots == null)
            return 0;

        var capacity = 0;
        foreach (var slot in inventory.slots)
        {
            if (slot == null)
                continue;
            if (slot.IsEmpty())
                capacity += inventory.maxStackPerSlot;
            else if (slot.CanStack(item))
                capacity += Mathf.Max(0, inventory.maxStackPerSlot - slot.quantity);
        }

        return capacity;
    }
}
