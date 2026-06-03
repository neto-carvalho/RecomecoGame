using UnityEngine;

/// <summary>Detecta superfície a partir do raycast (Terrain splat, marcador ou nome do objeto).</summary>
public static class FootstepSurfaceResolver
{
    public static FootstepSurfaceType Resolve(in RaycastHit hit)
    {
        if (hit.collider == null)
            return FootstepSurfaceType.Default;

        var terrain = hit.collider.GetComponent<Terrain>();
        if (terrain != null && terrain.terrainData != null)
            return ResolveTerrainSplat(terrain, hit.point);

        var marker = hit.collider.GetComponent<FootstepSurfaceMarker>();
        if (marker == null)
            marker = hit.collider.GetComponentInParent<FootstepSurfaceMarker>();

        if (marker != null)
            return marker.Surface;

        var fromHit = FromObjectName(hit.collider.gameObject.name);
        if (fromHit != FootstepSurfaceType.Default)
            return fromHit;

        if (hit.collider.transform.root != null)
        {
            var fromRoot = FromObjectName(hit.collider.transform.root.name);
            if (fromRoot != FootstepSurfaceType.Default)
                return fromRoot;
        }

        return FootstepSurfaceType.Default;
    }

    public static FootstepSurfaceType FromObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return FootstepSurfaceType.Default;

        var n = objectName.ToLowerInvariant();

        if (IsTerrainObjectName(n))
            return FromNatureTerrainObjectName(n);

        if (IsWaterObjectName(n))
            return FootstepSurfaceType.Water;

        if (ContainsAny(n, "grass", "relva", "lawn", "park", "mato", "floresta", "forest", "tree", "arvore", "árvore"))
            return FootstepSurfaceType.Grass;

        if (ContainsAny(n, "snow", "neve", "ice", "gelo"))
            return FootstepSurfaceType.Snow;

        if (ContainsAny(n, "mud", "lama", "swamp", "pantano", "muddy"))
            return FootstepSurfaceType.Mud;

        if (ContainsAny(n, "sand", "areia", "beach", "praia"))
            return FootstepSurfaceType.Sand;

        if (ContainsAny(n, "wood", "madeira", "deck", "ponte", "bridge"))
            return FootstepSurfaceType.Wood;

        if (ContainsAny(n, "metal", "steel", "aco", "aço", "fence", "cerca"))
            return FootstepSurfaceType.Metal;

        if (ContainsAny(n, "rock", "pedra", "stone", "rocha", "cliff", "pebble", "soil"))
            return FootstepSurfaceType.Rock;

        if (ContainsAny(n, "leaf", "folha", "leaves"))
            return FootstepSurfaceType.Leaves;

        if (ContainsAny(n, "gravel", "gravilha", "dirt", "terra", "earth", "estrada"))
            return FootstepSurfaceType.Gravel;

        if (ContainsAny(n, "asphalt", "road", "street", "rua", "calçada", "calcada", "sidewalk",
                "pavement", "concrete", "concreto", "tile", "ladrilho", "piso"))
            return FootstepSurfaceType.Tile;

        return FootstepSurfaceType.Default;
    }

    static FootstepSurfaceType ResolveTerrainSplat(Terrain terrain, Vector3 worldPosition)
    {
        var data = terrain.terrainData;
        var layers = data.terrainLayers;
        if (layers == null || layers.Length == 0)
            return FromNatureTerrainObjectName(terrain.gameObject.name.ToLowerInvariant());

        var normX = (worldPosition.x - terrain.transform.position.x) / data.size.x;
        var normZ = (worldPosition.z - terrain.transform.position.z) / data.size.z;
        normX = Mathf.Clamp01(normX);
        normZ = Mathf.Clamp01(normZ);

        var mapX = Mathf.Clamp(Mathf.RoundToInt(normX * (data.alphamapWidth - 1)), 0, data.alphamapWidth - 1);
        var mapZ = Mathf.Clamp(Mathf.RoundToInt(normZ * (data.alphamapHeight - 1)), 0, data.alphamapHeight - 1);
        var weights = data.GetAlphamaps(mapX, mapZ, 1, 1);

        var dominant = 0;
        var maxWeight = weights[0, 0, 0];
        for (var i = 1; i < layers.Length && i < weights.GetLength(2); i++)
        {
            if (weights[0, 0, i] <= maxWeight)
                continue;
            maxWeight = weights[0, 0, i];
            dominant = i;
        }

        return FromTerrainLayerName(layers[dominant].name);
    }

    static FootstepSurfaceType FromTerrainLayerName(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
            return FootstepSurfaceType.Grass;

        var n = layerName.ToLowerInvariant();

        if (ContainsAny(n, "grass", "mato", "meadow", "turf"))
            return FootstepSurfaceType.Grass;

        if (ContainsAny(n, "mud", "muddy", "dirt", "soil", "clay"))
            return FootstepSurfaceType.Mud;

        if (ContainsAny(n, "sand", "beach", "tidal"))
            return FootstepSurfaceType.Sand;

        if (ContainsAny(n, "snow"))
            return FootstepSurfaceType.Snow;

        if (ContainsAny(n, "rock", "pebble", "stone", "gravel"))
            return FootstepSurfaceType.Gravel;

        if (ContainsAny(n, "road", "path", "estrada"))
            return FootstepSurfaceType.Gravel;

        return FootstepSurfaceType.Grass;
    }

    static FootstepSurfaceType FromNatureTerrainObjectName(string n)
    {
        if (ContainsAny(n, "estrada", "road", "path", "mud", "muddy", "dirt"))
            return FootstepSurfaceType.Mud;

        if (ContainsAny(n, "mato", "grass", "relva", "nature"))
            return FootstepSurfaceType.Grass;

        return FootstepSurfaceType.Grass;
    }

    static bool IsTerrainObjectName(string n) =>
        n.Contains("terrain") || n.Contains("natureterrain") || n.StartsWith("natureterrain_");

    static bool IsWaterObjectName(string n)
    {
        if (IsTerrainObjectName(n))
            return false;

        if (n.StartsWith("lake_") || n.EndsWith("_water") || n == "lake_water")
            return true;

        return ContainsAny(n, "water", "agua", "água", "underwater", "pool", "piscina", "river", "rio")
               && !n.Contains("terrain");
    }

    static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (haystack.Contains(needle))
                return true;
        }

        return false;
    }
}
