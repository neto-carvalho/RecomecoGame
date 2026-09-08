using System;

[Serializable]
public class SaveGameData
{
    public int money;
    public GameSession.SlotSnapshot[] inventory;
    public string[] ownedHouses;
    public HomeStorageSnapshot[] storages;
    public MissionProgressSnapshot mission;
    public string lastScene;
    public string lastSpawnId;
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
}
