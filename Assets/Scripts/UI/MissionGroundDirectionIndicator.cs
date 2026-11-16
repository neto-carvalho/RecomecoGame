using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arco amarelo no chão aos pés do jogador, apontando o objetivo (textura com alpha).
/// </summary>
public sealed class MissionGroundDirectionIndicator : MonoBehaviour
{
    const string TextureResourcePath = "UI/mission_foot_direction_arc";
    const float GroundLift = 0.04f;
    const float HideWithinMeters = 4.5f;
    /// <summary>Largura do arco no mundo (escala cidade 0,2).</summary>
    const float BaseDiameterAtRefScale = 0.52f;
    const float RefPlayerScale = 0.2f;
    const float YawSmoothTime = 0.14f;
    const float PlanarSmoothTime = 0.07f;
    const float GroundYSmoothTime = 0.12f;

    Transform _root;
    MeshRenderer _renderer;
    Material _material;
    float _textureAspect = 1.6f;
    float _smoothedYaw;
    float _yawVelocity;
    float _smoothedGroundY;
    float _groundYVelocity;
    Vector3 _smoothedPlanar;
    Vector3 _planarVelocity;
    bool _smoothingInitialized;

    void Awake()
    {
        EnsureBuilt();
    }

    void OnEnable()
    {
        MissionProgress.Changed += RefreshActive;
        RefreshActive();
    }

    void OnDisable()
    {
        MissionProgress.Changed -= RefreshActive;
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (_root != null)
            Destroy(_root.gameObject);
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled || _root == null)
            return;

        var target = MissionObjectiveLocator.GetCurrentTarget();
        if (!target.HasTarget)
        {
            ResetSmoothing();
            SetVisible(false);
            return;
        }

        var feet = ResolveFeetPlanar(transform);
        var bearingOrigin = feet;
        bearingOrigin.y = feet.y;

        var toTarget = target.WorldPosition - bearingOrigin;
        toTarget.y = 0f;
        var distance = toTarget.magnitude;
        if (distance < HideWithinMeters)
        {
            ResetSmoothing();
            SetVisible(false);
            return;
        }

        if (!_smoothingInitialized)
            InitializeSmoothing(feet, toTarget);

        var targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        _smoothedYaw = Mathf.SmoothDampAngle(_smoothedYaw, targetYaw, ref _yawVelocity, YawSmoothTime);

        var ground = SnapToGround(feet);
        _smoothedGroundY = Mathf.SmoothDamp(_smoothedGroundY, ground.y, ref _groundYVelocity, GroundYSmoothTime);

        var targetPlanar = new Vector3(feet.x, _smoothedGroundY, feet.z);
        _smoothedPlanar = Vector3.SmoothDamp(_smoothedPlanar, targetPlanar, ref _planarVelocity, PlanarSmoothTime);

        _root.SetPositionAndRotation(
            _smoothedPlanar + Vector3.up * GroundLift,
            Quaternion.Euler(90f, _smoothedYaw, 0f));

        var playerScale = Mathf.Max(0.12f, transform.lossyScale.y);
        var diameterWorld = BaseDiameterAtRefScale * (playerScale / RefPlayerScale);
        var invScale = 1f / playerScale;
        var width = diameterWorld * invScale;
        var height = diameterWorld * invScale / Mathf.Max(0.45f, _textureAspect);
        _root.localScale = new Vector3(width, height, 1f);

        SetVisible(true);
    }

    void RefreshActive()
    {
        if (!MissionObjectiveLocator.GetCurrentTarget().HasTarget)
        {
            ResetSmoothing();
            SetVisible(false);
        }
    }

    void SetVisible(bool visible)
    {
        if (_root == null)
            return;

        if (visible && !_root.gameObject.activeSelf)
            ResetSmoothing();

        _root.gameObject.SetActive(visible);
    }

    void InitializeSmoothing(Vector3 feet, Vector3 flatToTarget)
    {
        var ground = SnapToGround(feet);
        _smoothedYaw = Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
        _smoothedGroundY = ground.y;
        _smoothedPlanar = new Vector3(feet.x, ground.y, feet.z);
        _yawVelocity = 0f;
        _groundYVelocity = 0f;
        _planarVelocity = Vector3.zero;
        _smoothingInitialized = true;
    }

    void ResetSmoothing()
    {
        _smoothingInitialized = false;
        _yawVelocity = 0f;
        _groundYVelocity = 0f;
        _planarVelocity = Vector3.zero;
    }

    void EnsureBuilt()
    {
        if (_root != null)
            return;

        var tex = LoadArcTexture();
        if (tex != null)
            _textureAspect = tex.width / (float)Mathf.Max(1, tex.height);

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "MissionFootDirectionArc";
        Object.Destroy(go.GetComponent<Collider>());

        _root = go.transform;
        _root.SetParent(null, false);

        _renderer = go.GetComponent<MeshRenderer>();
        _material = CreateArcMaterial(tex);
        _renderer.sharedMaterial = _material;
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        go.SetActive(false);
    }

    static Texture2D LoadArcTexture()
    {
        var tex = Resources.Load<Texture2D>(TextureResourcePath);
        if (tex != null)
            return tex;

        var sprite = Resources.Load<Sprite>(TextureResourcePath);
        return sprite != null ? sprite.texture : null;
    }

    static Material CreateArcMaterial(Texture2D texture)
    {
        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Transparent")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");

        var mat = new Material(shader);
        mat.name = "MissionFootArcMat";

        if (texture != null)
        {
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", texture);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", texture);
        }

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        SetMaterialColor(mat, Color.white);
        return mat;
    }

    static void SetMaterialColor(Material mat, Color color)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
    }

    static Vector3 ResolveFeetPlanar(Transform player)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return player.position;

        var bottomY = player.position.y + cc.center.y - cc.height * 0.5f;
        var planar = player.position;
        planar.y = bottomY;
        return planar;
    }

    static Vector3 SnapToGround(Vector3 nearFeet)
    {
        var origin = nearFeet + Vector3.up * 0.25f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.normal.y > 0.45f)
                return new Vector3(nearFeet.x, hit.point.y, nearFeet.z);
        }

        if (InteriorSceneLayout.IsInteriorScene(SceneManager.GetActiveScene()))
            return SpawnGroundUtility.GetGroundPosition(nearFeet, 12f, 28f);

        return SpawnGroundUtility.GetGroundPosition(nearFeet, 8f, 40f);
    }
}