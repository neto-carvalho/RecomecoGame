using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class SidewalkNpcWalker : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Patrulha")]
    [SerializeField] private Vector3 patrolWorldDirection = Vector3.forward;
    [SerializeField] private float patrolHalfLength = 6f;
    [SerializeField] private float walkSpeed = 1.35f;

    [Header("Virar no próprio eixo (Inspector no prefab SidewalkNpc, não no spawn)")]
    [Tooltip("Só avança quando o corpo já aponta quase para a direção de marcha.")]
    [SerializeField] private float alignAngleToMoveDegrees = 12f;
    [Tooltip("Velocidade de rotação em marcha (apenas eixo Y).")]
    [SerializeField] private float rotateSpeedDegrees = 360f;
    [Tooltip("Velocidade de rotação enquanto alinha ao virar (apenas eixo Y).")]
    [SerializeField] private float rotateSpeedWhileAligningDegrees = 720f;

    [Header("Chão e obstáculos")]
    [SerializeField] private float gravity = -22f;
    [SerializeField] private bool snapToGroundOnStart = true;
    [SerializeField] private float groundProbeHeight = 3f;
    [SerializeField] private float groundProbeDistance = 12f;
    [Tooltip("Raio à frente: parede quase vertical inverte o sentido da patrulha.")]
    [SerializeField] private float wallProbeDistance = 0.42f;

    [Header("Apresentação — 2 animações (andar / parar breve)")]
    [Tooltip("Se ativo, alterna andar com uma pausa curta (idle: Hor/Vert → 0).")]
    [SerializeField] private bool useWalkWithBriefIdlePauses = true;
    [Tooltip("Quanto tempo anda antes de parar (segundos).")]
    [SerializeField] private float walkSegmentDurationSeconds = 5f;
    [Tooltip("Quanto tempo fica parado (idle) antes de voltar a andar (segundos).")]
    [SerializeField] private float idlePauseDurationSeconds = 1.25f;
    [Tooltip("Velocidade com que «State» volta a 0 (andar, sem corrida).")]
    [SerializeField] private float animatorStateBlendSpeed = 4f;

    [Header("Skins completos (como o jogador / ithappy)")]
    [Tooltip(
        "Opcional: um prefab por variante (índice = ordem do spawn). Deve ser a hierarquia visual completa " +
        "(ex.: Base_Mesh + roupa/cabelo como montas no Player), **sem** CharacterController no prefab. " +
        "Se a entrada do índice estiver vazia, mantém-se o Base_Mesh embebido do SidewalkNpc.")]
    [SerializeField] private GameObject[] optionalFullCharacterRigPrefabs = new GameObject[4];

    [Header("Visual — só se NÃO usares prefabs completos acima")]
    [Tooltip("Textura por variante (MaterialPropertyBlock). Ignorado se existir prefab completo para esse índice.")]
    [SerializeField] private Texture2D[] diffuseTextures = new Texture2D[4];
    [Tooltip("Cor que multiplica o albedo quando não há prefab completo.")]
    [SerializeField] private Color[] tintPalette =
    {
        new(1f, 0.92f, 0.88f, 1f),
        new(0.75f, 0.88f, 1f, 1f),
        new(0.85f, 1f, 0.82f, 1f),
        new(1f, 0.82f, 0.92f, 1f),
    };

    private CharacterController _controller;
    private Animator _animator;
    private Vector3 _patrolOrigin;
    private Vector3 _moveDir;
    private float _verticalVelocity;
    private bool _hasHor;
    private bool _hasVert;
    private bool _hasState;
    private bool _hasIsJump;
    private float _locomotionPhaseTimer;
    private bool _inIdlePause;
    private int _skinVariantFromSpawn = int.MinValue;
    private MaterialPropertyBlock _skinBlock;
    private bool _replacedDefaultRig;

    private void Awake()
    {
        _skinBlock = new MaterialPropertyBlock();
        _controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        _patrolOrigin = transform.position;

        patrolWorldDirection.y = 0f;
        if (patrolWorldDirection.sqrMagnitude < 0.0001f)
            patrolWorldDirection = Vector3.forward;
        _moveDir = patrolWorldDirection.normalized;

        _inIdlePause = false;
        _locomotionPhaseTimer = Random.Range(
            0.35f * Mathf.Max(0.1f, walkSegmentDurationSeconds),
            Mathf.Max(0.1f, walkSegmentDurationSeconds));

        var variant = _skinVariantFromSpawn >= 0
            ? _skinVariantFromSpawn
            : Random.Range(0, Mathf.Max(1, tintPalette != null && tintPalette.Length > 0 ? tintPalette.Length : 4));

        _replacedDefaultRig = TryReplaceCharacterRig(variant);
        CacheAnimatorFromHierarchy();

        if (!_replacedDefaultRig)
            ApplySkinVisual(variant);

        if (snapToGroundOnStart)
        {
            if (_animator != null)
                _animator.Update(0f);

            CharacterGroundSnap.FitControllerToWorldScale(_controller, 2f, new Vector3(0f, 1f, 0f), 0.35f, 0.25f, 0.08f);
            CharacterGroundSnap.TrySnap(transform, _controller, 0.02f, groundProbeHeight + 40f, groundProbeDistance + 80f);
            _patrolOrigin = transform.position;
        }
    }

    private void CacheAnimatorFromHierarchy()
    {
        _hasHor = _hasVert = _hasState = _hasIsJump = false;
        _animator = FindLocomotionAnimator();
        if (_animator == null)
            return;

        _animator.applyRootMotion = false;
        _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach (var p in _animator.parameters)
        {
            if (p.name == "Hor") _hasHor = true;
            if (p.name == "Vert") _hasVert = true;
            if (p.name == "State") _hasState = true;
            if (p.name == "IsJump") _hasIsJump = true;
        }

       
        if (_hasHor || _hasVert)
        {
            _animator.Play("Movement", 0, 0f);
            _animator.Update(0f);
        }
    }

    private Animator FindLocomotionAnimator()
    {
        Animator fallback = null;
        foreach (var a in GetComponentsInChildren<Animator>(true))
        {
            if (a == null || !a.enabled)
                continue;

            if (a.runtimeAnimatorController == null)
                continue;

            fallback ??= a;

            foreach (var p in a.parameters)
            {
                if (p.name != "Hor" && p.name != "Vert")
                    continue;
                return a;
            }
        }

        return fallback ?? GetComponentInChildren<Animator>(true);
    }

    private bool TryReplaceCharacterRig(int variantIndex)
    {
        if (optionalFullCharacterRigPrefabs == null || optionalFullCharacterRigPrefabs.Length == 0)
            return false;

        var len = optionalFullCharacterRigPrefabs.Length;
        var slot = Mathf.Abs(variantIndex) % len;
        var prefab = optionalFullCharacterRigPrefabs[slot];
        if (prefab == null)
            return false;

        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(ch);
            else
                DestroyImmediate(ch);
        }

        var inst = Instantiate(prefab, transform);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        inst.name = prefab.name;

        if (!HasSkinnedMeshBonesInHierarchy(inst))
        {
            Debug.LogError(
                $"[SidewalkNpc] O prefab '{prefab.name}' não tem ossos na hierarquia (só o mesh). " +
                "No Unity: Recomeco → NPC → Mixamo → Reparar prefab(s) de skin selecionado(s).",
                prefab);
        }

        return true;
    }

    static bool HasSkinnedMeshBonesInHierarchy(GameObject root)
    {
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.bones == null || smr.bones.Length == 0)
                continue;
            var bone = smr.bones[0];
            if (bone != null && bone.transform.IsChildOf(root.transform))
                return true;
        }

        return root.transform.childCount > 0;
    }

    Transform _interactionFaceTarget;

    public bool InteractionPaused { get; private set; }

    public void PauseForInteraction(Transform faceTarget)
    {
        InteractionPaused = true;
        _interactionFaceTarget = faceTarget;
    }

    public void ResumeFromInteraction()
    {
        InteractionPaused = false;
        _interactionFaceTarget = null;
    }

    void UpdatePausedInteraction()
    {
        if (_interactionFaceTarget != null)
            ApplyYawDegreesTowardsWorldDirection(
                _interactionFaceTarget.position - transform.position, rotateSpeedDegrees);

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        _verticalVelocity += gravity * Time.deltaTime;
        _controller.Move(Vector3.up * (_verticalVelocity * Time.deltaTime));

        if (_animator == null)
            return;

        var dt = Time.deltaTime;
        if (_hasHor)
            _animator.SetFloat("Hor", Mathf.MoveTowards(_animator.GetFloat("Hor"), 0f, 6f * dt));
        if (_hasVert)
            _animator.SetFloat("Vert", Mathf.MoveTowards(_animator.GetFloat("Vert"), 0f, 6f * dt));
    }

    private void Update()
    {
        if (InteractionPaused)
        {
            UpdatePausedInteraction();
            return;
        }

        var planar = transform.position - _patrolOrigin;
        planar.y = 0f;
        var along = Vector3.Dot(planar, _moveDir);
        if (along > patrolHalfLength || along < -patrolHalfLength)
            FlipPatrolDirection();

        var probeOrigin = transform.position + Vector3.up * 0.95f;
        if (Physics.Raycast(probeOrigin, _moveDir, out var wallHit, wallProbeDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(Vector3.Dot(wallHit.normal, Vector3.up)) < 0.55f)
                FlipPatrolDirection();
        }

        var flatFwd = transform.forward;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude > 0.0001f)
            flatFwd.Normalize();
        else
            flatFwd = _moveDir;

        var angleToMoveDir = Vector3.Angle(flatFwd, _moveDir);
        var aligned = angleToMoveDir <= alignAngleToMoveDegrees;

        var rotSpeed = aligned ? rotateSpeedDegrees : rotateSpeedWhileAligningDegrees;
        ApplyYawDegreesTowardsWorldDirection(_moveDir, rotSpeed);

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;
        _verticalVelocity += gravity * Time.deltaTime;

        var dt = Time.deltaTime;
        if (aligned && useWalkWithBriefIdlePauses)
        {
            _locomotionPhaseTimer -= dt;
            if (_locomotionPhaseTimer <= 0f)
            {
                if (_inIdlePause)
                {
                    _inIdlePause = false;
                    _locomotionPhaseTimer = Mathf.Max(0.1f, walkSegmentDurationSeconds);
                }
                else
                {
                    _inIdlePause = true;
                    _locomotionPhaseTimer = Mathf.Max(0.05f, idlePauseDurationSeconds);
                }
            }
        }

        var mayWalk = aligned && (!useWalkWithBriefIdlePauses || !_inIdlePause);
        var horizontal = mayWalk ? _moveDir * walkSpeed : Vector3.zero;
        var delta = (horizontal + Vector3.up * _verticalVelocity) * Time.deltaTime;
        _controller.Move(delta);

        UpdateAnimator();
    }

    private void FlipPatrolDirection()
    {
        _moveDir = -_moveDir;
        _patrolOrigin = transform.position;
        if (useWalkWithBriefIdlePauses)
        {
            _inIdlePause = false;
            _locomotionPhaseTimer = Mathf.Max(0.1f, walkSegmentDurationSeconds * 0.45f);
        }
    }

    private void ApplyYawDegreesTowardsWorldDirection(Vector3 worldDirXZ, float maxDegreesPerSecond)
    {
        worldDirXZ.y = 0f;
        if (worldDirXZ.sqrMagnitude < 1e-8f)
            return;
        worldDirXZ.Normalize();

        var fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-8f)
            fwd = worldDirXZ;
        else
            fwd.Normalize();

        var currentYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        var targetYaw = Mathf.Atan2(worldDirXZ.x, worldDirXZ.z) * Mathf.Rad2Deg;
        var delta = Mathf.DeltaAngle(currentYaw, targetYaw);
        var maxStep = maxDegreesPerSecond * Time.deltaTime;
        var step = Mathf.Clamp(delta, -maxStep, maxStep);
        transform.Rotate(0f, step, 0f, Space.World);

        var e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, e.y, 0f);
    }

    private void ApplySkinVisual(int variantIndex)
    {
        if (tintPalette == null || tintPalette.Length == 0)
            tintPalette = new[] { Color.white };

        var ti = Mathf.Abs(variantIndex) % tintPalette.Length;
        var tint = tintPalette[ti];

        Texture2D tex = null;
        if (diffuseTextures != null && diffuseTextures.Length > 0)
        {
            var slot = Mathf.Abs(variantIndex) % diffuseTextures.Length;
            tex = diffuseTextures[slot];
        }

        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            for (var m = 0; m < r.sharedMaterials.Length; m++)
            {
                var mat = r.sharedMaterials[m];
                if (mat == null)
                    continue;

                r.GetPropertyBlock(_skinBlock, m);

                if (mat.HasProperty(BaseColorId))
                    _skinBlock.SetColor(BaseColorId, tint);
                else if (mat.HasProperty(ColorId))
                    _skinBlock.SetColor(ColorId, tint);

                if (tex != null)
                {
                    if (mat.HasProperty(BaseMapId))
                        _skinBlock.SetTexture(BaseMapId, tex);
                    if (mat.HasProperty(MainTexId))
                        _skinBlock.SetTexture(MainTexId, tex);
                }

                r.SetPropertyBlock(_skinBlock, m);
            }
        }
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        var flatFwd = transform.forward;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude > 0.0001f)
            flatFwd.Normalize();
        else
            flatFwd = _moveDir;
        var aligned = Vector3.Angle(flatFwd, _moveDir) <= alignAngleToMoveDegrees;

        var walkingAnim = aligned && _moveDir.sqrMagnitude > 0.01f && walkSpeed > 0.05f
                          && (!useWalkWithBriefIdlePauses || !_inIdlePause);
        var dt = Time.deltaTime;

        var local = transform.InverseTransformDirection(_moveDir);
        local.y = 0f;
        if (local.sqrMagnitude > 0.0001f)
            local.Normalize();

        var horTarget = walkingAnim ? local.x : 0f;
        var vertTarget = walkingAnim ? local.z : 0f;

       
        var blendStep = walkingAnim ? 12f * dt : 4.5f * dt;
        if (_hasHor)
            _animator.SetFloat("Hor", Mathf.MoveTowards(_animator.GetFloat("Hor"), horTarget, blendStep));
        if (_hasVert)
            _animator.SetFloat("Vert", Mathf.MoveTowards(_animator.GetFloat("Vert"), vertTarget, blendStep));
        if (_hasState)
        {
           
            var stateTarget = 0f;
            _animator.SetFloat(
                "State",
                Mathf.MoveTowards(_animator.GetFloat("State"), stateTarget, animatorStateBlendSpeed * dt));
        }
        if (_hasIsJump)
            _animator.SetBool("IsJump", false);
    }

    public void ConfigurePatrol(in Vector3 worldDirection, float halfLength, float speed, int skinVariantIndex = -1)
    {
        patrolWorldDirection = worldDirection;
        patrolWorldDirection.y = 0f;
        if (patrolWorldDirection.sqrMagnitude < 0.0001f)
            patrolWorldDirection = Vector3.forward;
        patrolHalfLength = Mathf.Max(0.5f, halfLength);
        walkSpeed = Mathf.Max(0.05f, speed);
        if (skinVariantIndex >= 0)
            _skinVariantFromSpawn = skinVariantIndex;
    }
}
