using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ao abrir a cena CartoonLowPolyCityLite_01, instancia NPCs decorativos.
/// Se existirem objetos com <see cref="SidewalkNpcSpawnPoint"/> na cena, usa-os (recomendado).
/// Caso contrário, usa posições embutidas (podem estar erradas para o teu layout) e avisa no Console.
/// </summary>
public sealed class CartoonLowPolyCityLiteNpcBootstrap : MonoBehaviour
{
    private const string TargetSceneName = "CartoonLowPolyCityLite_01";
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
        if (SceneManager.GetActiveScene().name != TargetSceneName)
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
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var sp = spawnPoints[i];
                var dir = sp.GetPatrolWorldDirection();
                var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir, Vector3.up) : Quaternion.identity;
                var instance = Instantiate(prefab, sp.transform.position, rot);
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

    private static void ConfigureNpcInstance(GameObject instance, int index, Vector3 patrolDir, float halfLength, float speed)
    {
        var walker = instance.GetComponent<SidewalkNpcWalker>();
        if (walker != null)
            walker.ConfigurePatrol(patrolDir, halfLength, speed, index);
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
