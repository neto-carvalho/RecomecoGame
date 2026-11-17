using Controller;
using UnityEngine;

/// <summary>
/// Bloqueio físico (empurrão) e dano quando o tráfego atinge o jogador.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(120)]
public sealed class TrafficVehiclePlayerImpact : MonoBehaviour
{
    [SerializeField] float minSpeedKmhToHurt = 6f;
    [SerializeField] float damagePerHit = 38f;
    [SerializeField] float knockbackForce = 6.5f;
    [SerializeField] float hitCooldownSeconds = 0.85f;
    [SerializeField] float playerSeparationPadding = 0.12f;

    TrafficRouteFollower _follower;
    BoxCollider _box;
    Rigidbody _rb;
    float _nextHitTime;

    void Awake()
    {
        _follower = GetComponent<TrafficRouteFollower>();
        _box = GetComponent<BoxCollider>();
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (_box == null)
            return;

        var center = _box.bounds.center;
        var half = _box.bounds.extents * 0.82f;
        var hits = Physics.OverlapBox(center, half, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        var speedKmh = GetSpeedKmh();

        foreach (var col in hits)
        {
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            var playerRoot = ResolvePlayerRoot(col);
            if (playerRoot == null)
                continue;

            if (PlayerDrivableVehicle.Active != null
                && PlayerDrivableVehicle.Active.transform.root == playerRoot)
                continue;

            PushPlayerOutOfVehicle(playerRoot, col);
            NudgeVehicleFromPlayer(playerRoot);

            if (speedKmh >= minSpeedKmhToHurt && Time.time >= _nextHitTime)
            {
                ApplyHit(playerRoot, speedKmh);
                return;
            }
        }
    }

    void PushPlayerOutOfVehicle(Transform playerRoot, Collider playerCol)
    {
        var cc = playerCol.GetComponent<CharacterController>()
                 ?? playerCol.GetComponentInParent<CharacterController>();
        if (cc == null || !cc.enabled)
            return;

        var playerCenter = cc.transform.position + cc.center;
        var closest = _box.ClosestPoint(playerCenter);

        var offset = playerCenter - closest;
        offset.y = 0f;

        var minDist = cc.radius + playerSeparationPadding;
        if (offset.sqrMagnitude < 0.0004f)
        {
            offset = -transform.forward;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0004f)
                offset = Vector3.right;
        }

        offset.Normalize();
        var penetration = minDist - Vector3.Distance(
            new Vector3(playerCenter.x, 0f, playerCenter.z),
            new Vector3(closest.x, 0f, closest.z));

        if (penetration <= 0f)
            return;

        var push = offset * (penetration + 0.02f);
        cc.Move(push);
    }

    void NudgeVehicleFromPlayer(Transform playerRoot)
    {
        if (_rb == null)
            return;

        var playerPos = playerRoot.position;
        var toPlayer = playerPos - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f)
            return;

        if (Vector3.Dot(transform.forward, toPlayer.normalized) > 0.25f)
        {
            var back = -transform.forward * 0.06f;
            _rb.MovePosition(_rb.position + back);
        }
    }

    float GetSpeedKmh()
    {
        if (_follower != null && _follower.enabled)
            return _follower.CurrentSpeedKmh;

        return transform.forward.sqrMagnitude > 0.01f ? 18f : 0f;
    }

    static Transform ResolvePlayerRoot(Collider col)
    {
        var cc = col.GetComponent<CharacterController>() ?? col.GetComponentInParent<CharacterController>();
        if (cc == null)
            return null;

        if (cc.GetComponent<PlayerNeeds>() == null && cc.GetComponentInParent<PlayerNeeds>() == null)
            return null;

        return cc.transform.root;
    }

    void ApplyHit(Transform playerRoot, float speedKmh)
    {
        _nextHitTime = Time.time + hitCooldownSeconds;

        var settings = RecomecoGameplaySettings.Instance;
        var damage = settings != null ? settings.trafficRunOverDamage : damagePerHit;
        var knock = settings != null ? settings.trafficRunOverKnockback : knockbackForce;
        var speedFactor = Mathf.Clamp(speedKmh / 28f, 0.55f, 1.35f);
        damage *= speedFactor;
        knock *= speedFactor;

        var needs = playerRoot.GetComponent<PlayerNeeds>();
        if (needs != null)
            needs.AddHealth(-damage);

        var flatFwd = transform.forward;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude < 0.01f)
            flatFwd = Vector3.forward;
        else
            flatFwd.Normalize();

        PlayerTrafficKnockback.Apply(playerRoot.gameObject, flatFwd, knock);

        if (needs != null && needs.Health <= 0f)
            PlayerFaintHandler.TryFaint(playerRoot.gameObject);
    }
}

public static class PlayerTrafficKnockback
{
    public static void Apply(GameObject player, Vector3 worldDirection, float strength)
    {
        if (player == null || strength <= 0f)
            return;

        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.01f)
            return;

        worldDirection.Normalize();

        var existing = player.GetComponent<PlayerTrafficKnockbackImpulse>();
        if (existing == null)
            existing = player.AddComponent<PlayerTrafficKnockbackImpulse>();

        existing.Begin(worldDirection, strength);
    }
}

sealed class PlayerTrafficKnockbackImpulse : MonoBehaviour
{
    CharacterController _cc;
    CharacterMover _mover;
    MovePlayerInput _input;
    Vector3 _velocity;
    float _endTime;

    public void Begin(Vector3 direction, float strength)
    {
        _cc = GetComponent<CharacterController>();
        _mover = GetComponent<CharacterMover>();
        _input = GetComponent<MovePlayerInput>();
        _velocity = direction * strength;
        _endTime = Time.time + 0.45f;
        enabled = true;
    }

    void OnEnable()
    {
        if (_mover != null)
            _mover.enabled = false;
        if (_input != null)
            _input.enabled = false;
    }

    void Update()
    {
        if (_cc == null)
        {
            enabled = false;
            return;
        }

        _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, 14f * Time.deltaTime);
        if (_cc.enabled)
            _cc.Move(_velocity * Time.deltaTime);

        if (Time.time >= _endTime && _velocity.sqrMagnitude < 0.04f)
            End();
    }

    void End()
    {
        if (_mover != null)
            _mover.enabled = true;
        if (_input != null)
            _input.enabled = true;
        enabled = false;
        Destroy(this);
    }

    void OnDisable()
    {
        if (_mover != null && !PlayerFaintSequence.IsPlaying)
            _mover.enabled = true;
        if (_input != null && !PlayerFaintSequence.IsPlaying)
            _input.enabled = true;
    }
}