using Controller;
using UnityEngine;

/// <summary>Skate: só avança; direção com o mouse (yaw da câmera).</summary>
public static class PlayerSkateSteering
{
    public static bool IsRiding(Transform player)
    {
        if (player == null)
            return false;

        var skate = player.GetComponent<PlayerSkateController>();
        return skate != null && skate.IsRiding;
    }

    public static void ApplyBeforeMover(ref Vector2 axis, Transform player, PlayerCamera camera)
    {
        if (!IsRiding(player))
            return;

        if (camera != null)
        {
            var yaw = camera.HorizontalYaw;
            player.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        var forward = Mathf.Max(0f, axis.y);
        axis = new Vector2(0f, forward);
    }
}