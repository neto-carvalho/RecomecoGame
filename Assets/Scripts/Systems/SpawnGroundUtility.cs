using UnityEngine;

public static class SpawnGroundUtility
{
    public static Vector3 GetGroundPosition(Vector3 near, float rayUp = 80f, float rayDown = 250f)
    {
        FerroVelhoWalkableGround.EnsureInActiveScene();

        var origin = near + Vector3.up * rayUp;
        if (Physics.Raycast(origin, Vector3.down, out var hit, rayUp + rayDown, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

        if (FerroVelhoWalkableGround.IsFerroVelhoActive() && !FerroVelhoSceneGround.HasWalkableSceneGround())
        {
            near.x = Mathf.Clamp(near.x, FerroVelhoWalkableGround.Center.x - FerroVelhoWalkableGround.Size.x * 0.45f,
                FerroVelhoWalkableGround.Center.x + FerroVelhoWalkableGround.Size.x * 0.45f);
            near.z = Mathf.Clamp(near.z, FerroVelhoWalkableGround.Center.z - FerroVelhoWalkableGround.Size.z * 0.45f,
                FerroVelhoWalkableGround.Center.z + FerroVelhoWalkableGround.Size.z * 0.45f);
            near.y = FerroVelhoWalkableGround.SurfaceY;
        }

        return near;
    }

    public static void PlacePlayerOnGround(GameObject player, Vector3 position)
    {
        if (player == null)
            return;

        FerroVelhoWalkableGround.EnsureInActiveScene();

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            CharacterGroundSnap.FitControllerToWorldScale(cc);

        var grounded = GetGroundPosition(position);

        if (cc != null)
            cc.enabled = false;

        player.transform.position = grounded;

        if (cc != null)
            cc.enabled = true;

        var snap = player.GetComponent<CharacterGroundSnap>();
        if (snap != null)
            snap.SnapNow();
        else
            CharacterGroundSnap.TrySnap(player.transform, cc);

        if (FerroVelhoWalkableGround.IsFerroVelhoActive() && !FerroVelhoSceneGround.HasWalkableSceneGround())
        {
            var target = new Vector3(
                grounded.x,
                Mathf.Max(grounded.y, FerroVelhoWalkableGround.SurfaceY),
                grounded.z);
            var scale = Mathf.Max(0.15f, Mathf.Abs(player.transform.lossyScale.y));
            target.y = Mathf.Max(target.y, FerroVelhoWalkableGround.SurfaceY) + 0.2f * scale;
            if (cc != null)
                target.y += cc.height * 0.5f;
            player.transform.position = target;
            if (snap != null)
                snap.SnapNow();
            else
                CharacterGroundSnap.TrySnap(player.transform, cc);
        }
    }
}
