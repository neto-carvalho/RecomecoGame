using Controller;
using UnityEngine;

public static class SceneTransitionPlayerSetup
{
    public static void AfterSceneLoad(GameObject player)
    {
        if (player == null)
            return;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        InteractionUI.HideMessage();

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = true;
            CharacterGroundSnap.FitControllerToWorldScale(cc);
        }

        var mover = player.GetComponent<CharacterMover>();
        if (mover != null)
            mover.enabled = true;

        var input = player.GetComponent<MovePlayerInput>();
        if (input != null)
            input.enabled = true;

        PlayerScenePersistence.RefreshTravelingReferences();
        PlayerScenePersistence.EnsureTravelingCameraActive();
        PlayerScenePersistence.WireInteractionUIAfterLoad();
        PlayerScenePersistence.WireCameraAfterLoad(player);

        if (input != null)
            input.RefreshCameraBinding();

        PlayerAnimatorSetup.RefreshLocomotion(player);
    }
}
