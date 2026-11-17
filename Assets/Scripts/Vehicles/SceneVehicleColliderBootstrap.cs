using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante colliders em veículos de cena (decor, interior, táxi parado).
/// </summary>
public static class SceneVehicleColliderBootstrap
{
    public static void EnsureForActiveScene()
    {
        if (RecomecoSceneNames.IsMenuScene(SceneManager.GetActiveScene()))
            return;

        foreach (var drivable in Object.FindObjectsByType<PlayerDrivableVehicle>(FindObjectsSortMode.None))
        {
            if (drivable != null)
                VehicleColliderUtility.EnsureSceneVehicleBlocksPlayer(drivable.gameObject);
        }

        var vehiclesRoot = GameObject.Find("Vehicles");
        if (vehiclesRoot != null)
            EnsureHierarchy(vehiclesRoot.transform);

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            EnsureHierarchy(root.transform);
    }

    static void EnsureHierarchy(Transform node)
    {
        if (node == null)
            return;

        if (ShouldEnsureColliderForName(node.name))
            VehicleColliderUtility.EnsureSceneVehicleBlocksPlayer(node.gameObject);

        for (var i = 0; i < node.childCount; i++)
            EnsureHierarchy(node.GetChild(i));
    }

    static bool ShouldEnsureColliderForName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (IsParkedInteriorVehicleName(objectName))
            return true;

        return objectName.StartsWith("Car", System.StringComparison.OrdinalIgnoreCase)
               || objectName.StartsWith("Cart", System.StringComparison.OrdinalIgnoreCase)
               || objectName.IndexOf("Taxi", System.StringComparison.OrdinalIgnoreCase) >= 0
               || objectName.IndexOf("Táxi", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsParkedInteriorVehicleName(string objectName)
    {
        return objectName.StartsWith("vehicle", System.StringComparison.OrdinalIgnoreCase)
               && objectName.IndexOf("interior", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}