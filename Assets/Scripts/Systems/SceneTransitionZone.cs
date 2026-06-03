using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Zona que leva o jogador a outra cena (ex.: cidade → ferro velho). Aperte E dentro do trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneTransitionZone : MonoBehaviour
{
    [Tooltip("Nome da cena no Build Settings (ex.: FerroVelho)")]
    public string targetSceneName = RecomecoSceneNames.FerroVelho;

    [Tooltip("Spawn na cena de destino (SceneSpawnPoint.spawnId)")]
    public string targetSpawnId = "EntradaFerroVelho";

    [Tooltip("Mensagem na UI ao entrar na zona")]
    public string messageNear = "Aperte E para entrar";

    public KeyCode interactKey = KeyCode.E;

    bool _playerInside;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = true;
        InteractionUI.ShowMessage(messageNear);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = false;
        InteractionUI.HideMessage();
    }

    void Update()
    {
        if (!_playerInside || !Input.GetKeyDown(interactKey))
            return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("SceneTransitionZone: targetSceneName vazio.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogError(
                "SceneTransitionZone: cena '" + targetSceneName +
                "' não está em File → Build Settings. Adicione Assets/Scenes/FerroVelho.unity.");
            return;
        }

        GameSession.SaveBeforeSceneLoad();
        PlayerScenePersistence.PrepareForSceneLoad();
        SceneTransitionState.SetNextSpawn(targetSpawnId);

        InteractionUI.HideMessage();

        SceneManager.LoadScene(targetSceneName);
    }
}
