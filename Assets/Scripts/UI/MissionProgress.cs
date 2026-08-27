using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MissionId
{
    CollectCans,
    SellAtJunkyard,
    GoToCity,
    BuyAtShop,
    Resell,
    AllComplete,
}

public static class MissionProgress
{
    public const string CanItemName = "Latinha";
    public const int CansRequired = 10;

    public static event Action Changed;

    static bool _started;
    static MissionId _current = MissionId.CollectCans;
    static int _junkyardSoldCount;
    static int _lastReportedCollectCount = -1;
    static float _completionFlashUntil;

    public static bool IsActive => _started && _current != MissionId.AllComplete;
    public static MissionId Current => _current;
    public static float CompletionFlashUntil => _completionFlashUntil;

    public static void BeginNewGame(string startScene)
    {
        _started = true;
        _junkyardSoldCount = 0;
        _lastReportedCollectCount = -1;
        _completionFlashUntil = 0f;

        if (startScene == RecomecoSceneNames.Cidade)
        {
            _current = MissionId.BuyAtShop;
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
        _completionFlashUntil = 0f;
        NotifyChanged();
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
        if (!IsActive || _current != MissionId.CollectCans)
            return;

        if (latinhaCount >= CansRequired)
        {
            CompleteCurrent();
            return;
        }

        if (latinhaCount == _lastReportedCollectCount)
            return;

        _lastReportedCollectCount = latinhaCount;
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

        NotifyChanged();
    }

    public static void NotifyShopPurchase()
    {
        if (!IsActive || _current != MissionId.BuyAtShop)
            return;

        CompleteCurrent();
    }

    public static void NotifyResell()
    {
        if (!IsActive || _current != MissionId.Resell)
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
                Title = "MISSÃO",
                Description = "Objetivos concluídos! Continue lucrando e crescendo.",
                ProgressText = string.Empty,
                ShowProgress = false,
                IsComplete = true,
            };
        }

        var sceneName = SceneManager.GetActiveScene().name;

        switch (_current)
        {
            case MissionId.CollectCans:
                var collected = Mathf.Clamp(latinhaInInventory, 0, CansRequired);
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Colete 10 latinhas na cidade",
                    Hint = sceneName == RecomecoSceneNames.FerroVelho
                        ? "Vá até a cidade — as latinhas ficam nas ruas."
                        : "Procure latinhas pelo chão nas ruas.",
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
                    Description = "Venda 10 latinhas no ferro velho",
                    Hint = sceneName == RecomecoSceneNames.Cidade
                        ? "Vá ao ferro velho pelo portal para vender."
                        : string.Empty,
                    ProgressText = sold + "/" + CansRequired,
                    ShowProgress = true,
                    ProgressCurrent = sold,
                    ProgressTarget = CansRequired,
                };

            case MissionId.GoToCity:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Vá até a cidade pelo portal",
                    Hint = sceneName == RecomecoSceneNames.FerroVelho
                        ? "Atravesse o portal na entrada do ferro velho."
                        : string.Empty,
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.BuyAtShop:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Compre um pacote na Lojinha",
                    Hint = "Sem dinheiro? Colete latinhas na cidade e venda no ferro velho.",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            case MissionId.Resell:
                return new MissionDisplay
                {
                    Title = "MISSÃO",
                    Description = "Revenda na rua ou a um pedestre",
                    ProgressText = string.Empty,
                    ShowProgress = false,
                };

            default:
                return default;
        }
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
                _current = MissionId.BuyAtShop;
                break;
            case MissionId.BuyAtShop:
                _current = MissionId.Resell;
                break;
            case MissionId.Resell:
                _current = MissionId.AllComplete;
                break;
        }

        NotifyChanged();
    }

    public static void NotifyChanged() => Changed?.Invoke();

    public struct MissionDisplay
    {
        public string Title;
        public string Description;
        public string Hint;
        public string ProgressText;
        public bool ShowProgress;
        public int ProgressCurrent;
        public int ProgressTarget;
        public bool IsComplete;
    }
}
