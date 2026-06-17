using UnityEngine;

[DisallowMultipleComponent]
public sealed class SidewalkNpcSpawnPoint : MonoBehaviour
{
    [Header("Spawn (o NPC em si é o prefab SidewalkNpc)")]
    [Tooltip("Metade do percurso em linha reta (metros), ao longo do forward deste objeto.")]
    [SerializeField] private float patrolHalfLength = 6f;

    [SerializeField] private float walkSpeed = 1.2f;

    [Tooltip("Se ativo, ignora o forward e usa o vetor abaixo (mundo, XZ).")]
    [SerializeField] private bool useCustomWorldPatrolDirection;

    [SerializeField] private Vector3 customPatrolWorldDirection = Vector3.forward;

    public float PatrolHalfLength => Mathf.Max(0.5f, patrolHalfLength);

    public float WalkSpeed => Mathf.Max(0.05f, walkSpeed);

    public Vector3 GetPatrolWorldDirection()
    {
        if (useCustomWorldPatrolDirection)
        {
            var d = customPatrolWorldDirection;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }

        var f = transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var origin = transform.position;
        var dir = GetPatrolWorldDirection();
        var half = PatrolHalfLength;
        Gizmos.color = Color.yellow;
        var a = origin - dir * half;
        var b = origin + dir * half;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(origin, 0.15f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, dir * 1.2f);
    }
#endif
}
