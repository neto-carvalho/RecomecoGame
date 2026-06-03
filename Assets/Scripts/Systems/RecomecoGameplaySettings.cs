using Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Referências partilhadas para montar gameplay em qualquer cena (Resources/RecomecoGameplaySettings).
/// </summary>
[CreateAssetMenu(fileName = "RecomecoGameplaySettings", menuName = "Recomeco/Gameplay Settings")]
public class RecomecoGameplaySettings : ScriptableObject
{
    public RuntimeAnimatorController movementController;
    public Avatar playerAvatar;

    [Tooltip("Escala Y do player para a qual walk/run/jump abaixo foram calibrados (ex.: 0.2 no pack ithappy)")]
    public float referencePlayerScale = 0.2f;

    [Tooltip("Velocidades do CharacterMover (km/h no Inspector do CharacterMover; aqui em unidades do pack)")]
    public float walkSpeed = 5f;
    public float runSpeed = 15f;
    public float rotateSpeed = 200f;

    [Tooltip("Altura do pulo em metros (CharacterMover.m_JumpHeight) na referencePlayerScale")]
    public float jumpHeight = 5f;

    [Header("Escala do player por cena (portal / DontDestroyOnLoad)")]
    [Tooltip("Escala uniforme (X=Y=Z) na cidade. Use a mesma da Hierarchy do Player na Demo.")]
    public float playerScaleCity = 0.2f;

    [Tooltip("Escala no ferro velho. Igual à cidade = prefira encolher o ambiente junkyard no editor.")]
    public float playerScaleFerroVelho = 0.2f;

    public float GetPlayerScaleForScene(Scene scene)
    {
        return FerroVelhoWalkableGround.IsFerroVelhoScene(scene) ? playerScaleFerroVelho : playerScaleCity;
    }

    public float GetPlayerScaleForActiveScene()
    {
        return GetPlayerScaleForScene(SceneManager.GetActiveScene());
    }

    public void ApplyPlayerScaleForScene(GameObject player, Scene scene)
    {
        if (player == null || !scene.IsValid())
            return;

        var uniform = Mathf.Max(0.01f, GetPlayerScaleForScene(scene));
        player.transform.localScale = new Vector3(uniform, uniform, uniform);

        var snap = player.GetComponent<CharacterGroundSnap>();
        if (snap != null)
            snap.SnapNow();

        var mover = player.GetComponent<CharacterMover>();
        if (mover != null)
            ApplyToMover(mover, player.transform);
    }

    public float GetScaleFactor(Transform player)
    {
        if (player == null)
            return 1f;
        var scale = Mathf.Max(0.01f, Mathf.Abs(player.lossyScale.y));
        var reference = Mathf.Max(0.01f, referencePlayerScale);
        return scale / reference;
    }

    public void ApplyToMover(CharacterMover mover, Transform player)
    {
        if (mover == null)
            return;

        var factor = GetScaleFactor(player);
        mover.SetLocomotionSpeeds(walkSpeed * factor, runSpeed * factor, rotateSpeed);
        mover.SetJumpHeight(jumpHeight * factor);
    }

    static RecomecoGameplaySettings _cached;

    public static RecomecoGameplaySettings Instance
    {
        get
        {
            if (_cached != null)
                return _cached;
            _cached = Resources.Load<RecomecoGameplaySettings>("RecomecoGameplaySettings");
            return _cached;
        }
    }
}
