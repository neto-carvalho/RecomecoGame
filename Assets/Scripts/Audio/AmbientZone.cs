using UnityEngine;

/// <summary>
/// Zona de ambiente (trigger). Coloque no terreno natureza ou num volume à volta do lago.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class AmbientZone : MonoBehaviour
{
    public enum ZoneKind
    {
        Nature,
        Lake,
    }

    [SerializeField] ZoneKind kind = ZoneKind.Nature;
    [SerializeField, Range(0f, 1f)] float natureBlend = 1f;
    [SerializeField, Range(0f, 1f)] float waterBlend = 1f;

    public ZoneKind Kind => kind;
    public float NatureBlend => natureBlend;
    public float WaterBlend => waterBlend;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        AmbientAudioController.NotifyEnter(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        AmbientAudioController.NotifyExit(this);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null)
            return;

        Gizmos.color = kind == ZoneKind.Lake
            ? new Color(0.2f, 0.5f, 1f, 0.25f)
            : new Color(0.2f, 0.85f, 0.3f, 0.25f);

        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);
        else if (col is SphereCollider sphere)
            Gizmos.DrawSphere(sphere.center, sphere.radius);
    }
#endif
}
