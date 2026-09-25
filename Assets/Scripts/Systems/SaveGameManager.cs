using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveGameManager
{
    const string SaveFileName = "recomeco_save.json";

    static SaveGameData _pendingLoad;

    public static bool HasSave()
    {
        return File.Exists(GetSavePath());
    }

    public static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    public static void StageForLoad(SaveGameData data)
    {
        _pendingLoad = data;
    }

    public static bool HasPendingLoad => _pendingLoad != null;

    public static void ClearPendingLoad()
    {
        _pendingLoad = null;
    }

    public static SaveGameData LoadFromDisk()
    {
        var path = GetSavePath();
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveGameData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SaveGameManager: falha ao ler save — " + ex.Message);
            return null;
        }
    }

    public static bool SaveCurrentGame(string spawnIdOverride = null)
    {
        GameSession.SaveBeforeSceneLoad();
        var data = CaptureCurrentState(spawnIdOverride);
        if (data == null)
            return false;

        try
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(GetSavePath(), json);
            Debug.Log("SaveGameManager: jogo salvo em " + GetSavePath());
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SaveGameManager: falha ao salvar — " + ex.Message);
            return false;
        }
    }

    public static SaveGameData CaptureCurrentState(string spawnIdOverride = null)
    {
        var data = new SaveGameData
        {
            money = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0,
            inventory = GameSession.ExportInventorySnapshot(),
            ownedHouses = PlayerHousingState.ExportOwned(),
            storages = HomeStorage.ExportAll(),
            mission = MissionProgress.ExportSnapshot(),
            needs = ExportNeedsSnapshot(),
            dayNight = ExportDayNightSnapshot(),
            lastScene = SceneManager.GetActiveScene().name,
            lastSpawnId = !string.IsNullOrEmpty(spawnIdOverride)
                ? spawnIdOverride
                : GuessSpawnIdForScene(SceneManager.GetActiveScene().name),
        };

        return data;
    }

    public static void ApplyPendingToGame()
    {
        if (_pendingLoad == null)
            return;

        ApplyLoadedState(_pendingLoad);
        _pendingLoad = null;
    }

    public static void ApplyLoadedState(SaveGameData data)
    {
        if (data == null)
            return;

        GameSession.ImportSnapshot(data.money, data.inventory);
        PlayerHousingState.ImportOwned(data.ownedHouses);
        MissionProgress.ImportSnapshot(data.mission);
        HomeStorage.ImportAll(data.storages);
        ApplyNeedsSnapshot(data.needs);
        ApplyDayNightSnapshot(data.dayNight);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            GameSession.ApplyToPlayer(player);
    }

    static GameplayDayNightSnapshot ExportDayNightSnapshot()
    {
        GameplayDayNightCycle.Ensure();
        return GameplayDayNightCycle.Instance != null
            ? GameplayDayNightCycle.Instance.ExportSnapshot()
            : default;
    }

    static void ApplyDayNightSnapshot(GameplayDayNightSnapshot snapshot)
    {
        GameplayDayNightCycle.Ensure();
        if (GameplayDayNightCycle.Instance != null)
            GameplayDayNightCycle.Instance.ImportSnapshot(snapshot);
    }

    static PlayerNeedsSnapshot ExportNeedsSnapshot()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = PlayerScenePersistence.TravelingPlayer;

        var needs = player != null ? player.GetComponent<PlayerNeeds>() : null;
        return needs != null ? needs.ExportSnapshot() : DefaultNeedsSnapshot();
    }

    static PlayerNeedsSnapshot DefaultNeedsSnapshot()
    {
        var settings = RecomecoGameplaySettings.Instance;
        return new PlayerNeedsSnapshot
        {
            hunger = PlayerNeeds.MaxHunger,
            health = PlayerNeeds.MaxHealth,
            reputation = PlayerNeeds.MaxReputation,
            protection = settings != null ? settings.newGameProtection : 42f,
            illness = 0f,
        };
    }

    static void ApplyNeedsSnapshot(PlayerNeedsSnapshot snapshot)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = PlayerScenePersistence.TravelingPlayer;

        if (player == null)
            return;

        var needs = player.GetComponent<PlayerNeeds>();
        if (needs == null)
            needs = player.AddComponent<PlayerNeeds>();

        if (snapshot.health <= 0f && snapshot.hunger <= 0f && snapshot.reputation <= 0f)
            snapshot = DefaultNeedsSnapshot();

        needs.ImportSnapshot(snapshot);
    }

    static string GuessSpawnIdForScene(string sceneName)
    {
        if (sceneName == RecomecoSceneNames.InteriorCasaElegante)
            return RecomecoSceneNames.EntradaCasaElegante;

        if (sceneName == RecomecoSceneNames.Cidade)
            return RecomecoSceneNames.MoradiaInicial;

        if (sceneName == RecomecoSceneNames.FerroVelho)
            return RecomecoSceneNames.EntradaFerroVelho;

        return null;
    }
}
