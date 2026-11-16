using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Passeia por destinos aleatórios no NavMesh (calçadas/ruas), respeitando obstáculos e desnível.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public sealed class CityPedestrianNavWalker : MonoBehaviour
{
    [SerializeField] private float wanderRadius = 60f;
    [SerializeField] private float minTimeBeforeNewDestination = 2.5f;
    [SerializeField] private float maxTimeBeforeNewDestination = 9f;
    [SerializeField] private float idleChanceAtDestination = 0.35f;
    [SerializeField] private float idleDurationSeconds = 2f;

    NavMeshAgent _agent;
    SidewalkNpcWalker _walker;
    float _nextPickTime;
    float _idleUntil;

    public void Configure(float radius, float walkSpeed)
    {
        wanderRadius = Mathf.Max(8f, radius);
        if (_agent != null)
            _agent.speed = Mathf.Max(0.1f, walkSpeed);
    }

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _walker = GetComponent<SidewalkNpcWalker>();
    }

    void Start()
    {
        if (_walker != null)
            _walker.EnableNavMeshMode(true);

        ConfigureAgentDimensions();
        ScheduleNextDestinationPick(0.5f, 1.5f);
        TryPickNewDestination();
    }

    void ConfigureAgentDimensions()
    {
        var scale = Mathf.Max(0.15f, transform.lossyScale.y);
        _agent.radius = Mathf.Clamp(0.12f * scale / 0.2f, 0.08f, 0.35f);
        _agent.height = Mathf.Clamp(1.75f * scale / 0.2f, 0.35f, 2f);
        _agent.baseOffset = _agent.height * 0.05f;
        _agent.acceleration = 12f;
        _agent.angularSpeed = 360f;
        _agent.autoRepath = true;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        _agent.stoppingDistance = 0.35f;
        _agent.autoBraking = true;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            _agent.speed = Mathf.Max(0.1f, settings.cityPedestrianWalkSpeed * Mathf.Sqrt(scale));
    }

    void Update()
    {
        if (_walker != null && _walker.InteractionPaused)
            return;

        if (Time.time < _idleUntil)
        {
            _agent.isStopped = true;
            return;
        }

        _agent.isStopped = false;

        if (Time.time < _nextPickTime)
            return;

        if (!_agent.pathPending &&
            (!_agent.hasPath || _agent.remainingDistance <= _agent.stoppingDistance + 0.25f))
            TryPickNewDestination();
    }

    void TryPickNewDestination()
    {
        var origin = transform.position;
        for (var attempt = 0; attempt < 24; attempt++)
        {
            var offset = Random.insideUnitSphere * wanderRadius;
            offset.y = 0f;
            var candidate = origin + offset;

            if (!NavMesh.SamplePosition(candidate, out var hit, wanderRadius * 0.5f, NavMesh.AllAreas))
                continue;

            var path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.Length >= 2)
                {
                    _agent.SetDestination(hit.position);

                    if (Random.value < idleChanceAtDestination)
                    {
                        _idleUntil = Time.time + idleDurationSeconds;
                        _agent.isStopped = true;
                    }

                    ScheduleNextDestinationPick(minTimeBeforeNewDestination, maxTimeBeforeNewDestination);
                    return;
                }
            }
        }

        ScheduleNextDestinationPick(1f, 2f);
    }

    void ScheduleNextDestinationPick(float minDelay, float maxDelay)
    {
        _nextPickTime = Time.time + Random.Range(minDelay, maxDelay);
    }

    public void StopForInteraction()
    {
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }
    }

    public void ResumeAfterInteraction()
    {
        _idleUntil = 0f;
        if (_agent != null)
            _agent.isStopped = false;
        ScheduleNextDestinationPick(0.3f, 0.8f);
    }
}
