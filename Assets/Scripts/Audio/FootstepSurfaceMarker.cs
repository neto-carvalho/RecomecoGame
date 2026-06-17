using UnityEngine;

public sealed class FootstepSurfaceMarker : MonoBehaviour
{
    [SerializeField] FootstepSurfaceType surface = FootstepSurfaceType.Tile;

    public FootstepSurfaceType Surface => surface;
}
