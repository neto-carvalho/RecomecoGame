using UnityEngine;

/// <summary>
/// Define a superfície do lago e aplica efeito subaquático quando a câmara do jogador fica abaixo da água.
/// Coloca num objeto com collider trigger (caixa) que cubra o volume do lago.
/// </summary>
[DisallowMultipleComponent]
public sealed class LakeWaterZone : MonoBehaviour
{
    [Tooltip("Superfície da água (Y mundial). Se vazio, usa o Lake_Water mais próximo ou este transform.")]
    [SerializeField] private Transform waterSurface;

    [Tooltip("Offset abaixo da superfície para considerar \"debaixo de água\" (evita flicker na borda).")]
    [SerializeField] private float surfaceSubmergeOffset = 0.15f;

    [Tooltip("Quanto mergulhar para efeito máximo (metros abaixo da superfície).")]
    [SerializeField] private float fullSubmergeDepth = 1.25f;

    [SerializeField] private string playerTag = "Player";

    Transform _cameraTransform;
    UnderwaterCameraEffect _underwaterEffect;

    float WaterSurfaceY =>
        waterSurface != null ? waterSurface.position.y : transform.position.y;

    void Start()
    {
        ResolveCameraAndEffect();
        if (waterSurface == null)
        {
            var lake = GameObject.Find("Lake_Water");
            if (lake != null)
                waterSurface = lake.transform;
        }
    }

    void Update()
    {
        if (_cameraTransform == null)
        {
            ResolveCameraAndEffect();
            if (_cameraTransform == null)
                return;
        }

        var camY = _cameraTransform.position.y;
        var surfaceY = WaterSurfaceY - surfaceSubmergeOffset;
        float blend;

        if (camY >= surfaceY)
            blend = 0f;
        else
        {
            var depth = surfaceY - camY;
            blend = Mathf.Clamp01(depth / Mathf.Max(0.05f, fullSubmergeDepth));
        }

        if (_underwaterEffect != null)
            _underwaterEffect.SetSubmergedAmount(blend);
    }

    void ResolveCameraAndEffect()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            var cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
                _cameraTransform = cam.transform;
        }

        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        if (_cameraTransform == null)
            return;

        _underwaterEffect = _cameraTransform.GetComponent<UnderwaterCameraEffect>();
        if (_underwaterEffect == null)
            _underwaterEffect = _cameraTransform.gameObject.AddComponent<UnderwaterCameraEffect>();
    }

    void OnDrawGizmosSelected()
    {
        var y = WaterSurfaceY;
        var p = transform.position;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
        Gizmos.DrawLine(new Vector3(p.x - 15f, y, p.z), new Vector3(p.x + 15f, y, p.z));
        Gizmos.DrawLine(new Vector3(p.x, y, p.z - 15f), new Vector3(p.x, y, p.z + 15f));
    }
}
