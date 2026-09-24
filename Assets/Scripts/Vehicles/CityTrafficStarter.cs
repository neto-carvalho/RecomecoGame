using System.Collections;
using UnityEngine;

/// <summary>
/// Atrasa o tráfego até coliders e rotas estarem prontos na Cidade.
/// </summary>
public sealed class CityTrafficStarter : MonoBehaviour
{
    public static void Schedule()
    {
        if (FindFirstObjectByType<CityTrafficStarter>() != null)
            return;

        var go = new GameObject("_CityTrafficStarter");
        DontDestroyOnLoad(go);
        go.AddComponent<CityTrafficStarter>();
    }

    IEnumerator Start()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        TrafficRoute.RealignAllRoutesInScene();
        foreach (var route in FindObjectsByType<TrafficRoute>(FindObjectsSortMode.None))
            route.EnsureReady();

        var count = CityTrafficManager.EnsureInstance().AssignAllTraffic();
        Debug.Log("[Tráfego] Inicialização tardia concluída. Veículos ativos (rota + ruas): " + count);

        Destroy(gameObject);
    }
}
