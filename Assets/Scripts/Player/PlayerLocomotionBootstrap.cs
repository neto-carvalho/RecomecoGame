using Controller;
using UnityEngine;

/// <summary>
/// Garante locomoção ithappy no Player (MovePlayerInput + CharacterMover, sem PlayerMovement antigo).
/// </summary>
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

    /// <summary>Chamado após portal / DontDestroyOnLoad (Start não repete em cenas novas).</summary>
    public void RefreshLocomotion()
    {
        if (!CompareTag("Player"))
            return;

        PlayerAnimatorSetup.RefreshLocomotion(gameObject);
    }
}
