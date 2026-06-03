using UnityEngine;

/// <summary>
/// Define o som de passos deste collider. Prioridade sobre o nome do objeto.
/// Coloque em chão, calçada, relva, água (trigger/collider sólido).
/// </summary>
public sealed class FootstepSurfaceMarker : MonoBehaviour
{
    [SerializeField] FootstepSurfaceType surface = FootstepSurfaceType.Tile;

    public FootstepSurfaceType Surface => surface;
}
