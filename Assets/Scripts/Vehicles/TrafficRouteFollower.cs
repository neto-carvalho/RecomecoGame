using UnityEngine;

[DisallowMultipleComponent]
public class TrafficRouteFollower : MonoBehaviour
{
    [SerializeField] float arriveDistance = 0.25f;
    [SerializeField] float rotateSpeed = 140f;
    [Tooltip("Curva ≥ este ângulo: para no waypoint e só segue depois de alinhar.")]
    [SerializeField] float sharpCornerAngle = 38f;
    [Tooltip("Se o carro sair da linha, puxa de volta para o trecho atual (m).")]
    [SerializeField] float maxLateralOffset = 0.45f;

    TrafficRoute _route;
    int _waypointIndex;
    int _stepDirection = 1;
    float _speedMs;
    Rigidbody _rb;
    bool _aligningAtCorner;
    Vector3 _alignForward;

    public bool IsDriving => enabled && _route != null && _route.WaypointCount >= 2;

    public TrafficRoute Route => _route;

    void Awake()
    {
        VehicleColliderUtility.PrepareTrafficObject(gameObject);
        _rb = GetComponent<Rigidbody>();
    }

    public void AssignRoute(TrafficRoute route, int startWaypointIndex = 0, bool snapOntoRoute = true)
    {
        BeginRoute(route, startWaypointIndex, snapOntoRoute ? Random.Range(0.15f, 0.85f) : -1f);
    }

    /// <param name="segmentAlong01">0–1 entre waypoint e o próximo; &lt; 0 não reposiciona.</param>
    public void BeginRoute(TrafficRoute route, int startWaypointIndex, float segmentAlong01)
    {
        _route = route;
        if (_route != null)
            _route.EnsureReady();

        _waypointIndex = 0;
        if (_route != null && _route.WaypointCount > 0)
            _waypointIndex = Mathf.Clamp(startWaypointIndex, 0, _route.WaypointCount - 1);

        _stepDirection = 1;
        _aligningAtCorner = false;
        _speedMs = _route != null ? Mathf.Max(5f, _route.speedKmh / 3.6f) : 8f;

        foreach (var wander in GetComponentsInChildren<CityTrafficVehicle>(true))
            wander.enabled = false;

        enabled = _route != null && _route.WaypointCount >= 2;
        if (!enabled)
        {
            Debug.LogWarning("[Tráfego] " + name + " sem rota válida (mínimo 2 waypoints).", this);
            return;
        }

        if (segmentAlong01 >= 0f)
            PlaceOnRouteSegment(_waypointIndex, Mathf.Clamp01(segmentAlong01));
    }

    void PlaceOnRouteSegment(int waypointIndex, float alongSegment01)
    {
        if (_route == null)
            return;

        var a = _route.GetWorldPoint(waypointIndex);
        var b = _route.GetWorldPoint(waypointIndex + 1);
        var dir = b - a;
        dir.y = 0f;
        var len = dir.magnitude;
        if (len < 0.05f)
        {
            ApplyPose(a, transform.rotation);
            return;
        }

        dir /= len;
        var pos = a + dir * (len * Mathf.Clamp01(alongSegment01));
        pos.y = VehicleGroundSnap.SnapYNearReference(pos.x, pos.z, a.y);
        var rot = Quaternion.LookRotation(dir, Vector3.up);
        ApplyPose(pos, rot);
    }

    void Update()
    {
        if (!IsDriving)
            return;

        Step(Time.deltaTime);
        if (_rb != null)
        {
            _rb.position = transform.position;
            _rb.rotation = transform.rotation;
        }
    }

