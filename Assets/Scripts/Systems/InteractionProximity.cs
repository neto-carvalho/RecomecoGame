using Controller;
using UnityEngine;

public static class InteractionProximity
{
    public static GameObject GetPlayer()
    {
        var traveling = PlayerScenePersistence.TravelingPlayer;
        if (traveling != null)
            return traveling;

        return GameObject.FindGameObjectWithTag("Player");
    }

    public static float GetScaledRange(float baseRange, Transform player)
    {
        if (player == null)
            return baseRange;

        var scale = Mathf.Max(0.15f, player.lossyScale.y);
        return baseRange * scale;
    }

    public static bool IsWithinRange(Vector3 targetPosition, float baseRange, Transform player)
    {
        return IsWithinRange(targetPosition, baseRange, player, scaleWithPlayer: true);
    }

    public static bool IsWithinRange(
        Vector3 targetPosition,
        float baseRange,
        Transform player,
        bool scaleWithPlayer)
    {
        if (player == null)
            return false;

        var range = scaleWithPlayer ? GetScaledRange(baseRange, player) : baseRange;
        var probe = GetPlayerProbe(player);
        return Vector3.Distance(probe, targetPosition) <= range;
    }

    public static bool IsWithinHorizontalRange(
        Vector3 targetPosition,
        float range,
        Transform player,
        bool scaleWithPlayer = false)
    {
        if (player == null)
            return false;

        var effectiveRange = scaleWithPlayer ? GetScaledRange(range, player) : range;
        var probe = GetPlayerProbe(player);
        var a = new Vector2(probe.x, probe.z);
        var b = new Vector2(targetPosition.x, targetPosition.z);
        return Vector2.Distance(a, b) <= effectiveRange;
    }

    public static bool IsInsideTrigger(Collider trigger, Transform player, float epsilon = 0.04f)
    {
        if (trigger == null || !trigger.enabled || !trigger.isTrigger || player == null)
            return false;

        if (IsPointInside(trigger, player.position, epsilon))
            return true;

        var cc = player.GetComponent<CharacterController>();
        if (cc != null && IsPointInside(trigger, player.TransformPoint(cc.center), epsilon))
            return true;

        return false;
    }

    public static Vector3 GetPlayerProbe(Transform player)
    {
        if (player == null)
            return Vector3.zero;

        var cc = player.GetComponent<CharacterController>();
        return cc != null ? player.TransformPoint(cc.center) : player.position;
    }

    public static bool IsWorldPointInsideTrigger(Collider trigger, Vector3 worldPoint, float epsilon = 0.04f)
    {
        if (trigger == null || !trigger.enabled)
            return false;

        return IsPointInside(trigger, worldPoint, epsilon);
    }

    static bool IsPointInside(Collider trigger, Vector3 worldPoint, float epsilon)
    {
        if (trigger is BoxCollider box)
        {
            var local = trigger.transform.InverseTransformPoint(worldPoint);
            var center = box.center;
            var extents = box.size * 0.5f;
            return local.x >= center.x - extents.x - epsilon &&
                   local.x <= center.x + extents.x + epsilon &&
                   local.y >= center.y - extents.y - epsilon &&
                   local.y <= center.y + extents.y + epsilon &&
                   local.z >= center.z - extents.z - epsilon &&
                   local.z <= center.z + extents.z + epsilon;
        }

        var closest = trigger.ClosestPoint(worldPoint);
        return (closest - worldPoint).sqrMagnitude <= epsilon * epsilon;
    }
}
