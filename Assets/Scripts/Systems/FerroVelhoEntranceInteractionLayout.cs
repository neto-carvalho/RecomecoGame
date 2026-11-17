using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Alinha o portal do táxi no veículo, amplia o trigger e limita o alcance da venda.
/// </summary>
public static class FerroVelhoEntranceInteractionLayout
{
    const float EntranceSafeMargin = 1.25f;
    const float MinSellRadius = 2.25f;
    const float MaxSellRadius = 3f;
    const float TaxiPortalHorizontalRange = 2.75f;
    static readonly Vector3 TaxiPortalBoxSize = new(3.6f, 3.2f, 4.2f);
    static readonly Vector3 TaxiPortalBoxCenter = new(0f, 1.1f, 0f);
    const string SpawnId = "EntradaFerroVelho";
    const string TaxiNameToken = "vehicle24_interior";

    public static void Ensure(Scene scene)
    {
        if (!FerroVelhoWalkableGround.IsFerroVelhoScene(scene))
            return;

        var spawn = FindSpawn(scene);
        var entrance = spawn != null
            ? spawn.transform.position + spawn.positionOffset
            : (Vector3?)null;

        ConfigureReturnPortal(entrance);
        if (entrance.HasValue)
            CapSellRangeNearEntrance(entrance.Value);
    }

    static SceneSpawnPoint FindSpawn(Scene scene)
    {
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp == null || sp.gameObject.scene != scene)
                continue;
            if (sp.spawnId == SpawnId)
                return sp;
        }

        return null;
    }

    static void ConfigureReturnPortal(Vector3? entranceFallback)
    {
        var portal = GameObject.Find("Portal_VoltaCidade");
        if (portal == null)
            return;

        var taxi = FindTaxiTransform();
        var anchor = taxi != null ? taxi.position : entranceFallback ?? portal.transform.position;

        var pos = portal.transform.position;
        pos.x = anchor.x;
        pos.z = anchor.z;
        if (entranceFallback.HasValue)
            pos.y = entranceFallback.Value.y;
        portal.transform.position = pos;

        var box = portal.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.size = TaxiPortalBoxSize;
            box.center = TaxiPortalBoxCenter;
            box.isTrigger = true;
        }

        var zone = portal.GetComponent<SceneTransitionZone>();
        if (zone != null)
            zone.horizontalInteractionRange = TaxiPortalHorizontalRange;
    }

    /// <summary>
    /// Na porta de venda / perto do balcão, o táxi não deve roubar a legenda nem o E.
    /// </summary>
    public static bool ShouldPreferSellOverTaxi(Transform player)
    {
        if (player == null)
            return false;

        var sellGo = GameObject.Find("FerroVelho_Venda");
        if (sellGo == null)
            return false;

        var sell = sellGo.GetComponent<SellItems>();
        if (sell == null)
            return false;

        var taxi = FindTaxiTransform();
        var taxiPos = taxi != null
            ? taxi.position
            : sellGo.transform.position + Vector3.right * 10f;

        var toSell = HorizontalDistance(sellGo.transform.position, player.position);
        var toTaxi = HorizontalDistance(taxiPos, player.position);

        if (toSell <= sell.sellDistance)
            return true;

        return toSell + 0.75f < toTaxi;
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var da = new Vector2(a.x, a.z);
        var db = new Vector2(b.x, b.z);
        return Vector2.Distance(da, db);
    }

    static Transform FindTaxiTransform()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null)
                continue;
            if (t.name.IndexOf(TaxiNameToken, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }

        return null;
    }

    static void CapSellRangeNearEntrance(Vector3 entrance)
    {
        var sellGo = GameObject.Find("FerroVelho_Venda");
        if (sellGo == null)
            return;

        var sell = sellGo.GetComponent<SellItems>();
        if (sell == null)
            return;

        var sellPos = sellGo.transform.position;
        var planarDist = Vector2.Distance(
            new Vector2(sellPos.x, sellPos.z),
            new Vector2(entrance.x, entrance.z));

        var maxRange = Mathf.Max(
            MinSellRadius,
            planarDist - EntranceSafeMargin);
        maxRange = Mathf.Min(maxRange, MaxSellRadius);

        if (sell.sellDistance > maxRange)
            sell.sellDistance = maxRange;

        var scale = sellGo.transform.localScale;
        if (scale.x > 1.5f || scale.y > 1.5f || scale.z > 1.5f)
            sellGo.transform.localScale = Vector3.one;
    }
}