using UnityEngine;

/// <summary>
/// Coloca a latinha sobre o chão e alinha a base do modelo 3D ao solo.
/// </summary>
[DisallowMultipleComponent]
public class LatinhaPlacement : MonoBehaviour
{
    [Tooltip("Folga acima do ponto de impacto do chão (metros)")]
    public float clearanceAboveGround = 0.015f;

    [Tooltip("Distância máxima do raio para baixo")]
    public float raycastDownDistance = 12f;

    void Awake()
    {
        AlignToGround();
    }

    public void AlignToGround()
    {
        var planar = transform.position;
        if (!TryGetGroundY(planar, out var groundY))
            return;

        transform.position = new Vector3(planar.x, groundY + clearanceAboveGround, planar.z);
        SinkVisualBottomToPivot();
    }

    void SinkVisualBottomToPivot()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var floatAbovePivot = bounds.min.y - transform.position.y;
        if (floatAbovePivot > 0.001f)
            transform.position -= new Vector3(0f, floatAbovePivot, 0f);
    }

    public static bool TryGetGroundY(Vector3 planar, out float groundY)
    {
        groundY = 0f;
        var foundTerrain = false;

        foreach (var terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            var local = planar - pos;
            if (local.x < 0f || local.z < 0f || local.x > size.x || local.z > size.z)
                continue;

            var y = terrain.SampleHeight(planar) + pos.y;
            if (!foundTerrain || y > groundY)
            {
                groundY = y;
                foundTerrain = true;
            }
        }

        if (foundTerrain)
            return true;

        var origin = new Vector3(planar.x, planar.y + 80f, planar.z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, 250f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                groundY = hit.point.y;
                return true;
            }
        }

        return false;
    }
}
