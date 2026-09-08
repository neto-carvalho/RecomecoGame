using UnityEngine;
using UnityEngine.SceneManagement;

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

    void OnDisable()
    {
        SetInside(false);
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
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

        SetInside(false);
    }

    void LateUpdate()
    {
        var player = FindPlayerRoot();
        var inside = player != null && InteractionProximity.IsInsideTrigger(_trigger, player.transform);

        if (inside != _playerInside)
            SetInside(inside);

        if (!_playerInside)
        {
            InteractionUI.HideMessage(this);
            return;
        }

        if (!Input.GetKeyDown(interactKey))
            return;

        TryLoadTargetScene();
    }

    void SetInside(bool inside)
    {
        if (_playerInside == inside)
            return;

        _playerInside = inside;
        if (inside)
            InteractionUI.ShowMessage(messageNear, this, InteractionUI.PriorityNavigation);
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

    public bool IsPointInsideTrigger(Vector3 worldPoint)
    {
        return InteractionProximity.IsWorldPointInsideTrigger(_trigger, worldPoint);
    }

    static GameObject FindPlayerRoot() => InteractionProximity.GetPlayer();

    public static Vector3 ResolveSpawnOutsideZones(Vector3 position, Quaternion rotation)
    {
        const float step = 3f;
        const int maxAttempts = 4;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var insideZone = false;
            foreach (var zone in Object.FindObjectsByType<SceneTransitionZone>(FindObjectsSortMode.None))
            {
                if (zone == null || !zone.isActiveAndEnabled)
                    continue;

                var trigger = zone.GetComponent<Collider>();
                if (trigger == null || !trigger.enabled || !trigger.isTrigger)
                    continue;

                if (!zone.IsPointInsideTrigger(position))
                    continue;

                insideZone = true;
                break;
            }

            if (!insideZone)
                return position;

            var forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            position += forward.normalized * step;
        }

        return position;
    }

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        var root = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform.root
            : other.transform.root;

        if (root.CompareTag("Player"))
            return true;

        var traveling = PlayerScenePersistence.TravelingPlayer;
        return traveling != null && root.gameObject == traveling;
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
