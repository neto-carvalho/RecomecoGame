using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CityTrafficVehicle : MonoBehaviour
{
    [SerializeField] float minSpeedKmh = 16f;
    [SerializeField] float maxSpeedKmh = 38f;
    [SerializeField] float steerDegreesPerSecond = 32f;
    [SerializeField] float lookAheadMeters = 5f;
    [SerializeField] float sideProbeMeters = 2.5f;
    [SerializeField] float probeHeight = 1.1f;
    [SerializeField] LayerMask obstacleMask = ~0;

    Rigidbody _rb;
    float _speedMs;
    float _steerDirection = 1f;
    float _nextDecisionTime;
    bool _initialized;

    public void ApplySettings(RecomecoGameplaySettings settings)
    {
        if (settings == null)
            return;

        minSpeedKmh = settings.trafficMinSpeedKmh;
        maxSpeedKmh = settings.trafficMaxSpeedKmh;
    }

    /// <summary>Inicia velocidade sem alterar a rotação colocada na cena.</summary>
    public void PrepareForTraffic()
    {
        VehicleColliderUtility.PrepareTrafficObject(gameObject);
        _rb = GetComponent<Rigidbody>();
        if (!_initialized)
        {
            _speedMs = Random.Range(minSpeedKmh, maxSpeedKmh) / 3.6f;
            _steerDirection = Random.value > 0.5f ? 1f : -1f;
            _nextDecisionTime = Time.time + Random.Range(1.5f, 4f);
            _initialized = true;
        }
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            VehicleColliderUtility.EnsureTrafficRigidbody(gameObject);
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name == RecomecoSceneNames.Cidade)
        {
            enabled = false;
            return;
        }

        PrepareForTraffic();
    }

    void FixedUpdate()
    {
        if (_rb == null)
            return;

        if (Time.time >= _nextDecisionTime)
        {
            _nextDecisionTime = Time.time + Random.Range(2f, 5f);
            if (Random.value < 0.3f)
                _steerDirection *= -1f;
        }

        var origin = transform.position + Vector3.up * probeHeight;
        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        var forwardBlocked = IsBlocked(origin, forward, lookAheadMeters);
        var right = Vector3.Cross(Vector3.up, forward);
        var leftClear = !IsBlocked(origin, -right, sideProbeMeters);
        var rightClear = !IsBlocked(origin, right, sideProbeMeters);

        if (forwardBlocked)
        {
            if (leftClear && !rightClear)
                _steerDirection = -1f;
            else if (rightClear && !leftClear)
                _steerDirection = 1f;
            else if (leftClear && rightClear)
                _steerDirection = Random.value > 0.5f ? 1f : -1f;
            else
            {
                var targetRot = _rb.rotation * Quaternion.Euler(0f, 180f, 0f);
                _rb.MoveRotation(Quaternion.RotateTowards(_rb.rotation, targetRot, 120f * Time.fixedDeltaTime));
                _speedMs = Mathf.Max(_speedMs * 0.6f, minSpeedKmh / 3.6f * 0.25f);
            }

            _speedMs = Mathf.Max(_speedMs * 0.92f, minSpeedKmh / 3.6f * 0.4f);
        }
        else
        {
            var targetSpeed = Random.Range(minSpeedKmh, maxSpeedKmh) / 3.6f;
            _speedMs = Mathf.MoveTowards(_speedMs, targetSpeed, 3f * Time.fixedDeltaTime);
        }

        var yaw = _steerDirection * steerDegreesPerSecond * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, yaw, 0f));

        forward = _rb.rotation * Vector3.forward;
        forward.y = 0f;
        forward.Normalize();
        var delta = forward * (_speedMs * Time.fixedDeltaTime);
        var nextPos = _rb.position + delta;
        nextPos = SnapToGround(nextPos);
        _rb.MovePosition(nextPos);
    }

    Vector3 SnapToGround(Vector3 position)
    {
        var rayOrigin = position + Vector3.up * 3f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, 8f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (IsGroundLike(hit.normal))
                position.y = hit.point.y + 0.05f;
        }

        return position;
    }

    bool IsBlocked(Vector3 origin, Vector3 direction, float distance)
    {
        if (direction.sqrMagnitude < 0.001f)
            return false;

        direction.Normalize();
        var hits = Physics.RaycastAll(origin, direction, distance, obstacleMask, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform.root == transform.root)
                continue;

            if (IsGroundLike(hit.normal))
                continue;

            return true;
        }

        return false;
    }

    static bool IsGroundLike(Vector3 normal)
    {
        return normal.y > 0.55f;
    }
}