    void Step(float deltaTime)
    {
        if (_aligningAtCorner)
        {
            StepCornerAlign(deltaTime);
            return;
        }

        var segEndIndex = ResolveDestinationIndex(_waypointIndex);
        if (segEndIndex == _waypointIndex && !_route.loop)
            return;

        var segStart = Flatten(_route.GetWorldPoint(_waypointIndex));
        var segEnd = Flatten(_route.GetWorldPoint(segEndIndex));
        var seg = segEnd - segStart;
        var segLen = seg.magnitude;
        if (segLen < 0.05f)
        {
            AdvanceWaypoint(segEndIndex);
            return;
        }

        var segDir = seg / segLen;
        var pos = Flatten(transform.position);
        var along = Vector3.Dot(pos - segStart, segDir);
        along = Mathf.Clamp(along, 0f, segLen);

        var lateral = pos - (segStart + segDir * along);
        if (lateral.sqrMagnitude > maxLateralOffset * maxLateralOffset)
            pos = segStart + segDir * along;

        along += _speedMs * deltaTime;

        if (along >= segLen - arriveDistance)
        {
            var endY = _route.GetWorldPoint(segEndIndex).y;
            var endPos = segEnd;
            endPos.y = VehicleGroundSnap.SnapYNearReference(endPos.x, endPos.z, endY);
            var endRot = Quaternion.LookRotation(segDir, Vector3.up);
            ApplyPose(endPos, endRot);
            BeginCornerAlignIfNeeded(segEndIndex, segDir);
            return;
        }

        var nextPos = segStart + segDir * along;
        var refY = _route.GetWorldPoint(_waypointIndex).y;
        nextPos.y = VehicleGroundSnap.SnapYNearReference(nextPos.x, nextPos.z, refY);
        var rot = Quaternion.LookRotation(segDir, Vector3.up);
        transform.SetPositionAndRotation(nextPos, rot);
    }

    void BeginCornerAlignIfNeeded(int cornerWaypointIndex, Vector3 incomingDir)
    {
        AdvanceWaypoint(cornerWaypointIndex);

        var nextEnd = ResolveDestinationIndex(_waypointIndex);
        if (nextEnd == _waypointIndex)
            return;

        var from = Flatten(_route.GetWorldPoint(_waypointIndex));
        var to = Flatten(_route.GetWorldPoint(nextEnd));
        var outgoing = to - from;
        if (outgoing.sqrMagnitude < 0.0025f)
            return;

        outgoing.Normalize();
        if (Vector3.Angle(incomingDir, outgoing) < sharpCornerAngle)
            return;

        _aligningAtCorner = true;
        _alignForward = outgoing;
    }

    void StepCornerAlign(float deltaTime)
    {
        var targetRot = Quaternion.LookRotation(_alignForward, Vector3.up);
        var rot = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * deltaTime);
        transform.rotation = rot;
        if (_rb != null)
            _rb.rotation = rot;

        if (Quaternion.Angle(transform.rotation, targetRot) <= 6f)
            _aligningAtCorner = false;
    }

    void AdvanceWaypoint(int newIndex)
    {
        _waypointIndex = newIndex;
        if (_route.pingPong)
            UpdatePingPongDirection();
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    int ResolveDestinationIndex(int fromIndex)
    {
        if (_route == null || _route.WaypointCount == 0)
            return 0;

        var next = fromIndex + _stepDirection;

        if (_route.pingPong)
        {
            if (next >= _route.WaypointCount)
            {
                _stepDirection = -1;
                next = fromIndex + _stepDirection;
            }
            else if (next < 0)
            {
                _stepDirection = 1;
                next = fromIndex + _stepDirection;
            }

            return Mathf.Clamp(next, 0, _route.WaypointCount - 1);
        }

        if (next >= _route.WaypointCount)
            return _route.loop ? 0 : _route.WaypointCount - 1;

        if (next < 0)
            return _route.loop ? _route.WaypointCount - 1 : 0;

        return next;
    }

    void UpdatePingPongDirection()
    {
        if (_route == null || !_route.pingPong || _route.WaypointCount < 2)
            return;

        if (_waypointIndex >= _route.WaypointCount - 1)
            _stepDirection = -1;
        else if (_waypointIndex <= 0)
            _stepDirection = 1;
    }

    void ApplyPose(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
        if (_rb != null)
        {
            _rb.position = pos;
            _rb.rotation = rot;
        }
    }
}
