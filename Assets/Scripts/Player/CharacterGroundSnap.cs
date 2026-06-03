using UnityEngine;

/// <summary>
/// Encosta os pés visuais ao chão (raycast). Ajusta o CharacterController quando a escala do transform ≠ 1.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class CharacterGroundSnap : MonoBehaviour
{
    [SerializeField] float clearance = 0.02f;
    [SerializeField] float raycastUp = 8f;
    [SerializeField] float raycastDown = 120f;
    [SerializeField] bool snapOnStart = true;
    [SerializeField] bool fitControllerToScale = true;

    [Header("Referência do CharacterController (escala 1)")]
    [SerializeField] float referenceHeight = 1.8f;
    [SerializeField] Vector3 referenceCenter = new(0f, 0.95f, 0f);
    [SerializeField] float referenceRadius = 0.4f;
    [SerializeField] float referenceStepOffset = 0.3f;
    [SerializeField] float referenceSkinWidth = 0.08f;

    CharacterController _controller;

    void Awake() => _controller = GetComponent<CharacterController>();

    void Start()
    {
        if (fitControllerToScale)
            FitControllerToWorldScale(_controller, referenceHeight, referenceCenter, referenceRadius,
                referenceStepOffset, referenceSkinWidth);

        if (snapOnStart)
            SnapNow();
    }

    public bool SnapNow() => TrySnap(transform, _controller, clearance, raycastUp, raycastDown);

    public static void FitControllerToWorldScale(
        CharacterController controller,
        float referenceHeight = 1.8f,
        Vector3? referenceCenter = null,
        float referenceRadius = 0.4f,
        float referenceStepOffset = 0.3f,
        float referenceSkinWidth = 0.08f)
    {
        if (controller == null)
            return;

        var scale = Mathf.Abs(controller.transform.lossyScale.y);
        if (Mathf.Approximately(scale, 1f))
            return;

        var center = referenceCenter ?? new Vector3(0f, 0.95f, 0f);
        controller.height = Mathf.Max(0.2f, referenceHeight * scale);
        controller.center = center * scale;
        controller.radius = Mathf.Max(0.04f, referenceRadius * scale);
        controller.stepOffset = Mathf.Min(referenceStepOffset * scale, controller.height * 0.45f);
        controller.skinWidth = Mathf.Max(0.01f, referenceSkinWidth * scale);
    }

    public static bool TrySnap(
        Transform character,
        CharacterController controller,
        float clearance = 0.02f,
        float raycastUp = 8f,
        float raycastDown = 120f)
    {
        if (character == null || controller == null)
            return false;

        var feetY = GetFeetBottomY(character, controller);
        var origin = new Vector3(character.position.x, feetY + raycastUp, character.position.z);

        if (!Physics.Raycast(origin, Vector3.down, out var hit, raycastUp + raycastDown, ~0, QueryTriggerInteraction.Ignore))
            return false;

        var scaleY = Mathf.Max(0.01f, Mathf.Abs(character.lossyScale.y));
        var scaledClearance = clearance * Mathf.Clamp(scaleY, 0.15f, 1f);

        var deltaY = hit.point.y + scaledClearance - feetY;
        if (Mathf.Abs(deltaY) < 0.0005f)
            return true;

        character.position += new Vector3(0f, deltaY, 0f);
        return true;
    }

    static float GetFeetBottomY(Transform character, CharacterController controller)
    {
        if (TryGetHumanoidFeetY(character, out var humanoidFeet))
            return humanoidFeet;

        if (TryGetRendererBoundsBottomY(character, out var boundsFeet))
            return boundsFeet;

        return character.position.y + controller.center.y - controller.height * 0.5f;
    }

    static bool TryGetHumanoidFeetY(Transform character, out float feetY)
    {
        feetY = 0f;
        Animator animator = null;
        foreach (var a in character.GetComponentsInChildren<Animator>(true))
        {
            if (a == null || !a.isHuman || a.avatar == null || !a.avatar.isHuman)
                continue;
            animator = a;
            break;
        }

        if (animator == null)
            return false;

        var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (left == null && right == null)
            return false;

        if (left != null && right != null)
            feetY = Mathf.Min(left.position.y, right.position.y);
        else
            feetY = (left != null ? left : right).position.y;

        return true;
    }

    static bool TryGetRendererBoundsBottomY(Transform character, out float bottomY)
    {
        bottomY = 0f;
        var renderers = character.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        bottomY = bounds.min.y;
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Snap To Ground Now")]
    void EditorSnap()
    {
        if (_controller == null)
            _controller = GetComponent<CharacterController>();

        if (fitControllerToScale)
            FitControllerToWorldScale(_controller, referenceHeight, referenceCenter, referenceRadius,
                referenceStepOffset, referenceSkinWidth);

        SnapNow();
    }
#endif
}
