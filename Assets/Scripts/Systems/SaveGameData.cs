using System;

[Serializable]
public class SaveGameData
{
    public int money;
    public GameSession.SlotSnapshot[] inventory;
    public string[] ownedHouses;
    public HomeStorageSnapshot[] storages;
    public MissionProgressSnapshot mission;
    public PlayerNeedsSnapshot needs;
    public GameplayDayNightSnapshot dayNight;
    public string lastScene;
    public string lastSpawnId;
}

[Serializable]
public struct PlayerNeedsSnapshot
{
    public float hunger;
    public float health;
    public float reputation;
    public float protection;
    public float illness;
}

[Serializable]
public struct HomeStorageSnapshot
{
    public string storageId;
    public GameSession.SlotSnapshot[] slots;
}

[Serializable]
public struct MissionProgressSnapshot
{
    public bool started;
    public int currentMission;
    public int junkyardSoldCount;
    public int lastReportedCollectCount;
    /// <summary>0 = cadeia antiga; 1 = FOOD4U + revenda; 2 = latinhas antes da casa.</summary>
    public int schemaVersion;
    public int resellEarningsCents;
    public int junkyardSoldForHouseCount;
    public int lastReportedHouseCollectCount;
}
