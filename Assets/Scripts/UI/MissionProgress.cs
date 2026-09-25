using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MissionId
{
    CollectCans,
    SellAtJunkyard,
    GoToCity,
    KnowYourShelter,
    BuyAtShop,
    BuyMealAtFood4U,
    EatWhenHungry,
    RestAtBarraca,
    Resell,
    CollectCansForHouse,
    SellAtJunkyardForHouse,
    BuyHouse,
    RestInSafeBed,
    AllComplete,
}

public static class MissionProgress
{
    public const string CanItemName = "Latinha";
    public const int CansRequired = 10;
    public const int ResellGoalCents = 400;
    public const string LanchoneteDisplayName = "lanchonete";
    const int MissionSchemaVersion = 2;
    const string InventoryKeysHint = "Pressione I ou Tab para abrir o inventário.";

    public static event Action Changed;

    static bool _started;
    static MissionId _current = MissionId.CollectCans;
    static int _junkyardSoldCount;
    static int _lastReportedCollectCount = -1;
    static int _resellEarningsCents;
    static int _junkyardSoldForHouseCount;
    static int _lastReportedHouseCollectCount = -1;
    static float _completionFlashUntil;

    public static bool IsActive => _started && _current != MissionId.AllComplete;
    public static MissionId Current => _current;
    public static float CompletionFlashUntil => _completionFlashUntil;

    public static void BeginNewGame(string startScene)
    {
        _started = true;
        _junkyardSoldCount = 0;
        _lastReportedCollectCount = -1;
        _resellEarningsCents = 0;
        _junkyardSoldForHouseCount = 0;
        _lastReportedHouseCollectCount = -1;
        _completionFlashUntil = 0f;

        if (startScene == RecomecoSceneNames.Cidade)
        {
            _current = MissionId.KnowYourShelter;
            PlayerNeeds.RequestPrecariousStartForNewGame();
            GameplayDayNightCycle.RequestNewGameStart();
        }
        else
        {
            _current = MissionId.CollectCans;
        }

        NotifyChanged();
    }

    public static void EnsureStartedForScene(string sceneName)
    {
        if (_started || RecomecoSceneNames.IsMenuScene(sceneName))
            return;

        BeginNewGame(sceneName);
    }

    public static void Reset()
    {
        _started = false;
        _current = MissionId.CollectCans;
        _junkyardSoldCount = 0;
        _lastReportedCollectCount = -1;
        _resellEarningsCents = 0;
        _junkyardSoldForHouseCount = 0;
        _lastReportedHouseCollectCount = -1;
        _completionFlashUntil = 0f;
        NotifyChanged();
    }

    public static MissionProgressSnapshot ExportSnapshot()
    {
        return new MissionProgressSnapshot
        {
            started = _started,
            currentMission = (int)_current,
            junkyardSoldCount = _junkyardSoldCount,
            lastReportedCollectCount = _lastReportedCollectCount,
            schemaVersion = MissionSchemaVersion,
            resellEarningsCents = _resellEarningsCents,
            junkyardSoldForHouseCount = _junkyardSoldForHouseCount,
            lastReportedHouseCollectCount = _lastReportedHouseCollectCount,
        };
    }

    public static void ImportSnapshot(MissionProgressSnapshot snapshot)
    {
        _started = snapshot.started;
        _current = MigrateMissionId(snapshot);
        _junkyardSoldCount = snapshot.junkyardSoldCount;
        _lastReportedCollectCount = snapshot.lastReportedCollectCount;
        _resellEarningsCents = snapshot.resellEarningsCents;
        _junkyardSoldForHouseCount = snapshot.junkyardSoldForHouseCount;
        _lastReportedHouseCollectCount = snapshot.lastReportedHouseCollectCount;
        _completionFlashUntil = 0f;
        NotifyChanged();
    }

    static MissionId MigrateMissionId(MissionProgressSnapshot snapshot)
    {
        var raw = snapshot.currentMission;
        var schema = snapshot.schemaVersion;

        if (schema < 1)
        {
            if (raw <= 5)
            {
                return raw switch
                {
                    0 => MissionId.CollectCans,
                    1 => MissionId.SellAtJunkyard,
                    2 => MissionId.GoToCity,
                    3 => MissionId.BuyAtShop,
                    4 => MissionId.Resell,
                    5 => MissionId.AllComplete,
                    _ => MissionId.CollectCans,
                };
            }

            if (raw >= 5 && raw <= 10)
                raw += 1;
            schema = 1;
        }

        if (schema < 2 && raw >= 10)
            raw += 2;

        return (MissionId)Mathf.Clamp(raw, 0, (int)MissionId.AllComplete);
    }

