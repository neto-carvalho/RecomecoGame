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

    [Header("Debug / testes (Editor e Development Build)")]
    [Tooltip("Atalhos F9–F12 e dinheiro de teste ao iniciar partida")]
    public bool enableDebugCheats = true;

    [Tooltip("Se ativo, substitui initialMoneyCents ao começar nova partida")]
    public bool useTestStartingMoney;

    [Tooltip("Dinheiro ao iniciar com useTestStartingMoney (500000 = R$ 5.000)")]
    public int testStartingMoneyCents = 500000;

    [Header("Escala do player por cena (portal / DontDestroyOnLoad)")]
    [Tooltip("Escala uniforme (X=Y=Z) na cidade. Use a mesma da Hierarchy do Player na cena Cidade.")]
    public float playerScaleCity = 0.2f;

    [Tooltip("Escala no ferro velho. Igual à cidade = prefira encolher o ambiente junkyard no editor.")]
    public float playerScaleFerroVelho = 0.2f;

    [Header("Vida / fome")]
    [Tooltip("Fome perdida por minuto parado")]
    public float hungerDrainPerMinute = 1.2f;

    [Tooltip("Multiplicador de fome ao correr")]
    public float hungerDrainRunMultiplier = 1.6f;

    [Tooltip("Vida perdida por segundo com fome zerada")]
    public float healthLossPerSecondWhenStarving = 2f;

    [Tooltip("Vida recuperada por segundo quando fome acima do limiar")]
    public float healthRegenPerSecond = 1.25f;

    [Tooltip("Fome mínima para regenerar vida")]
    public float healthRegenMinHunger = 25f;

    [Header("Reputação")]
    [Tooltip("Reputação perdida ao errar o minigame de venda na calçada")]
    public float sellMissReputationLoss = 10f;

    [Header("Hospital (desmaio)")]
    [Tooltip("Conta hospital em centavos (8500 = R$ 85,00)")]
    public int hospitalBillCents = 8500;

    public float hospitalWakeHealth = 35f;
    public float hospitalWakeHunger = 30f;

    [Tooltip("Clip de queda (Death_Forward do Aminset_Basic). Use Recomeco → Player → Configurar animação de desmaio")]
    public AnimationClip faintAnimationClip;

    [Tooltip("Reserva: controller inteiro do ithappy (evite — é demo em sequência)")]
    public RuntimeAnimatorController faintAnimatorController;

    [Tooltip("Reserva se usar faintAnimatorController")]
    public string faintAnimationState = "Death_Forward";

    [Tooltip("Altura da câmera = altura do corpo × este valor (visão de cima, estilo GTA)")]
    [Range(2f, 6f)]
    public float faintOverheadBodyHeightMultiplier = 3.4f;

    [Tooltip("Inclinação da câmera (graus) — ~88 = quase de cima, estilo GTA wasted")]
    [Range(55f, 90f)]
    public float faintOverheadPitch = 88f;

    [Tooltip("Folga acima do chão ao alinhar o corpo caído (metros na escala do player)")]
    public float faintGroundClearance = 0.12f;

    [Tooltip("Tamanho do título \"Você desmaiou\" na tela")]
    public float faintTitleFontSize = 76f;

    [Tooltip("Segundos com a mensagem após a queda, antes do hospital")]
    public float faintHoldAfterFallSeconds = 1.35f;

    [Tooltip("Fade para preto antes de teleportar ao hospital")]
    public float faintFadeOutSeconds = 0.45f;

    [Header("Veículos (Cidade)")]
    public bool enableCityTraffic = true;

    [Tooltip("Limite de carros com IA de tráfego (filhos de \"Vehicles\").")]
    public int maxTrafficVehicles = 48;

    public float trafficMinSpeedKmh = 16f;
    public float trafficMaxSpeedKmh = 38f;

    [Header("Câmera")]
    [Tooltip("Evita que a câmera atravesse paredes e objetos sólidos em qualquer cena de gameplay.")]
    public bool cameraWallCollision = true;

    [Tooltip("Quão rápido a câmera aproxima do personagem quando encosta em uma parede.")]
    [Range(1f, 6f)]
    public float cameraCollisionPullSpeedMultiplier = 2.5f;

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
