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

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            GameSession.ApplyToPlayer(player);
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