    public static void NotifyEnteredScene(string sceneName)
    {
        EnsureStartedForScene(sceneName);
        if (!IsActive || _current != MissionId.GoToCity)
            return;

        if (sceneName == RecomecoSceneNames.Cidade)
            CompleteCurrent();
    }

    public static void NotifyCollectProgress(int latinhaCount)
    {
        if (!IsActive)
            return;

        if (_current == MissionId.CollectCans)
            TrackCollectProgress(latinhaCount, ref _lastReportedCollectCount);
        else if (_current == MissionId.CollectCansForHouse)
            TrackCollectProgress(latinhaCount, ref _lastReportedHouseCollectCount);
    }

    static void TrackCollectProgress(int latinhaCount, ref int lastReported)
    {
        if (latinhaCount >= CansRequired)
        {
            CompleteCurrent();
            return;
        }

        if (latinhaCount == lastReported)
            return;

        lastReported = latinhaCount;
        NotifyChanged();
    }

    public static void NotifyJunkyardSale(int soldCount)
    {
        if (soldCount <= 0)
            return;

        if (_current == MissionId.SellAtJunkyard)
        {
            _junkyardSoldCount += soldCount;
            if (_junkyardSoldCount >= CansRequired)
                CompleteCurrent();
            else
                NotifyChanged();
            return;
        }

        if (_current == MissionId.SellAtJunkyardForHouse)
        {
            _junkyardSoldForHouseCount += soldCount;
            if (_junkyardSoldForHouseCount >= CansRequired)
                CompleteCurrent();
            else
                NotifyChanged();
        }
    }

    public static void NotifyShelterLocated()
    {
        if (!IsActive || _current != MissionId.KnowYourShelter)
            return;

        CompleteCurrent();
    }

    public static void NotifyShopPurchase()
    {
        if (!IsActive || _current != MissionId.BuyAtShop)
            return;

        CompleteCurrent();
    }

    public static void NotifyFoodShopPurchase()
    {
        if (!IsActive || _current != MissionId.BuyMealAtFood4U)
            return;

        CompleteCurrent();
    }

    public static void NotifyAteFood()
    {
        if (!IsActive || _current != MissionId.EatWhenHungry)
            return;

        CompleteCurrent();
    }

    public static void NotifyPrecariousSleep()
    {
        if (!IsActive || _current != MissionId.RestAtBarraca)
            return;

        CompleteCurrent();
    }

    public static void NotifyStreetSale(int earningsCents)
    {
        if (earningsCents <= 0)
            return;

        if (!IsActive || _current != MissionId.Resell)
            return;

        _resellEarningsCents += earningsCents;
        if (_resellEarningsCents >= ResellGoalCents)
            CompleteCurrent();
        else
            NotifyChanged();
    }

    public static void NotifyHousePurchased(string housingId)
    {
        if (!IsActive || _current != MissionId.BuyHouse)
            return;

        if (housingId != PlayerHousingState.CasaElegante)
            return;

        CompleteCurrent();
    }

    public static void NotifySafeBedRest()
    {
        if (!IsActive || _current != MissionId.RestInSafeBed)
            return;

        CompleteCurrent();
    }

    public static MissionDisplay GetDisplay(int latinhaInInventory)
    {
        if (!_started)
        {
            return new MissionDisplay
            {
                Title = "MISSÃO",
                Description = "Carregando objetivos...",
                ProgressText = string.Empty,
                ShowProgress = false,
            };
        }

        if (_current == MissionId.AllComplete)
        {
            return new MissionDisplay
            {
                Title = "RECOMEÇO",
                Description = "Você tem casa e um lugar seguro para descansar. Siga vivendo na cidade.",
                Hint = "Latinhas, lanchonete, revenda e cuidado com fome e proteção continuam valendo.",
                ProgressText = string.Empty,
                ShowProgress = false,
                IsComplete = true,
            };
        }

        var sceneName = SceneManager.GetActiveScene().name;
        var sleepWindow = GameplayDayNightCycle.GetBarracaSleepWindowText();

        switch (_current)
        {
            case MissionId.CollectCans:
                var collected = Mathf.Clamp(latinhaInInventory, 0, CansRequired);
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Tenha " + CansRequired + " latinhas no inventário",
                    Hint = sceneName == RecomecoSceneNames.FerroVelho
                        ? "As latinhas ficam nas ruas da cidade. Quando tiver " + CansRequired + ", pegue o táxi de volta. " + InventoryKeysHint
                        : "Pegue latinhas no chão das ruas. " + InventoryKeysHint,
                    ProgressText = collected + "/" + CansRequired,
                    ShowProgress = true,
                    ProgressCurrent = collected,
                    ProgressTarget = CansRequired,
                };

            case MissionId.SellAtJunkyard:
                var sold = Mathf.Clamp(_junkyardSoldCount, 0, CansRequired);
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Venda as latinhas no ferro velho",
                    Hint = sceneName == RecomecoSceneNames.Cidade
                        ? "Pegue o táxi até o ferro velho e venda na zona de sucata."
                        : "Use a zona de venda do ferro velho.",
                    ProgressText = sold + "/" + CansRequired,
                    ShowProgress = true,
                    ProgressCurrent = sold,
                    ProgressTarget = CansRequired,
                };

