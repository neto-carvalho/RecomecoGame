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

    [Header("Economia (centavos)")]
    [Tooltip("Ferro velho: valor por latinha (120 = R$ 1,20). 10 latinhas ≈ R$ 12.")]
    public int junkyardLatinhaPriceCents = 120;

    [Tooltip("Missão revenda: lucro total na calçada antes de ir à lanchonete (3000 = R$ 30).")]
    public int resellGoalCents = 1500;

    [Tooltip("Casa elegante (referência; o preço na porta usa HouseDoorInteract).")]
    public int housePurchasePriceCents = 15000;

    [Header("Skate")]
    [Tooltip("Desbloqueia skate ao concluir a missão de revenda na rua.")]
    public bool skateUnlockOnResellMission = true;

    [Tooltip("Sem este controller + clips, B não monta no skate (só posse/desbloqueio).")]
    public bool requireSkateAnimatorForRiding = true;

    [Tooltip("Animator com blend ride/remar/pulo (depois de importar Mixamo).")]
    public RuntimeAnimatorController skateMovementController;

    [Tooltip("Só permite andar de skate na cena Cidade (desliga no ferro velho).")]
    public bool skateOnlyInCity = true;

    public float skateWalkSpeed = 5.5f;
    public float skateRunSpeed = 12f;
    public KeyCode skateToggleKey = KeyCode.B;

    [Tooltip("Cooldown entre triggers de remada (Push).")]
    public float skatePushCooldownSeconds = 0.85f;

    [Tooltip("Após montar (B), não dispara Push por este tempo (evita impulso da animação).")]
    public float skateMountPushDelaySeconds = 0.45f;

    [Tooltip("Enquanto anda de skate, nova remada a cada N segundos (0 = só ao sair do parado).")]
    public float skateCoastPushIntervalSeconds = 3.5f;

    [Tooltip("Estado do Character_Skate ao interromper remada por pulo.")]
    public string skateJumpStateName = "Jump";

    [Tooltip("Trava XZ do Hips na pose base (Mixamo sem in-place desloca o mesh).")]
    public bool skateLockHipsHorizontal = true;

    [Tooltip("Compensa agachamento da animação de skate (evita afundar no chão).")]
    public float skateHipsVerticalOffset = 0.06f;

    public string skateHorParam = "Hor";
    public string skateVertParam = "Vert";
    public string skateSpeedParam = "Speed";
    public string skateJumpParam = "IsJump";
    public string skatePushTrigger = "Push";

    [Tooltip("Item no inventário (Assets/Items/Skate.asset).")]
    public ItemData skateItem;

    [Tooltip("Prefab do skate nos pés (menu: Recomeco/Player/Criar prefab placeholder).")]
    public GameObject skateVisualPrefab;

    [Tooltip("Ajuste fino após centralizar o mesh nos pés (X/Z = lateral e frente).")]
    public Vector3 skateVisualLocalPosition = new(0f, 0f, 0f);

    public Vector3 skateVisualLocalEuler;

    [Tooltip("Escala local do prefab Sir_bike (~1 com personagem ithappy em escala 1).")]
    public Vector3 skateVisualLocalScale = Vector3.one;

    [Tooltip("Centraliza o prefab 3D (pivot Sir_bike) sob o personagem.")]
    public bool skateVisualAutoCenterBounds = true;

    [Tooltip("Sons de rodas e pulo no skate (Recomeco → Player → Skate — configurar sons).")]
    public SkateSoundLibrary skateSoundLibrary;

    [TextArea(2, 4)]
    public string skateUnlockBanner = "Você ganhou um skate! Quando as animações estiverem prontas, aperte B na cidade.";

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

    [Tooltip("Dica no HUD após errar venda com pedestre (segundos)")]
    public float sellMissTipSeconds = 8f;

    [Header("Venda na rua — proteção do jogador")]
    [Tooltip("Proteção 0%: pedestres recusam sempre. 100%: sempre aceitam. Entre isso, chance linear (50% prot = 50% chance).")]
    public string npcSellRefuseZeroProtectionTip =
        "Ninguém quer negociar com você neste estado. Coma algo (lanchonete FOOD4U) para recuperar proteção e voltar a vender na calçada.";

    [Tooltip("Quando a proteção é baixa mas > 0 e o sorteio falha")]
    public string npcSellRefuseRandomTip = "Esse pedestre não quis negociar agora. Proteção baixa — coma na lanchonete para se sentir melhor.";

    public float npcSellRefuseTipSeconds = 9f;

    [Tooltip("Proteção extra ao consumir refeição da lanchonete (além do ItemData, se protectionRestore = 0)")]
    public float mealProtectionRestore = 18f;

    [Header("Moradia precária (barraca / lugar abandonado)")]
    [Tooltip("Proteção ao iniciar nova partida na Cidade (0–100)")]
    public float newGameProtection = 42f;

    [Tooltip("Com proteção baixa, doença sobe por minuto (só na Cidade, sem casa comprada)")]
    public float illnessGainPerMinuteWhenExposed = 2.5f;

    [Tooltip("Proteção abaixo disto conta como exposto para ganho de doença")]
    public float exposedProtectionThreshold = 45f;

    [Tooltip("Dormir na barraca: fome recuperada")]
    public float precariousSleepHungerRestore = 18f;

    [Tooltip("Dormir na barraca: vida recuperada")]
    public float precariousSleepHealthRestore = 8f;

    [Tooltip("Dormir na barraca: perde proteção")]
    public float precariousSleepProtectionLoss = 12f;

    [Tooltip("Dormir na barraca: ganha doença")]
    public float precariousSleepIllnessGain = 22f;

    [Tooltip("Dormir na cama (casa): fome / vida / proteção / cura doença")]
    public float safeSleepHungerRestore = 35f;
    public float safeSleepHealthRestore = 28f;
    public float safeSleepProtectionGain = 35f;
    public float safeSleepIllnessReduce = 40f;

    [Header("Dia e noite (Cidade)")]
    [Tooltip("Duração de um ciclo completo 24h em minutos reais")]
    public float dayNightCycleMinutes = 8f;

    public float nightStartHour = 20f;
    public float nightEndHour = 6f;

    [Tooltip("Hora ao iniciar nova partida na Cidade (perto da noite para testar descanso)")]
    public float newGameStartHour = 19.5f;

    public float daySunIntensity = 1.05f;
    public float nightSunIntensity = 0.03f;

    [Tooltip("Horas de transição crepuscular antes/depois da noite")]
    public float twilightHours = 2f;

    [Tooltip("Suavização visual extra (maior = mudança mais lenta)")]
    public float dayNightVisualSmoothTime = 1.8f;

    public float sleepFadeOutSeconds = 0.55f;
    public float sleepFadeInSeconds = 0.9f;

    [Header("Vídeo — dormir na barraca")]
    public VideoClip precariousSleepVideoClip;

    [Tooltip("Resources/Video/carneirinhos_pulando se vazio")]
    public float sleepVideoFadeInSeconds = 0.45f;
    public float sleepVideoFadeOutSeconds = 0.5f;
    public float sleepVideoVolume = 0.85f;
    public bool allowSkipSleepVideo = true;

    [Tooltip("Material de céu estrelado (Skybox2 no Gameplay Settings)")]
    public Material nightSkyboxMaterial;

    [Header("Iluminação urbana (postes)")]
    public bool enableCityStreetLights = true;

    [Tooltip("Intensidade da luz pontual à noite (plena)")]
    public float streetLightIntensity = 1.15f;

    public float streetLightRange = 11f;

    public Color streetLightColor = new(1f, 0.82f, 0.55f, 1f);

    public bool streetLightShadows;

    [Tooltip("0 = acende todos; senão só os N postes mais perto do jogador")]
    public int streetLightMaxActiveNearPlayer = 28;

    [Tooltip("Altura da luz em relação à base do poste Pole (metros).")]
    public float streetLightLampHeight = 3.4f;

    [Tooltip("Segundos exibindo a dica após dormir na barraca")]
    public float precariousSleepTipSeconds = 7.5f;

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

    [Tooltip("Total de carros em movimento (ActiveCityTraffic) ao dar Play.")]
    public int maxTrafficVehicles = 8;

    [Tooltip("Máximo de carros na mesma rota (evita 3 no mesmo ponto).")]
    [Range(1, 3)]
    public int trafficMaxCarsPerRoute = 1;

    [Tooltip("Espaçamento mínimo na fila (m).")]
    public float trafficFollowGapMeters = 9f;

    public float trafficMinSpeedKmh = 8f;
    public float trafficMaxSpeedKmh = 10f;

    [Tooltip("Metros antes do canto em que o carro já aponta para o próximo trecho (com velocidade baixa).")]
    public float trafficCornerFacingBlendMeters = 4f;

    [Tooltip("Aceleração do tráfego em rotas (m/s²).")]
    public float trafficAcceleration = 4.2f;

    [Tooltip("Frenagem do tráfego (m/s²).")]
    public float trafficBraking = 7.5f;

    [Tooltip("Dano base ao atropelar o jogador.")]
    public float trafficRunOverDamage = 36f;

    [Tooltip("Empurrão horizontal ao ser atingido.")]
    public float trafficRunOverKnockback = 6.5f;

    [Tooltip("Ponto à frente na rota para o volante (m). Maior = curvas mais suaves.")]
    public float trafficLookaheadMeters = 12f;

    [Tooltip("Limite de rotação do carro (graus/s) — evita virar “de lado”.")]
    public float trafficMaxSteerDegPerSec = 46f;

    [Tooltip("Se desligado, curvas só desaceleram (sem parar em waypoints).")]
    public bool trafficHonorStopWaypoints = false;

    [Tooltip("Spline suave nos cantos (recomendado). Desligado = linha reta entre waypoints (mais “duro”).")]
    public bool trafficUseSplineMotion = true;

    [Tooltip("Suaviza rotação do mesh (segundos). 0 = instantâneo.")]
    public float trafficSteerSmoothSeconds = 0.18f;

    [Header("Pedestres (NavMesh na Cidade)")]
    [Tooltip("Usa NavMeshAgent para andar por calçadas/ruas (não atravessa paredes). Exige NavMesh assado na cena.")]
    public bool cityPedestriansUseNavMesh = true;

    [Tooltip("NPCs extras além dos marcadores SidewalkNpcSpawnPoint.")]
    public int cityPedestrianExtraCount = 10;

    [Tooltip("Raio (m) para sortear destinos de passeio.")]
    public float cityPedestrianWanderRadius = 60f;

    [Tooltip("Velocidade média dos pedestres em NavMesh (m/s no mundo).")]
    public float cityPedestrianWalkSpeed = 1.1f;

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
