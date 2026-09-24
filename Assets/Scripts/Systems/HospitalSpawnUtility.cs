using UnityEngine;

public static class HospitalSpawnUtility
{
    public static bool TryGetHospitalSpawn(out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.identity;

        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp == null || sp.spawnId != RecomecoSceneNames.HospitalEntrada)
                continue;

            position = sp.transform.position + sp.positionOffset;
            rotation = sp.transform.rotation;
            return true;
        }

        if (!TryFindHospitalRoot(out var hospital))
            return false;

        position = hospital.position + hospital.forward * 4f + Vector3.up * 0.05f;
        rotation = Quaternion.LookRotation(hospital.forward, Vector3.up);
        return true;
    }

    public static bool TryFindHospitalRoot(out Transform hospital)
    {
        hospital = null;
        Transform best = null;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
                continue;

            var name = t.name;
            if (name.StartsWith("Spawn_"))
                continue;

            if (name == RecomecoSceneNames.HospitalRootName)
            {
                hospital = t;
                return true;
            }

            if (name.IndexOf("Hospital", System.StringComparison.OrdinalIgnoreCase) >= 0)
                best = t;
        }

        if (best == null)
            return false;

        hospital = best;
        return true;
    }

    public static void PlacePlayerAtHospital(GameObject player)
    {
        if (player == null)
            return;

        if (!TryGetHospitalSpawn(out var pos, out var rot))
        {
            Debug.LogWarning(
                "HospitalSpawnUtility: coloque o hospital na Cidade e rode " +
                "Recomeco → Cidade → Configurar hospital (spawn na entrada).");
            return;
        }

        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        player.transform.SetPositionAndRotation(pos, rot);

        if (cc != null)
            cc.enabled = true;

        SpawnGroundUtility.PlacePlayerOnGround(player, pos);
    }

}
