using Controller;
using UnityEngine;

/// <summary>
/// Dano e empurrão quando o tráfego atinge o jogador (estilo atropelamento leve).
/// </summary>
[DisallowMultipleComponent]
public sealed class TrafficVehiclePlayerImpact : MonoBehaviour
{
    [SerializeField] float minSpeedKmhToHurt = 6f;
    [SerializeField] float damagePerHit = 38f;
    [SerializeField] float knockbackForce = 6.5f;
    [SerializeField] float hitCooldownSeconds = 0.85f;

    TrafficRouteFollower _follower;
    BoxCollider _box;
    float _nextHitTime;

    void Awake()
    {
        _follower = GetComponent<TrafficRouteFollower>();
        _box = GetComponent<BoxCollider>();
    }

    void FixedUpdate()
    {
        if (_box == null)
            return;

        var speedKmh = GetSpeedKmh();
        if (speedKmh < minSpeedKmhToHurt)
            return;

        if (Time.time < _nextHitTime)
            return;

        var center = _box.bounds.center;
        var half = _box.bounds.extents * 0.82f;
        var hits = Physics.OverlapBox(center, half, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
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

            ApplyHit(playerRoot, speedKmh);
            return;
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
