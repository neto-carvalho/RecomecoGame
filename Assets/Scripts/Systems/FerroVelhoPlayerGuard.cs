using UnityEngine;

/// <summary>
/// Evita queda no void na cena FerroVelho — reposiciona no chão se necessário.
/// </summary>
[DefaultExecutionOrder(-5000)]
public class FerroVelhoPlayerGuard : MonoBehaviour
{
    void Awake()
    {
        if (!FerroVelhoWalkableGround.IsFerroVelhoActive())
        {
            Destroy(this);
            return;
        }

        SnapToGround();
    }

    void LateUpdate()
    {
        if (transform.position.y < FerroVelhoWalkableGround.SurfaceY - 3f)
            SnapToGround(FerroVelhoWalkableGround.DefaultSpawn);
    }

    void SnapToGround()
    {
        SnapToGround(transform.position);
    }

    void SnapToGround(Vector3 near)
    {
        FerroVelhoWalkableGround.EnsureInActiveScene();
        SpawnGroundUtility.PlacePlayerOnGround(gameObject, near);
    }
}
