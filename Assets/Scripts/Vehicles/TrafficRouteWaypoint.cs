using UnityEngine;

/// <summary>
/// Opcional em cada waypoint filho de uma <see cref="TrafficRoute"/>.
/// Use em cruzamentos para parar em fila ou ceder passagem.
/// </summary>
[DisallowMultipleComponent]
public sealed class TrafficRouteWaypoint : MonoBehaviour
{
    [Tooltip("Para totalmente neste ponto (semáforo / stop).")]
    public bool requireFullStop;

    [Tooltip("Segundos parado antes de seguir.")]
    public float stopSeconds = 1.1f;

    [Tooltip("Antes de sair, olha tráfego cruzado (raio perpendicular à rota).")]
    public bool yieldToCrossTraffic = true;

    [Tooltip("Distância (m) do raio para deteção cruzada.")]
    public float crossTrafficProbeMeters = 11f;
}
