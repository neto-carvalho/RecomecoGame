using UnityEngine;

/// <summary>
/// Segue o centro da polilinha da rota (não corta quinas); volante alinhado à tangente com desaceleração em curva.
/// </summary>
[DisallowMultipleComponent]
public class TrafficRouteFollower : MonoBehaviour
{
    [SerializeField] float followProbeMeters = 14f;
    [SerializeField] LayerMask obstacleMask = ~0;

    TrafficRoute _route;
    int _waypointIndex;
    int _stepDirection = 1;
    float _alongOnSegmentMeters;
    float _cruiseSpeedMs;
    float _currentSpeedMs;
    float _stuckLowSpeedSeconds;
    float _steerYawVelocity;
    Rigidbody _rb;
    RecomecoGameplaySettings _settings;

    public bool IsDriving => enabled && _route != null && _route.WaypointCount >= 2;

    public TrafficRoute Route => _route;

    public float CurrentSpeedKmh => _currentSpeedMs * 3.6f;

    void Awake()
    {
        VehicleColliderUtility.PrepareTrafficObject(gameObject);
        _rb = GetComponent<Rigidbody>();
        if (GetComponent<TrafficVehiclePlayerImpact>() == null)
            gameObject.AddComponent<TrafficVehiclePlayerImpact>();
    }

    public void AssignRoute(TrafficRoute route, int startWaypointIndex = 0, bool snapOntoRoute = true)
    {
        BeginRoute(route, startWaypointIndex, snapOntoRoute ? Random.Range(0.15f, 0.85f) : -1f);
    }

    public void BeginRoute(TrafficRoute route, int startWaypointIndex, float segmentAlong01)
    {
        _route = route;
        _settings = RecomecoGameplaySettings.Instance;
        if (_route != null)
            _route.EnsureReady();

        _waypointIndex = 0;
        if (_route != null && _route.WaypointCount > 0)
            _waypointIndex = Mathf.Clamp(startWaypointIndex, 0, _route.WaypointCount - 1);

        _stepDirection = 1;
        _alongOnSegmentMeters = 0f;
        _cruiseSpeedMs = ResolveCruiseSpeedMs();
        _currentSpeedMs = _cruiseSpeedMs * Random.Range(0.65f, 0.85f);

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
        _waypointIndex = waypointIndex;
        var seg = GetSegmentVectors(out var len);
        if (len < 0.05f)
        {
            ApplyPose(_route.GetWorldPoint(waypointIndex), transform.rotation);
            _alongOnSegmentMeters = 0f;
            return;
        }

        _alongOnSegmentMeters = len * Mathf.Clamp01(alongSegment01);
        var pos = seg.start + seg.dir * _alongOnSegmentMeters;
        pos.y = VehicleGroundSnap.SnapLowestRoadY(pos.x, pos.z, seg.start.y);
        var rot = Quaternion.LookRotation(seg.dir, Vector3.up);
        ApplyPose(pos, rot);
        VehicleGroundSnap.SnapTrafficWheelsToGround(transform);
    }

    void FixedUpdate()
    {
        if (!IsDriving)
            return;

        Step(Time.fixedDeltaTime);
        if (_rb != null)
        {
            _rb.MovePosition(transform.position);
            _rb.MoveRotation(transform.rotation);
        }
    }

