using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissionObjectiveLocator
{
    public struct ObjectiveTarget
    {
        public bool HasTarget;
        public Vector3 WorldPosition;
        public string Label;
    }

    public static ObjectiveTarget GetCurrentTarget()
    {
        if (!MissionProgress.IsActive)
            return default;

        switch (MissionProgress.Current)
        {
            case MissionId.CollectCans:
            case MissionId.CollectCansForHouse:
            case MissionId.AllComplete:
            case MissionId.EatWhenHungry:
                return default;

            case MissionId.SellAtJunkyard:
            case MissionId.SellAtJunkyardForHouse:
                return ResolveSellAtJunkyardTarget();

            case MissionId.GoToCity:
                return ResolvePortalTarget(RecomecoSceneNames.Cidade, "Portal_VoltaCidade", "Táxi (cidade)");

            case MissionId.KnowYourShelter:
            case MissionId.RestAtBarraca:
                return ResolveBarracaTarget();

            case MissionId.BuyAtShop:
                return ResolveResellShopTarget();

            case MissionId.BuyMealAtFood4U:
                return ResolveFood4UTarget();

            case MissionId.Resell:
                return ResolveNearestResellTarget();

            case MissionId.BuyHouse:
                return ResolveHouseDoorTarget();

            case MissionId.RestInSafeBed:
                return ResolveSafeBedTarget();

            default:
                return default;
        }
    }

    static ObjectiveTarget ResolveResellShopTarget()
    {
        foreach (var shop in Object.FindObjectsByType<ShopZone>(FindObjectsSortMode.None))
        {
            if (shop == null || shop.shopKind != ShopZone.ShopKind.Resell)
                continue;

            return new ObjectiveTarget
            {
                HasTarget = true,
                WorldPosition = GetGroundPoint(shop.transform),
                Label = string.IsNullOrEmpty(shop.shopTitle) ? "Lojinha" : shop.shopTitle,
            };
        }

        return ResolveNamedTarget("Lojinha", "Lojinha", typeof(ShopZone));
    }

    static ObjectiveTarget ResolveFood4UTarget()
    {
        foreach (var shop in Object.FindObjectsByType<ShopZone>(FindObjectsSortMode.None))
        {
            if (shop == null || shop.shopKind != ShopZone.ShopKind.FastFood)
                continue;

            return new ObjectiveTarget
            {
                HasTarget = true,
                WorldPosition = GetGroundPoint(shop.transform),
                Label = "Lanchonete",
            };
        }

        return ResolveNamedTarget("FOOD4U", "Lanchonete", typeof(ShopZone));
    }

    static ObjectiveTarget ResolveBarracaTarget()
    {
        var sleep = Object.FindFirstObjectByType<PrecariousSleepInteract>();
        if (sleep != null)
        {
            return new ObjectiveTarget
            {
                HasTarget = true,
                WorldPosition = GetGroundPoint(sleep.transform),
                Label = "Barraca",
            };
        }

        return ResolveNamedTarget(
            StreetPropsSceneColliders.MoradiaRootName,
            "Moradia precária",
            null);
    }

    static ObjectiveTarget ResolveHouseDoorTarget()
    {
        foreach (var door in Object.FindObjectsByType<HouseDoorInteract>(FindObjectsSortMode.None))
        {
            if (door == null || door.housingId != PlayerHousingState.CasaElegante)
                continue;

            return new ObjectiveTarget
            {
                HasTarget = true,
                WorldPosition = GetGroundPoint(door.transform),
                Label = door.houseDisplayName,
            };
        }

        return ResolveNamedTarget(RecomecoSceneNames.CasaEleganteRootName, "Casa elegante", typeof(HouseDoorInteract));
    }

    static ObjectiveTarget ResolveSafeBedTarget()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name == RecomecoSceneNames.InteriorCasaElegante)
        {
            var bed = Object.FindFirstObjectByType<BedSaveInteract>();
            if (bed != null)
            {
                return new ObjectiveTarget
                {
                    HasTarget = true,
                    WorldPosition = GetGroundPoint(bed.transform),
                    Label = "Cama",
                };
            }
        }

        return ResolveHouseDoorTarget();
    }

    static ObjectiveTarget ResolveSellAtJunkyardTarget()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == RecomecoSceneNames.FerroVelho)
            return ResolveNamedTarget("FerroVelho_Venda", "Venda no ferro velho", typeof(SellItems));

        return ResolvePortalTarget(RecomecoSceneNames.FerroVelho, "Portal_FerroVelho", "Táxi (ferro velho)");
    }

    static ObjectiveTarget ResolvePortalTarget(string targetScene, string objectName, string label)
    {
        var portal = FindPortalToScene(targetScene);
        if (portal == null)
            portal = FindTransformByName(objectName);

        if (portal == null)
            return default;

        return new ObjectiveTarget
        {
            HasTarget = true,
            WorldPosition = GetGroundPoint(portal),
            Label = label,
        };
    }

    static ObjectiveTarget ResolveNamedTarget(string objectName, string label, System.Type componentFallback)
    {
        var transform = FindTransformByName(objectName);
        if (transform == null && componentFallback != null)
            transform = FindFirstTransformWithComponent(componentFallback);

        if (transform == null)
            return default;

        return new ObjectiveTarget
        {
            HasTarget = true,
            WorldPosition = GetGroundPoint(transform),
            Label = label,
        };
    }

    static ObjectiveTarget ResolveNearestResellTarget()
    {
        if (SceneManager.GetActiveScene().name != RecomecoSceneNames.Cidade)
            return default;

        var player = ResolvePlayerPosition();
        if (!player.HasValue)
            return default;

        Transform best = null;
        var bestDist = float.MaxValue;
        var bestLabel = "Ponto de venda";

        foreach (var zone in Object.FindObjectsByType<StreetSellZone>(FindObjectsSortMode.None))
        {
            if (zone == null)
                continue;

            var dist = PlanarDistance(player.Value, zone.transform.position);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = zone.transform;
            bestLabel = "Venda na rua";
        }

        foreach (var npc in Object.FindObjectsByType<NpcSellInteraction>(FindObjectsSortMode.None))
        {
            if (npc == null)
                continue;

            var dist = PlanarDistance(player.Value, npc.transform.position);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = npc.transform;
            bestLabel = "Pedestre";
        }

        if (best == null)
            return default;

        return new ObjectiveTarget
        {
            HasTarget = true,
            WorldPosition = GetGroundPoint(best),
            Label = bestLabel,
        };
    }

    static Transform FindPortalToScene(string targetScene)
    {
        foreach (var zone in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
        {
            if (zone == null || zone.targetSceneName != targetScene)
                continue;

            return zone.transform;
        }

        return null;
    }

    static Transform FindTransformByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        return go != null ? go.transform : null;
    }

    static Transform FindFirstTransformWithComponent(System.Type type)
    {
        var component = Object.FindFirstObjectByType(type) as Component;
        return component != null ? component.transform : null;
    }

    static Vector3? ResolvePlayerPosition()
    {
        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return player.transform.position;
    }

    static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static Vector3 GetGroundPoint(Transform target)
    {
        if (target == null)
            return Vector3.zero;

        var pos = target.position;
        pos.y += 1.5f;
        return pos;
    }
}
