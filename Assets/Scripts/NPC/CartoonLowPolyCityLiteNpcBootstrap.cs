using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Instancia NPCs decorativos quando a cena tem <see cref="SidewalkNpcSpawnPoint"/>.
/// Funciona em Cidade, na cena legada do pack e em qualquer mapa com marcadores de spawn.
/// </summary>
public sealed class CartoonLowPolyCityLiteNpcBootstrap : MonoBehaviour
{
    const string LegacySceneName = "CartoonLowPolyCityLite_01";
    const string RuntimeNpcRootName = "Runtime_SidewalkNpcs";
    const string NpcPrefabResourcePath = "SidewalkNpc";

    static bool _sceneHookRegistered;
    static int _spawnedForSceneHandle = int.MinValue;

    static readonly NpcSpawnSpec[] s_FallbackSpecs =
    {
        new(new Vector3(-27.35f, 0.83f, 43f), new Vector3(0f, 0f, 1f), 6.5f, 1.25f),
        new(new Vector3(-27.35f, 0.83f, 54f), new Vector3(0f, 0f, -1f), 6.5f, 1.15f),
        new(new Vector3(-28.05f, 0.83f, 48f), new Vector3(1f, 0f, 0f), 4f, 1.3f),
        new(new Vector3(-26.85f, 0.83f, 49.5f), new Vector3(-1f, 0f, 0f), 3.5f, 1.2f),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterSceneHook()
    {
        if (_sceneHookRegistered)
            return;

        _sceneHookRegistered = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ScheduleSpawnForScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _spawnedForSceneHandle = int.MinValue;
        ScheduleSpawnForScene(scene);
    }

    static void ScheduleSpawnForScene(Scene scene)
    {
        if (!ShouldSpawnInScene(scene))
            return;

        var host = new GameObject(nameof(CartoonLowPolyCityLiteNpcBootstrap));
        host.AddComponent<CartoonLowPolyCityLiteNpcBootstrap>();
    }

    static bool ShouldSpawnInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        if (RecomecoSceneNames.IsMenuScene(scene))
            return false;

        if (scene.name == RecomecoSceneNames.Cidade || scene.name == LegacySceneName)
            return true;

        return FindSpawnPointsInScene(scene).Length > 0;
    }

    void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    IEnumerator SpawnWhenReady()
    {
        yield return null;
        yield return null;

        var scene = SceneManager.GetActiveScene();
        if (!ShouldSpawnInScene(scene))
        {
            Destroy(gameObject);
            yield break;
        }

        if (_spawnedForSceneHandle == scene.handle)
        {
            Destroy(gameObject);
            yield break;
        }

        var prefab = Resources.Load<GameObject>(NpcPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError(
                $"{nameof(CartoonLowPolyCityLiteNpcBootstrap)}: prefab não encontrado. Confirme que existe o ficheiro " +
                $"Assets/Prefabs/NPC/Resources/{NpcPrefabResourcePath}.prefab (Resources.Load usa o nome sem extensão).");
            Destroy(gameObject);
            yield break;
        }

        ClearRuntimeNpcsInScene(scene);

        var npcRoot = new GameObject(RuntimeNpcRootName);
        SceneManager.MoveGameObjectToScene(npcRoot, scene);

        var spawnPoints = FindSpawnPointsInScene(scene);
        Array.Sort(spawnPoints, (a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

        if (spawnPoints.Length > 0)
        {
            Debug.Log($"[NPC] A spawnar {spawnPoints.Length} NPC(s) em '{scene.name}'.");

            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                if (sp == null || !sp.isActiveAndEnabled)
                    continue;

                var dir = sp.GetPatrolWorldDirection();
                var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir, Vector3.up) : Quaternion.identity;
                var spawnPos = SnapSpawnToGround(sp.transform.position);
                var instance = Instantiate(prefab, spawnPos, rot, npcRoot.transform);
                instance.name = $"SidewalkNpc_{i + 1}";

                ConfigureNpcInstance(instance, i, dir, sp.PatrolHalfLength, sp.WalkSpeed);
            }
        }
        else
        {
            Debug.LogWarning(
                "[NPC] Nenhum SidewalkNpcSpawnPoint na cena — a usar posições embutidas (podem estar dentro de prédios). " +
                "Menu: Recomeco → NPC → Criar marcadores de spawn; coloca-os na calçada e guarda a cena.");

            for (var i = 0; i < s_FallbackSpecs.Length; i++)
            {
                var spec = s_FallbackSpecs[i];
                var dir = spec.PatrolDirection;
                dir.y = 0f;
                var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir.normalized, Vector3.up) : Quaternion.identity;
                var instance = Instantiate(prefab, spec.Position, rot, npcRoot.transform);
                instance.name = $"SidewalkNpc_{i + 1} (fallback)";

                ConfigureNpcInstance(instance, i, spec.PatrolDirection, spec.HalfLength, spec.Speed);
            }
        }

        _spawnedForSceneHandle = scene.handle;
        Destroy(gameObject);
    }

    static SidewalkNpcSpawnPoint[] FindSpawnPointsInScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return Array.Empty<SidewalkNpcSpawnPoint>();

        var list = new List<SidewalkNpcSpawnPoint>();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var sp in root.GetComponentsInChildren<SidewalkNpcSpawnPoint>(true))
            {
                if (sp != null)
                    list.Add(sp);
            }
        }

        return list.Count > 0 ? list.ToArray() : Array.Empty<SidewalkNpcSpawnPoint>();
    }

    static void ClearRuntimeNpcsInScene(Scene scene)
    {
        if (!scene.IsValid())
            return;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root != null && root.name == RuntimeNpcRootName)
                Destroy(root);
        }
    }

    static Vector3 SnapSpawnToGround(Vector3 position)
    {
        const float rayStart = 80f;
        const float rayDistance = 200f;
        var origin = position + Vector3.up * rayStart;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * 0.03f;

        Debug.LogWarning(
            $"[NPC] Raycast não encontrou chão em {position}. Ajusta Y do marcador na calçada.");
        return position;
    }

    static void ConfigureNpcInstance(GameObject instance, int index, Vector3 patrolDir, float halfLength, float speed)
    {
        var worldScale = GetNpcWorldScale();
        instance.transform.localScale = Vector3.one * worldScale;

        var walker = instance.GetComponent<SidewalkNpcWalker>();
        if (walker != null)
            walker.ConfigurePatrol(patrolDir, halfLength * worldScale, speed * Mathf.Sqrt(worldScale), index);

        var cc = instance.GetComponent<CharacterController>();
        if (cc != null)
        {
            CharacterGroundSnap.FitControllerToWorldScale(cc, 2f, new Vector3(0f, 1f, 0f), 0.35f, 0.25f, 0.08f);
            CharacterGroundSnap.TrySnap(instance.transform, cc);
        }
    }

    static float GetNpcWorldScale()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            return Mathf.Max(0.15f, settings.GetPlayerScaleForActiveScene());

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return 1f;

        return Mathf.Max(0.15f, player.transform.lossyScale.y);
    }

    readonly struct NpcSpawnSpec
    {
        public readonly Vector3 Position;
        public readonly Vector3 PatrolDirection;
        public readonly float HalfLength;
        public readonly float Speed;

        public NpcSpawnSpec(Vector3 position, Vector3 patrolDirection, float halfLength, float speed)
        {
            Position = position;
            PatrolDirection = patrolDirection;
            HalfLength = halfLength;
            Speed = speed;
        }
    }
}
