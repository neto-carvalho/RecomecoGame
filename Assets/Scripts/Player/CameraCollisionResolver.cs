using UnityEngine;

namespace Controller
{
    public static class CameraCollisionResolver
    {
        const float MinRadius = 0.06f;
        const float MaxRadius = 0.22f;
        const float MinSkin = 0.05f;
        const float MaxSkin = 0.14f;

        public static Vector3 Resolve(
            Vector3 pivot,
            Vector3 desiredCameraPos,
            Transform player,
            float playerScale,
            LayerMask mask)
        {
            var delta = desiredCameraPos - pivot;
            var distance = delta.magnitude;
            if (distance < 0.001f)
                return desiredCameraPos;

            var direction = delta / distance;
            var scale = Mathf.Max(0.15f, playerScale);
            var radius = Mathf.Clamp(0.18f * scale, MinRadius, MaxRadius);
            var skin = Mathf.Clamp(0.12f * scale, MinSkin, MaxSkin);
            var minDistance = Mathf.Max(radius * 2.2f, 0.35f * scale);

            var best = distance;

            foreach (var hit in Physics.SphereCastAll(
                         pivot, radius, direction, distance, mask, QueryTriggerInteraction.Ignore))
            {
                if (ShouldIgnore(hit.collider, player))
                    continue;

                if (hit.distance < best)
                    best = hit.distance;
            }

            if (Physics.Linecast(pivot, desiredCameraPos, out var lineHit, mask, QueryTriggerInteraction.Ignore) &&
                !ShouldIgnore(lineHit.collider, player))
            {
                best = Mathf.Min(best, lineHit.distance);
            }

            if (best >= distance)
                return desiredCameraPos;

            return pivot + direction * Mathf.Max(minDistance, best - skin);
        }

        public static Vector3 Resolve(
            Vector3 pivot,
            Vector3 desiredCameraPos,
            Transform player,
            float playerScale)
        {
            return Resolve(pivot, desiredCameraPos, player, playerScale, ~0);
        }

        static bool ShouldIgnore(Collider col, Transform player)
        {
            if (col == null || col.isTrigger)
                return true;

            if (player != null &&
                (col.transform == player || col.transform.IsChildOf(player)))
            {
                return true;
            }

            if (col.CompareTag("Player"))
                return true;

            return false;
        }
    }
}