    void Step(float deltaTime)
    {
        if (!TryGetDriveSegment(out var seg, out var segLen))
            return;

        var travelDir = seg.dir;
        var pathTurn = UpcomingCornerAngleDegrees();
        var targetSpeed = ComputeTargetSpeed(pathTurn, travelDir);
        TickStuckRecovery(deltaTime, ref targetSpeed);

        var accel = targetSpeed > _currentSpeedMs ? AccelRate() : BrakeRate();
        _currentSpeedMs = Mathf.MoveTowards(_currentSpeedMs, targetSpeed, accel * deltaTime);

        var move = _currentSpeedMs * deltaTime;
        if (move > 0.0001f)
            AdvanceAlongRoute(move);

        if (!TryGetDriveSegment(out seg, out segLen))
            return;

        var travelAlong = Mathf.Clamp(_alongOnSegmentMeters, 0f, segLen);
        Vector3 pos;
        Vector3 faceDir;
        if (UseSplineMotion()
            && TrafficRouteSpline.TryEvaluate(_route, _waypointIndex, _stepDirection, travelAlong, segLen, out pos, out faceDir))
        {
            pos.y = VehicleGroundSnap.SnapLowestRoadY(pos.x, pos.z, seg.start.y);
        }
        else
        {
            pos = seg.start + seg.dir * travelAlong;
            pos.y = VehicleGroundSnap.SnapLowestRoadY(pos.x, pos.z, seg.start.y);
            faceDir = seg.dir;
        }

        transform.position = pos;
        VehicleGroundSnap.SnapTrafficWheelsToGround(transform);
        ApplyFacing(faceDir, seg.dir, deltaTime);
    }

    bool UseSplineMotion() => _settings == null || _settings.trafficUseSplineMotion;

    bool TryGetDriveSegment(out (Vector3 start, Vector3 dir) seg, out float segLen)
    {
        seg = default;
        segLen = 0f;
        for (var guard = 0; guard < _route.WaypointCount + 2; guard++)
        {
            seg = GetSegmentVectors(out segLen);
            if (segLen >= 0.05f)
                return true;

            var end = ResolveDestinationIndex(_waypointIndex);
            if (end == _waypointIndex)
                return false;

            _waypointIndex = end;
            _alongOnSegmentMeters = 0f;
        }

        return false;
    }

    void TickStuckRecovery(float deltaTime, ref float targetSpeed)
    {
        if (_currentSpeedMs > 0.25f && targetSpeed > 0.25f)
        {
            _stuckLowSpeedSeconds = 0f;
            return;
        }

        _stuckLowSpeedSeconds += deltaTime;
        if (_stuckLowSpeedSeconds < 1.6f)
            return;

        targetSpeed = Mathf.Max(targetSpeed, _cruiseSpeedMs * 0.42f);
        if (_currentSpeedMs < 0.15f)
            _currentSpeedMs = _cruiseSpeedMs * 0.25f;

        _stuckLowSpeedSeconds = 0.45f;
    }

