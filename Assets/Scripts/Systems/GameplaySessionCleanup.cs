using UnityEngine;

public static class GameplaySessionCleanup
{
    public static void ClearDontDestroyOnLoadGameplay()
    {
        if (CityTrafficManager.Instance != null)
            Object.Destroy(CityTrafficManager.Instance.gameObject);

        foreach (var starter in Object.FindObjectsByType<CityTrafficStarter>(FindObjectsSortMode.None))
        {
            if (starter != null)
                Object.Destroy(starter.gameObject);
        }

        var activeTraffic = GameObject.Find("ActiveCityTraffic");
        if (activeTraffic != null)
            Object.Destroy(activeTraffic);

    }
}
