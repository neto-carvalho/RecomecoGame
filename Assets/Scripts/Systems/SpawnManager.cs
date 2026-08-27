using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    public GameObject latinhaPrefab;

    [Tooltip("Centro da área de spawn no plano XZ. Se vazio, usa a posição deste GameObject (SpawnManager).")]
    public Transform areaCenter;

    [Tooltip("Quantidade de latinhas ao iniciar a cena")]
    public int quantidadeInicial = 50;
    [Tooltip("Raio da área de spawn (em unidades)")]
    public float areaSpawn = 35f;
    [Tooltip("Segundos para uma nova latinha aparecer após uma ser coletada")]
    public float tempoRespawn = 8f;

    [Header("Chão (cidade / terreno)")]
    [Tooltip("Lança um raio para baixo e posiciona a latinha no chão.")]
    public bool snapToGround = true;
    [Tooltip("Altura inicial do raio acima do ponto aleatório")]
    public float raycastStartHeight = 120f;
    [Tooltip("Distância máxima do raio para baixo")]
    public float raycastMaxDistance = 250f;
    [Tooltip("Elevação acima do ponto de impacto")]
    public float heightAboveGround = 0.015f;
    [Tooltip("Camadas consideradas como chão (deixe Everything se não souber)")]
    public LayerMask groundLayers = ~0;
    [Tooltip("Inclinação máxima do chão (graus)")]
    public float maxGroundSlope = 42f;
    [Tooltip("Tentativas por latinha antes de desistir")]
    public int maxAttemptsPerSpawn = 16;

    void Awake()
    {
        if (!RecomecoSceneNames.AllowsLatinhaSpawn(SceneManager.GetActiveScene()))
        {
            enabled = false;
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Start()
    {
        if (!enabled)
            return;

        StartCoroutine(SpawnInitialBatch());
    }

    IEnumerator SpawnInitialBatch()
    {
        yield return null;

        if (FerroVelhoWalkableGround.IsFerroVelhoActive())
        {
            FerroVelhoWalkableGround.EnsureInActiveScene();
            FerroVelhoSceneGround.EnsureSceneGroundColliders();
        }

        if (latinhaPrefab == null)
        {
            UnityEngine.Debug.LogError("SpawnManager: arraste o prefab 'Latinha' no campo Latinha Prefab. Nada será spawnado até corrigir.");
            yield break;
        }

        var spawned = 0;
        for (var i = 0; i < quantidadeInicial; i++)
        {
            if (SpawnLatinha())
                spawned++;
        }

        if (spawned < quantidadeInicial)
            UnityEngine.Debug.LogWarning(
                "SpawnManager: spawnou " + spawned + "/" + quantidadeInicial +
                " latinhas. Ajuste areaSpawn ou maxAttemptsPerSpawn se faltar chão válido.");
    }

    public bool SpawnLatinha()
    {
        if (!RecomecoSceneNames.AllowsLatinhaSpawn(SceneManager.GetActiveScene()))
            return false;

        if (latinhaPrefab == null)
        {
            UnityEngine.Debug.LogWarning("SpawnManager.SpawnLatinha: Latinha Prefab não configurado.");
            return false;
        }

        if (!TryFindSpawnPosition(out var posicao, out var rotacao))
            return false;

        var instance = Instantiate(latinhaPrefab, posicao, rotacao);
        var placement = instance.GetComponent<LatinhaPlacement>();
        if (placement == null)
            placement = instance.AddComponent<LatinhaPlacement>();
        placement.AlignToGround();
        return true;
    }

    public void RespawnLatinha()
    {
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(tempoRespawn);
        SpawnLatinha();
    }

    bool TryFindSpawnPosition(out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        var center = GetSpawnCenter();

        for (var attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            var planar = GetRandomPlanarPoint(center);
            if (!IsInsideAllowedArea(planar))
                continue;

            if (!TryResolveGroundHeight(planar, out var groundY))
                continue;

            var candidate = new Vector3(planar.x, groundY + heightAboveGround, planar.z);
            if (IsOverWater(candidate))
                continue;

            position = candidate;
            rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            return true;
        }

        return false;
    }

    Vector3 GetSpawnCenter()
    {
        if (areaCenter != null)
            return areaCenter.position;

        if (FerroVelhoWalkableGround.IsFerroVelhoActive())
            return FerroVelhoWalkableGround.Center;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            return player.transform.position;

        return transform.position;
    }

    Vector3 GetRandomPlanarPoint(Vector3 center)
    {
        var offset = UnityEngine.Random.insideUnitCircle * areaSpawn;
        return new Vector3(center.x + offset.x, center.y, center.z + offset.y);
    }

    bool IsInsideAllowedArea(Vector3 planar)
    {
        if (!FerroVelhoWalkableGround.IsFerroVelhoActive())
            return true;

        var c = FerroVelhoWalkableGround.Center;
        var size = FerroVelhoWalkableGround.Size;
        var margin = 4f;
        return Mathf.Abs(planar.x - c.x) <= size.x * 0.5f - margin &&
               Mathf.Abs(planar.z - c.z) <= size.z * 0.5f - margin;
    }

    bool TryResolveGroundHeight(Vector3 planar, out float groundY)
    {
        groundY = planar.y;

        if (TrySampleUnityTerrain(planar, out groundY))
            return true;

        if (!snapToGround)
            return true;

        if (!TryRaycastGround(planar, out var hit))
            return false;

        groundY = hit.point.y;
        return true;
    }

    static bool TrySampleUnityTerrain(Vector3 planar, out float groundY)
    {
        groundY = float.MinValue;
        var found = false;

        foreach (var terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            var local = planar - pos;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z)
                continue;

            var y = terrain.SampleHeight(planar) + pos.y;
            if (!found || y > groundY)
            {
                groundY = y;
                found = true;
            }
        }

        return found;
    }

    bool TryRaycastGround(Vector3 planar, out RaycastHit bestHit)
    {
        bestHit = default;

        var origin = planar + Vector3.up * raycastStartHeight;
        var hits = Physics.RaycastAll(origin, Vector3.down, raycastMaxDistance, groundLayers,
            QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        var foundPreferred = false;
        var foundFallback = false;
        var bestPreferredY = float.MinValue;
        var bestFallbackY = float.MinValue;

        foreach (var hit in hits)
        {
            if (!IsWalkableHit(hit, maxGroundSlope))
                continue;

            if (IsPreferredGroundCollider(hit.collider))
            {
                if (hit.point.y > bestPreferredY)
                {
                    bestPreferredY = hit.point.y;
                    bestHit = hit;
                    foundPreferred = true;
                }
            }
            else if (!foundPreferred && hit.point.y > bestFallbackY)
            {
                bestFallbackY = hit.point.y;
                bestHit = hit;
                foundFallback = true;
            }
        }

        return foundPreferred || foundFallback;
    }

    static bool IsWalkableHit(RaycastHit hit, float maxSlope)
    {
        if (hit.collider == null)
            return false;

        if (hit.collider.isTrigger)
            return false;

        if (IsIgnoredCollider(hit.collider))
            return false;

        if (IsBuildingOrRoofCollider(hit.collider))
            return false;

        var slope = Vector3.Angle(hit.normal, Vector3.up);
        return slope <= maxSlope;
    }

    static bool IsPreferredGroundCollider(Collider col)
    {
        if (col is TerrainCollider)
            return true;

        var n = col.name;
        if (n.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("rua", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("chao", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("chão", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("nature", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("bg", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("surface", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("floor", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var parent = col.transform.parent;
        while (parent != null)
        {
            var pn = parent.name;
            if (pn.IndexOf("Ruas", StringComparison.OrdinalIgnoreCase) >= 0 ||
                pn.IndexOf("Enviroment", StringComparison.OrdinalIgnoreCase) >= 0 ||
                pn.IndexOf("Environment", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            parent = parent.parent;
        }

        return false;
    }

    static bool IsBuildingOrRoofCollider(Collider col)
    {
        if (col == null)
            return false;

        var n = col.name;
        if (n.IndexOf("house", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("building", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("predio", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("prédio", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("bridge", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("ponte", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("roof", StringComparison.OrdinalIgnoreCase) >= 0 ||
            n.IndexOf("telhado", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var walk = col.transform;
        while (walk != null)
        {
            var pn = walk.name;
            if (pn.IndexOf("Buildings", StringComparison.OrdinalIgnoreCase) >= 0 ||
                pn.IndexOf("House", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            walk = walk.parent;
        }

        return false;
    }

    static bool IsIgnoredCollider(Collider col)
    {
        if (col == null)
            return true;

        if (col.GetComponent<CharacterController>() != null)
            return true;

        if (col.CompareTag("Player"))
            return true;

        var name = col.name;
        if (name.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("lake", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("agua", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("água", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("river", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (col.GetComponent<LakeWaterZone>() != null)
            return true;

        return false;
    }

    static bool IsOverWater(Vector3 worldPos)
    {
        var lakes = FindObjectsByType<LakeWaterZone>(FindObjectsSortMode.None);
        foreach (var lake in lakes)
        {
            if (lake == null)
                continue;

            var t = lake.transform;
            var local = t.InverseTransformPoint(worldPos);
            var halfX = 5f * Mathf.Abs(t.lossyScale.x);
            var halfZ = 5f * Mathf.Abs(t.lossyScale.z);
            if (Mathf.Abs(local.x) > halfX || Mathf.Abs(local.z) > halfZ)
                continue;

            if (worldPos.y < t.position.y + 0.25f)
                return true;
        }

        var lakeObj = GameObject.Find("Lake_Water");
        if (lakeObj != null)
        {
            var t = lakeObj.transform;
            var local = t.InverseTransformPoint(worldPos);
            var halfX = 5f * Mathf.Abs(t.lossyScale.x);
            var halfZ = 5f * Mathf.Abs(t.lossyScale.z);
            if (Mathf.Abs(local.x) <= halfX && Mathf.Abs(local.z) <= halfZ &&
                worldPos.y < t.position.y + 0.25f)
                return true;
        }

        return false;
    }
}
