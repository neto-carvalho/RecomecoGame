using Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "RecomecoGameplaySettings", menuName = "Recomeco/Gameplay Settings")]
public class RecomecoGameplaySettings : ScriptableObject
{
    public RuntimeAnimatorController movementController;
    public Avatar playerAvatar;

    [Header("Aparência padrão do jogador")]
    public Mesh animBodyMesh;
    public Mesh faceMesh;
    public Mesh hairMesh;
    public Mesh outwearMesh;
    public Mesh pantsMesh;

    [Tooltip("Escala Y do player para a qual walk/run/jump abaixo foram calibrados (ex.: 0.2 no pack ithappy)")]
    public float referencePlayerScale = 0.2f;

    [Tooltip("Velocidades do CharacterMover (km/h no Inspector do CharacterMover; aqui em unidades do pack)")]
    public float walkSpeed = 3f;
    public float runSpeed = 8f;
    public float rotateSpeed = 200f;

    [Tooltip("Altura do pulo em metros (CharacterMover.m_JumpHeight) na referencePlayerScale")]
    public float jumpHeight = 2.5f;

    [Header("Áudio")]
    [Tooltip("Biblioteca de sons de passos por superfície (Assets/Audio/FootstepSurfaceLibrary.asset)")]
    public FootstepSurfaceLibrary footstepLibrary;

    [Header("Coleta")]
    [Tooltip("Prefab da latinha coletável (Assets/Latinha.prefab)")]
    public GameObject latinhaPrefab;

    [Header("Menu")]
    [Tooltip("Vídeo intro após escolher Ferro Velho ou Cidade (arraste Assets/Resources/Video/recomeco_intro.mp4)")]
    public VideoClip introVideoClip;

    [Tooltip("Dinheiro inicial em centavos ao começar partida (420 = R$ 4,20)")]
    public int initialMoneyCents = 420;

    [Header("Escala do player por cena (portal / DontDestroyOnLoad)")]
    [Tooltip("Escala uniforme (X=Y=Z) na cidade. Use a mesma da Hierarchy do Player na cena Cidade.")]
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

    public Mesh GetAppearanceMesh(PlayerAppearanceSetup.AppearanceSlot slot)
    {
        return slot switch
        {
            PlayerAppearanceSetup.AppearanceSlot.Body => animBodyMesh,
            PlayerAppearanceSetup.AppearanceSlot.Face => faceMesh,
            PlayerAppearanceSetup.AppearanceSlot.Hair => hairMesh,
            PlayerAppearanceSetup.AppearanceSlot.Outwear => outwearMesh,
            PlayerAppearanceSetup.AppearanceSlot.Pants => pantsMesh,
            _ => null,
        };
    }
}