            case MissionId.GoToCity:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Entre na cidade",
                    Hint = "A cidade é onde você tentará se recomeçar.",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.KnowYourShelter:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Encontre a barraca onde você vai dormir",
                    Hint = "Siga a seta até o abrigo. A barra Proteção mostra o quanto você está exposto.",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.BuyAtShop:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Compre um pacote na Lojinha para revender",
                    Hint = "A Lojinha vende doces e salgados para vender na rua, não comida pronta. " +
                           "Sem dinheiro? Colete latinhas e venda no ferro velho. " + InventoryKeysHint,
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.Resell:
                var resellEarned = Mathf.Clamp(_resellEarningsCents, 0, ResellGoalCents);
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Venda na rua o que comprou na Lojinha",
                    Hint = "Use pontos de venda ou fale com pedestres. Meta: " + MoneyManager.FormatBRL(ResellGoalCents) + " de lucro.",
                    ProgressText = MoneyManager.FormatBRL(resellEarned) + " / " + MoneyManager.FormatBRL(ResellGoalCents),
                    ShowProgress = true,
                    ProgressCurrent = resellEarned,
                    ProgressTarget = ResellGoalCents,
                };

            case MissionId.BuyMealAtFood4U:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Compre um lanche na " + LanchoneteDisplayName,
                    Hint = "Com o dinheiro da revenda, compre algo para comer. Lanches recuperam fome.",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.EatWhenHungry:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Coma o lanche que você comprou",
                    Hint = InventoryKeysHint + " Selecione o item e use para comer.",
                    ProgressText = FormatHungerProgress(),
                    ShowProgress = true,
                    ProgressCurrent = ResolveHungerPercent(),
                    ProgressTarget = 100,
                };

            case MissionId.RestAtBarraca:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Descanse na barraca à noite",
                    Hint = "É possível dormir das " + sleepWindow + ".",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.CollectCansForHouse:
                var houseCollect = Mathf.Clamp(latinhaInInventory, 0, CansRequired);
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Junte " + CansRequired + " latinhas para vender no ferro velho",
                    Hint = sceneName == RecomecoSceneNames.FerroVelho
                        ? "As latinhas ficam nas ruas da cidade. Pegue o táxi de volta quando tiver " + CansRequired + ". " + InventoryKeysHint
                        : "Pegue latinhas nas ruas. Depois pegue o táxi até o ferro velho. " + InventoryKeysHint,
                    ProgressText = houseCollect + "/" + CansRequired,
                    ShowProgress = true,
                    ProgressCurrent = houseCollect,
                    ProgressTarget = CansRequired,
                };

            case MissionId.SellAtJunkyardForHouse:
                var houseSold = Mathf.Clamp(_junkyardSoldForHouseCount, 0, CansRequired);
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Venda " + CansRequired + " latinhas no ferro velho",
                    Hint = sceneName == RecomecoSceneNames.Cidade
                        ? "Pegue o táxi até o ferro velho e venda na zona de sucata."
                        : "Venda na zona do ferro velho. Com o dinheiro você se aproxima da casa (" +
                          ResolveHousePriceLabel() + ").",
                    ProgressText = houseSold + "/" + CansRequired,
                    ShowProgress = true,
                    ProgressCurrent = houseSold,
                    ProgressTarget = CansRequired,
                };

            case MissionId.BuyHouse:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Compre a Casa elegante",
                    Hint = "Com moradia fixa você descansa melhor e pode salvar na cama. Preço: " +
                           ResolveHousePriceLabel() + ".",
                    ProgressText = FormatMoneyProgress(),
                    ShowProgress = false,
                };

            case MissionId.RestInSafeBed:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Descanse na cama da sua casa à noite",
                    Hint = "De noite, durma na cama para recuperar. De dia, você ainda pode salvar o jogo na cama, sem dormir.",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            default:
                return default;
        }
    }

    static string FormatHungerProgress()
    {
        var needs = PlayerNeeds.Instance;
        if (needs == null)
            return string.Empty;

        return "Fome " + Mathf.RoundToInt(needs.Hunger) + "%";
    }

    static int ResolveHungerPercent()
    {
        var needs = PlayerNeeds.Instance;
        return needs != null ? Mathf.RoundToInt(needs.Hunger) : 0;
    }

    static string FormatMoneyProgress()
    {
        if (MoneyManager.instance == null)
            return string.Empty;

        return MoneyManager.FormatBRL(MoneyManager.instance.GetMoney());
    }

    static string ResolveHousePriceLabel()
    {
        foreach (var door in UnityEngine.Object.FindObjectsByType<HouseDoorInteract>(FindObjectsSortMode.None))
        {
            if (door == null || door.housingId != PlayerHousingState.CasaElegante)
                continue;

            return MoneyManager.FormatBRL(door.purchasePriceCents);
        }

        return "R$ 150,00";
    }

    static void CompleteCurrent()
    {
        _completionFlashUntil = Time.unscaledTime + 2f;

        switch (_current)
        {
            case MissionId.CollectCans:
                _current = MissionId.SellAtJunkyard;
                break;
            case MissionId.SellAtJunkyard:
                _current = MissionId.GoToCity;
                break;
            case MissionId.GoToCity:
                _current = MissionId.KnowYourShelter;
                break;
            case MissionId.KnowYourShelter:
                _current = MissionId.BuyAtShop;
                break;
            case MissionId.BuyAtShop:
                _current = MissionId.Resell;
                break;
            case MissionId.Resell:
                _current = MissionId.BuyMealAtFood4U;
                break;
            case MissionId.BuyMealAtFood4U:
                _current = MissionId.EatWhenHungry;
                break;
            case MissionId.EatWhenHungry:
                _current = MissionId.RestAtBarraca;
                break;
            case MissionId.RestAtBarraca:
                _current = MissionId.CollectCansForHouse;
                _lastReportedHouseCollectCount = -1;
                break;
            case MissionId.CollectCansForHouse:
                _current = MissionId.SellAtJunkyardForHouse;
                _junkyardSoldForHouseCount = 0;
                break;
            case MissionId.SellAtJunkyardForHouse:
                _current = MissionId.BuyHouse;
                break;
            case MissionId.BuyHouse:
                _current = MissionId.RestInSafeBed;
                break;
            case MissionId.RestInSafeBed:
                _current = MissionId.AllComplete;
                break;
        }

        NotifyChanged();
    }

    public static void TryAdvanceSkippableSteps()
    {
        if (!IsActive)
            return;

        if (_current == MissionId.BuyMealAtFood4U && PlayerCarriesConsumableMeal())
            CompleteCurrent();

        if (_current == MissionId.EatWhenHungry)
        {
            var needs = PlayerNeeds.Instance;
            if (needs != null && needs.Hunger >= 78f)
                CompleteCurrent();
        }

        if (_current == MissionId.Resell && _resellEarningsCents >= ResellGoalCents)
            CompleteCurrent();

        if (_current == MissionId.BuyHouse && PlayerHousingState.Owns(PlayerHousingState.CasaElegante))
            CompleteCurrent();

        TrySkipHouseFundingIfAffordable();
        TryAdvanceHouseCollectFromInventory();
    }

    static void TrySkipHouseFundingIfAffordable()
    {
        if (!IsActive || !CanAffordHouse())
            return;

        while (_current == MissionId.CollectCansForHouse || _current == MissionId.SellAtJunkyardForHouse)
            CompleteCurrent();
    }

    static void TryAdvanceHouseCollectFromInventory()
    {
        if (_current != MissionId.CollectCansForHouse)
            return;

        var inventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
        if (inventory == null)
            return;

        NotifyCollectProgress(inventory.GetItemCount(CanItemName));
    }

    static bool CanAffordHouse()
    {
        if (MoneyManager.instance == null)
            return false;

        return MoneyManager.instance.GetMoney() >= ResolveHousePriceCents();
    }

    static int ResolveHousePriceCents()
    {
        foreach (var door in UnityEngine.Object.FindObjectsByType<HouseDoorInteract>(FindObjectsSortMode.None))
        {
            if (door == null || door.housingId != PlayerHousingState.CasaElegante)
                continue;

            return door.purchasePriceCents;
        }

        return 15000;
    }

    static bool PlayerCarriesConsumableMeal()
    {
        var inventory = UnityEngine.Object.FindFirstObjectByType<Inventory>();
        if (inventory == null || inventory.slots == null)
            return false;

        foreach (var slot in inventory.slots)
        {
            if (slot == null || slot.item == null || slot.quantity <= 0)
                continue;

            if (slot.item.CanConsume && slot.item.hungerRestore > 0f)
                return true;
        }

        return false;
    }

    public static void NotifyChanged() => Changed?.Invoke();
}

public struct MissionDisplay
{
    public string Title;
    public string Description;
    public string Hint;
    public string ProgressText;
    public bool ShowProgress;
    public bool IsComplete;
    public int ProgressCurrent;
    public int ProgressTarget;
}
