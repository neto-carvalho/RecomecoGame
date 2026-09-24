using System.Collections.Generic;
using Controller;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDrivableVehicle : MonoBehaviour, IInteractionPromptOwner
{
    [SerializeField] Transform seatPoint;
    [SerializeField] float enterDistance = 3.5f;
    [SerializeField] float maxSpeedKmh = 52f;
    [SerializeField] float acceleration = 14f;
    [SerializeField] float brakeForce = 22f;
    [SerializeField] float steerDegreesPerSecond = 95f;
    [SerializeField] KeyCode enterKey = KeyCode.E;

    Rigidbody _rb;
    GameObject _occupant;
    MovePlayerInput _occupantInput;
    CharacterMover _occupantMover;
    CharacterController _occupantController;
    readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
    bool _playerInRange;
    float _currentSpeedMs;

    public bool IsOccupied => _occupant != null;
    public static PlayerDrivableVehicle Active { get; private set; }

    public bool IsInteractionPromptActive() => _playerInRange && isActiveAndEnabled && !IsOccupied && !ShopUI.IsOpen;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            VehicleColliderUtility.EnsureDrivingRigidbody(gameObject);

        _rb = GetComponent<Rigidbody>();
        VehicleColliderUtility.EnsureRootBoxCollider(gameObject);

        if (seatPoint == null)
        {
            var seatGo = new GameObject("SeatPoint");
            seatGo.transform.SetParent(transform, false);
            seatGo.transform.localPosition = new Vector3(-0.35f, 0.55f, 0.15f);
            seatPoint = seatGo.transform;
        }
    }

    void OnDisable()
    {
        SetInRange(false);
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
        if (Active == this)
            Active = null;
    }

    void LateUpdate()
    {
        if (IsOccupied)
            return;

        if (ShopUI.IsOpen)
            return;

        var player = InteractionProximity.GetPlayer();
        var inRange = player != null &&
                      InteractionProximity.IsWithinHorizontalRange(transform.position, enterDistance, player.transform, true);

        if (inRange != _playerInRange)
            SetInRange(inRange);

        if (!_playerInRange || player == null)
            return;

        if (Input.GetKeyDown(enterKey))
        {
            InteractionUI.HideMessage(this);
            TryEnter(player);
            return;
        }

        InteractionUI.ShowMessage("Entrar no veículo — " + enterKey, this);
    }

    void FixedUpdate()
    {
        if (!IsOccupied || _rb == null)
            return;

        var vertical = Input.GetAxisRaw("Vertical");
        var horizontal = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(vertical) > 0.01f)
            _currentSpeedMs = Mathf.MoveTowards(_currentSpeedMs, vertical * (maxSpeedKmh / 3.6f), acceleration * Time.fixedDeltaTime);
        else
            _currentSpeedMs = Mathf.MoveTowards(_currentSpeedMs, 0f, brakeForce * Time.fixedDeltaTime);

        if (Mathf.Abs(_currentSpeedMs) > 0.05f)
        {
            var steer = horizontal * steerDegreesPerSecond * Time.fixedDeltaTime * Mathf.Sign(Mathf.Max(0.15f, Mathf.Abs(_currentSpeedMs)));
            transform.Rotate(0f, steer, 0f, Space.World);
        }

        _rb.MovePosition(_rb.position + transform.forward * (_currentSpeedMs * Time.fixedDeltaTime));

        if (Input.GetKeyDown(KeyCode.E) && Mathf.Abs(_currentSpeedMs) < 1.2f)
            ExitVehicle();
    }

    public bool TryEnter(GameObject player)
    {
        if (player == null || IsOccupied)
            return false;

        _occupant = player;
        _occupantInput = player.GetComponent<MovePlayerInput>();
        _occupantMover = player.GetComponent<CharacterMover>();
        _occupantController = player.GetComponent<CharacterController>();

        if (_occupantInput != null)
            _occupantInput.enabled = false;
        if (_occupantMover != null)
            _occupantMover.enabled = false;
        if (_occupantController != null)
            _occupantController.enabled = false;

        HideOccupantVisuals(player);
        _occupant.transform.SetParent(seatPoint, false);
        _occupant.transform.localPosition = Vector3.zero;
        _occupant.transform.localRotation = Quaternion.identity;

        var camera = Object.FindFirstObjectByType<PlayerCamera>();
        if (camera != null)
            camera.SetPlayer(transform);

        Active = this;
        _currentSpeedMs = 0f;
        return true;
    }

    public void ExitVehicle()
    {
        if (_occupant == null)
            return;

        var exitPos = transform.position + transform.right * 1.2f + Vector3.up * 0.2f;
        _occupant.transform.SetParent(null, true);
        _occupant.transform.position = exitPos;

        RestoreOccupantVisuals();

        if (_occupantController != null)
            _occupantController.enabled = true;
        if (_occupantMover != null)
            _occupantMover.enabled = true;
        if (_occupantInput != null)
            _occupantInput.enabled = true;

        var camera = Object.FindFirstObjectByType<PlayerCamera>();
        if (camera != null)
            camera.SetPlayer(_occupant.transform);

        _occupant = null;
        _occupantInput = null;
        _occupantMover = null;
        _occupantController = null;
        Active = null;
        _currentSpeedMs = 0f;
    }

    void HideOccupantVisuals(GameObject player)
    {
        _hiddenRenderers.Clear();
        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;

            _hiddenRenderers.Add(renderer);
            renderer.enabled = false;
        }
    }

    void RestoreOccupantVisuals()
    {
        foreach (var renderer in _hiddenRenderers)
        {
            if (renderer != null)
                renderer.enabled = true;
        }

        _hiddenRenderers.Clear();
    }

    void SetInRange(bool inRange)
    {
        _playerInRange = inRange;
        if (!inRange)
            InteractionUI.HideMessage(this);
    }
}
