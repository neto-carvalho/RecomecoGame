using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Instancia NPCs decorativos quando a cena tem <see cref="SidewalkNpcSpawnPoint"/>.
/// Funciona em qualquer cena (mapa novo, cidade, etc.). Sem marcadores, só tenta fallback na cena legada.
/// </summary>
public sealed class CartoonLowPolyCityLiteNpcBootstrap : MonoBehaviour
{
    private const string LegacySceneName = "CartoonLowPolyCityLite_01";
    /// <summary>Relativo a qualquer pasta <c>Resources</c> do projeto (aqui: Assets/Prefabs/NPC/Resources/SidewalkNpc).</summary>
    private const string NpcPrefabResourcePath = "SidewalkNpc";

    private static readonly NpcSpawnSpec[] s_FallbackSpecs =
    {
        new(new Vector3(-27.35f, 0.83f, 43f), new Vector3(0f, 0f, 1f), 6.5f, 1.25f),
        new(new Vector3(-27.35f, 0.83f, 54f), new Vector3(0f, 0f, -1f), 6.5f, 1.15f),
        new(new Vector3(-28.05f, 0.83f, 48f), new Vector3(1f, 0f, 0f), 4f, 1.3f),
        new(new Vector3(-26.85f, 0.83f, 49.5f), new Vector3(-1f, 0f, 0f), 3.5f, 1.2f),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
            return;

        var hasSpawnMarkers = FindObjectsByType<SidewalkNpcSpawnPoint>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0;
        var isLegacyScene = scene.name == LegacySceneName;

        if (!hasSpawnMarkers && !isLegacyScene)
            return;

        var host = new GameObject(nameof(CartoonLowPolyCityLiteNpcBootstrap));
        host.AddComponent<CartoonLowPolyCityLiteNpcBootstrap>();
    }

    private void Start()
    {
        var prefab = Resources.Load<GameObject>(NpcPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogError(
                $"{nameof(CartoonLowPolyCityLiteNpcBootstrap)}: prefab não encontrado. Confirme que existe o ficheiro " +
                $"Assets/Prefabs/NPC/Resources/{NpcPrefabResourcePath}.prefab (Resources.Load usa o nome sem extensão).");
            Destroy(gameObject);
            return;
        }

        var spawnPoints = FindObjectsByType<SidewalkNpcSpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Array.Sort(spawnPoints, (a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

        if (spawnPoints.Length > 0)
        {
            Debug.Log($"[NPC] A spawnar {spawnPoints.Length} NPC(s) em '{SceneManager.GetActiveScene().name}'.");

            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                var dir = sp.GetPatrolWorldDirection();
                var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir, Vector3.up) : Quaternion.identity;
                var spawnPos = SnapSpawnToGround(sp.transform.position);
                var instance = Instantiate(prefab, spawnPos, rot);
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
                var instance = Instantiate(prefab, spec.Position, rot);
                instance.name = $"SidewalkNpc_{i + 1} (fallback)";

                ConfigureNpcInstance(instance, i, spec.PatrolDirection, spec.HalfLength, spec.Speed);
            }
        }

        Destroy(gameObject);
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

    private static void ConfigureNpcInstance(GameObject instance, int index, Vector3 patrolDir, float halfLength, float speed)
    {
        var worldScale = GetPlayerWorldScale();
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

    /// <summary>Escala dos NPCs igual à do jogador (ex.: Player scale 0.3).</summary>
    static float GetPlayerWorldScale()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return 1f;
        return Mathf.Max(0.15f, player.transform.lossyScale.y);
    }

    private readonly struct NpcSpawnSpec
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