    void ApplyFacing(Vector3 desiredFacing, Vector3 moveDir, float deltaTime)
    {
        desiredFacing = Flatten(desiredFacing);
        moveDir = Flatten(moveDir);
        if (desiredFacing.sqrMagnitude < 0.0001f)
            desiredFacing = moveDir;
        if (moveDir.sqrMagnitude < 0.0001f)
            moveDir = desiredFacing;

        desiredFacing.Normalize();
        moveDir.Normalize();

        if (Vector3.Dot(moveDir, desiredFacing) < 0f)
            desiredFacing = moveDir;

        var targetYaw = Mathf.Atan2(desiredFacing.x, desiredFacing.z) * Mathf.Rad2Deg;
        var smooth = _settings != null ? _settings.trafficSteerSmoothSeconds : 0.18f;
        if (smooth <= 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(desiredFacing, Vector3.up);
            return;
        }

        var currentYaw = transform.eulerAngles.y;
        var newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _steerYawVelocity, smooth, 120f, deltaTime);
        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
    }

    float ResolveCruiseSpeedMs()
    {
        if (_route == null)
            return 5f;

        var kmh = _route.speedKmh;
        if (_settings != null)
            kmh = Mathf.Min(kmh, _settings.trafficMaxSpeedKmh);

        return Mathf.Max(3f, kmh / 3.6f);
    }

    void AdvanceAlongRoute(float distanceMeters)
    {
        var guard = 0;
        while (distanceMeters > 0.0001f && guard++ < 32)
        {
            var seg = GetSegmentVectors(out var len);
            if (len < 0.05f)
            {
                var endOnly = ResolveDestinationIndex(_waypointIndex);
                _waypointIndex = endOnly;
                _alongOnSegmentMeters = 0f;
                if (!_route.loop && ResolveDestinationIndex(_waypointIndex) == _waypointIndex)
                    break;
                continue;
            }

            var left = len - _alongOnSegmentMeters;
            if (distanceMeters <= left)
            {
                _alongOnSegmentMeters += distanceMeters;
                break;
            }

            distanceMeters -= left;
            var end = ResolveDestinationIndex(_waypointIndex);
            _waypointIndex = end;
            _alongOnSegmentMeters = 0f;

            if (!_route.loop && ResolveDestinationIndex(_waypointIndex) == _waypointIndex)
                break;
        }
    }

    (Vector3 start, Vector3 dir) GetSegmentVectors(out float length)
    {
        length = 0f;
        var end = ResolveDestinationIndex(_waypointIndex);
        var a = Flatten(_route.GetWorldPoint(_waypointIndex));
        var b = Flatten(_route.GetWorldPoint(end));
        var delta = b - a;
        length = delta.magnitude;
        if (length < 0.0001f)
            return (a, Vector3.forward);

        return (a, delta / length);
    }

    float UpcomingCornerAngleDegrees()
    {
        var end = ResolveDestinationIndex(_waypointIndex);
        var next = ResolveDestinationIndex(end);
        if (next == end)
            return 0f;

        var a = Flatten(_route.GetWorldPoint(_waypointIndex));
        var b = Flatten(_route.GetWorldPoint(end));
        var c = Flatten(_route.GetWorldPoint(next));

        var v1 = b - a;
        var v2 = c - b;
        if (v1.sqrMagnitude < 0.04f || v2.sqrMagnitude < 0.04f)
            return 0f;

        return Vector3.Angle(v1.normalized, v2.normalized);
    }

    float ComputeTargetSpeed(float pathTurnDegrees, Vector3 tangent)
    {
        var speed = _cruiseSpeedMs;

        if (pathTurnDegrees > 6f)
        {
            var t = Mathf.Clamp01((pathTurnDegrees - 6f) / 48f);
            speed *= Mathf.Lerp(1f, 0.28f, t);
        }

        speed = ApplyCarFollowSpeed(speed, tangent);

        return Mathf.Max(_cruiseSpeedMs * 0.22f, speed);
    }

    float ApplyCarFollowSpeed(float desired, Vector3 forward)
    {
        var gap = _settings != null ? _settings.trafficFollowGapMeters : 5.5f;
        var origin = transform.position + Vector3.up * 0.65f;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            return desired;

        forward.Normalize();
        if (!Physics.Raycast(origin, forward, out var hit, followProbeMeters, obstacleMask, QueryTriggerInteraction.Ignore))
            return desired;

        if (hit.collider != null && hit.collider.transform.root == transform.root)
            return desired;

        if (!IsOtherTraffic(hit.collider) && !IsPlayerObstacle(hit.collider))
            return desired;

        if (hit.distance <= gap * 0.55f)
            return 0f;

        if (hit.distance < gap + 2f)
            return Mathf.Min(desired, _cruiseSpeedMs * 0.35f);

        return Mathf.Min(desired, _cruiseSpeedMs * Mathf.Clamp01((hit.distance - gap) / gap));
    }

    static bool IsOtherTraffic(Collider col)
    {
        if (col == null)
            return false;

        var root = col.transform.root;
        return root.GetComponent<TrafficRouteFollower>() != null
               || root.GetComponent<CityTrafficVehicle>() != null;
    }

    static bool IsPlayerObstacle(Collider col)
    {
        if (col == null)
            return false;

        var cc = col.GetComponent<CharacterController>() ?? col.GetComponentInParent<CharacterController>();
        if (cc == null)
            return false;

        return cc.GetComponent<PlayerNeeds>() != null || cc.GetComponentInParent<PlayerNeeds>() != null;
    }

    float AccelRate() => _settings != null ? _settings.trafficAcceleration : 4.2f;

    float BrakeRate() => _settings != null ? _settings.trafficBraking : 7.5f;

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
