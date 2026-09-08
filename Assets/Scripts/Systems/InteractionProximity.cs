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
        if (player == null)
            return false;

        var range = GetScaledRange(baseRange, player);
        var probe = GetPlayerProbe(player);
        return Vector3.Distance(probe, targetPosition) <= range;
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
