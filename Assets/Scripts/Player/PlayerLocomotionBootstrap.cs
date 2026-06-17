using Controller;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PlayerLocomotionBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (!CompareTag("Player"))
            return;

        var legacy = GetComponent<PlayerMovement>();
        if (legacy != null)
        {
            legacy.enabled = false;
            Debug.LogWarning(
                "PlayerLocomotionBootstrap: PlayerMovement desativado — use CharacterMover + MovePlayerInput.");
        }

        var mover = GetComponent<CharacterMover>();
        if (mover == null)
            mover = gameObject.AddComponent<CharacterMover>();

        var input = GetComponent<MovePlayerInput>();
        if (input == null)
            input = gameObject.AddComponent<MovePlayerInput>();

        mover.enabled = true;
        input.enabled = true;
    }

    void Start()
    {
        if (!CompareTag("Player"))
            return;

        GameplaySceneRuntimeSetup.Run();
        RefreshLocomotion();
    }

    public void RefreshLocomotion()
    {
        if (!CompareTag("Player"))
            return;

        PlayerAnimatorSetup.RefreshLocomotion(gameObject);
    }
}
