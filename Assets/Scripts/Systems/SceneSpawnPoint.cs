using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Marca onde o jogador aparece ao entrar na cena (id deve coincidir com o portal de origem).
/// </summary>
[DefaultExecutionOrder(500)]
public class SceneSpawnPoint : MonoBehaviour
{
    [Tooltip("Identificador único nesta cena (ex.: EntradaCidade, EntradaFerroVelho)")]
    public string spawnId = "Default";

    [Tooltip("Deslocamento extra em relação ao marcador")]
    public Vector3 positionOffset;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var pos = transform.position + positionOffset;
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.85f);
        Gizmos.DrawSphere(pos, 0.6f);
        Gizmos.DrawLine(pos, pos + transform.forward * 2f);
        Handles.Label(pos + Vector3.up * 1.2f, "Spawn: " + spawnId);
    }
#endif

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        yield return null;

        if (FerroVelhoWalkableGround.IsFerroVelhoActive())
            FerroVelhoWalkableGround.EnsureInActiveScene();

        if (!string.IsNullOrEmpty(SceneTransitionState.PendingSpawnId) &&
            SceneTransitionState.PendingSpawnId == spawnId)
        {
            SceneTransitionState.TryApplyPendingSpawn();
            yield break;
        }

        var pending = SceneTransitionState.ConsumeSpawnId();
        if (string.IsNullOrEmpty(pending) || pending != spawnId)
            yield break;

        var player = PlayerScenePersistence.ResolvePlayerInLoadedScene();
        if (player == null)
            yield break;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            settings.ApplyPlayerScaleForScene(player, UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        var spawnPos = transform.position + positionOffset;
        SpawnGroundUtility.PlacePlayerOnGround(player, spawnPos);
        player.transform.rotation = transform.rotation;
        GameSession.ApplyToPlayer(player);
        SceneTransitionPlayerSetup.AfterSceneLoad(player);
    }
}
