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

    Collider _trigger;
    bool _playerInside;

    void Awake()
    {
        _trigger = GetComponent<Collider>();
        EnsureTriggerSetup();
    }

    void Reset()
    {
        EnsureTriggerSetup();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        SetInside(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        if (!IsPlayerPhysicallyInside())
            SetInside(false);
    }

    void LateUpdate()
    {
        var inside = IsPlayerPhysicallyInside();
        if (inside != _playerInside)
            SetInside(inside);

        if (!_playerInside || !inside || !Input.GetKeyDown(interactKey))
            return;

        TryLoadTargetScene();
    }

    void SetInside(bool inside)
    {
        if (_playerInside == inside)
            return;

        _playerInside = inside;
        if (inside)
            InteractionUI.ShowMessage(messageNear, this);
        else
            InteractionUI.HideMessage(this);
    }

    void TryLoadTargetScene()
    {
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

        InteractionUI.HideMessage(this);

        SceneManager.LoadScene(targetSceneName);
    }

    bool IsPlayerPhysicallyInside()
    {
        if (_trigger == null || !_trigger.enabled || !_trigger.isTrigger)
            return false;

        var player = FindPlayerRoot();
        if (player == null)
            return false;

        var bounds = _trigger.bounds;
        var cc = player.GetComponent<CharacterController>();

        if (cc != null)
        {
            var feet = player.transform.position + player.transform.TransformVector(cc.center);
            feet.y -= cc.height * 0.5f * Mathf.Abs(player.transform.lossyScale.y);
            var head = feet + Vector3.up * cc.height * Mathf.Abs(player.transform.lossyScale.y);

            if (bounds.Contains(feet) || bounds.Contains(head) || bounds.Contains(player.transform.position))
                return true;

            return bounds.Intersects(new Bounds(
                player.transform.position + player.transform.TransformVector(cc.center),
                new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f) * player.transform.lossyScale.y));
        }

        return bounds.Contains(player.transform.position);
    }

    static GameObject FindPlayerRoot()
    {
        var traveling = PlayerScenePersistence.TravelingPlayer;
        if (traveling != null)
            return traveling;

        return GameObject.FindGameObjectWithTag("Player");
    }

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        if (other.GetComponent<CharacterController>() != null)
            return true;

        if (other.GetComponentInParent<CharacterController>() != null)
            return true;

        return other.transform.root.CompareTag("Player");
    }

    void EnsureTriggerSetup()
    {
        if (_trigger == null)
            _trigger = GetComponent<Collider>();

        if (_trigger == null)
            return;

        _trigger.isTrigger = true;
    }
}
