using UnityEngine;

public static class PlayerSkateVisualFactory
{
    static readonly Color DeckColor = new(0.45f, 0.28f, 0.12f, 1f);
    static readonly Color WheelColor = new(0.15f, 0.15f, 0.15f, 1f);
    static readonly Color GripColor = new(0.2f, 0.2f, 0.22f, 1f);

    public static GameObject CreateUnderPlayer(Transform parent, RecomecoGameplaySettings settings)
    {
        if (parent == null)
            return null;

        var root = new GameObject("Skate_Visual");
        root.transform.SetParent(parent, false);

        var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Deck";
        deck.transform.SetParent(root.transform, false);
        deck.transform.localScale = new Vector3(0.55f, 0.06f, 0.18f);
        deck.transform.localPosition = new Vector3(0f, 0.03f, 0.02f);
        Object.Destroy(deck.GetComponent<Collider>());
        ApplyColor(deck, DeckColor);

        var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "Grip";
        grip.transform.SetParent(root.transform, false);
        grip.transform.localScale = new Vector3(0.48f, 0.02f, 0.14f);
        grip.transform.localPosition = new Vector3(0f, 0.065f, 0.02f);
        Object.Destroy(grip.GetComponent<Collider>());
        ApplyColor(grip, GripColor);

        var wheelOffsets = new[]
        {
            new Vector3(-0.2f, 0f, -0.05f),
            new Vector3(0.2f, 0f, -0.05f),
            new Vector3(-0.2f, 0f, 0.12f),
            new Vector3(0.2f, 0f, 0.12f),
        };

        foreach (var offset in wheelOffsets)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Wheel";
            wheel.transform.SetParent(root.transform, false);
            wheel.transform.localScale = new Vector3(0.08f, 0.03f, 0.08f);
            wheel.transform.localPosition = offset;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Object.Destroy(wheel.GetComponent<Collider>());
            ApplyColor(wheel, WheelColor);
        }

        ApplyFeetOffset(root.transform, parent, settings);
        return root;
    }

    public static void StripColliders(GameObject root)
    {
        if (root == null)
            return;

        foreach (var col in root.GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
                Object.Destroy(col);
        }
    }

    /// <summary>Pack Sir_bike usa Standard; o jogo roda em URP (magenta = shader errado).</summary>
    public static void FixUrpMaterials(GameObject root)
    {
        if (root == null)
            return;

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var shared = renderer.sharedMaterials;
            var changed = false;

            for (var i = 0; i < shared.Length; i++)
            {
                var oldMat = shared[i];
                if (oldMat == null || oldMat.shader == urpLit)
                    continue;

                var shaderName = oldMat.shader != null ? oldMat.shader.name : string.Empty;
                if (shaderName.Contains("Universal Render Pipeline"))
                    continue;

                var tex = oldMat.HasProperty("_MainTex") ? oldMat.GetTexture("_MainTex") : oldMat.mainTexture;
                var color = oldMat.HasProperty("_Color") ? oldMat.GetColor("_Color") : Color.white;

                var urpMat = new Material(urpLit);
                if (tex != null)
                    urpMat.mainTexture = tex;

                if (urpMat.HasProperty("_BaseColor"))
                    urpMat.SetColor("_BaseColor", color);

                if (urpMat.HasProperty("_Smoothness"))
                    urpMat.SetFloat("_Smoothness", 0.35f);

                shared[i] = urpMat;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = shared;
        }
    }

    public static void ApplyFeetOffset(Transform visual, Transform player, RecomecoGameplaySettings settings)
    {
        if (visual == null || player == null)
            return;

        var offset = settings != null ? settings.skateVisualLocalPosition : new Vector3(0f, 0.02f, 0.08f);
        var euler = settings != null ? settings.skateVisualLocalEuler : Vector3.zero;
        var scale = settings != null ? settings.skateVisualLocalScale : Vector3.one;
        var autoCenter = settings == null || settings.skateVisualAutoCenterBounds;

        visual.localScale = scale;
        visual.localRotation = Quaternion.Euler(euler);

        var pivotFix = Vector3.zero;
        var bottomY = 0f;
        if (autoCenter && TryGetRendererBoundsLocal(visual, out var center, out var min, out _))
        {
            pivotFix = new Vector3(center.x, 0f, center.z);
            bottomY = min.y;
        }

        var feetY = GetFeetLocalY(player);
        var y = feetY + offset.y;
        if (autoCenter)
            y -= bottomY;

        var anchor = GetFeetMidpointLocal(player);

        visual.localPosition = new Vector3(
            anchor.x + offset.x - pivotFix.x,
            y,
            anchor.z + offset.z - pivotFix.z);
    }

    /// <summary>Média dos pés no espaço local do player (animação de skate pode deslocar do pivot).</summary>
    public static Vector3 GetFeetMidpointLocal(Transform player)
    {
        if (player == null)
            return Vector3.zero;

        foreach (var animator in player.GetComponentsInChildren<Animator>(true))
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                continue;

            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (left == null || right == null)
                continue;

            var midWorld = (left.position + right.position) * 0.5f;
            return player.InverseTransformPoint(midWorld);
        }

        return Vector3.zero;
    }

    static bool TryGetRendererBoundsLocal(Transform root, out Vector3 center, out Vector3 min, out Vector3 max)
    {
        center = Vector3.zero;
        min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;

        var toLocal = root.worldToLocalMatrix;
        var any = false;

        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            var b = renderer.bounds;
            var c = b.center;
            var e = b.extents;

            for (var ix = -1; ix <= 1; ix += 2)
            {
                for (var iy = -1; iy <= 1; iy += 2)
                {
                    for (var iz = -1; iz <= 1; iz += 2)
                    {
                        var world = c + Vector3.Scale(e, new Vector3(ix, iy, iz));
                        var local = toLocal.MultiplyPoint3x4(world);
                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                        any = true;
                    }
                }
            }
        }

        if (!any)
            return false;

        center = (min + max) * 0.5f;
        return true;
    }

    public static float GetFeetLocalY(Transform player)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return 0f;

        return cc.center.y - cc.height * 0.5f;
    }

    static void ApplyColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null)
            return;

        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard")
                     ?? Shader.Find("Unlit/Color");
        if (shader == null)
            return;

        if (r.sharedMaterial == null || r.sharedMaterial.shader != shader)
            r.sharedMaterial = new Material(shader);

        if (r.sharedMaterial.HasProperty("_BaseColor"))
            r.sharedMaterial.SetColor("_BaseColor", color);
        else
            r.sharedMaterial.color = color;
    }
}