using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityTrafficBootstrap
{
    const string VehiclesRootName = "Vehicles";

    public static int EnsureForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
            return 0;

        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null && !settings.enableCityTraffic)
            return 0;

        var root = GameObject.Find(VehiclesRootName);
        if (root == null)
            return 0;

        var max = settings != null ? settings.maxTrafficVehicles : 24;
        var configured = 0;

        foreach (Transform child in root.transform)
        {
            if (child == null)
                continue;

            if (child.GetComponent<ParkedVehicleMarker>() != null)
                continue;

            if (child.GetComponent<PlayerDrivableVehicle>() != null)
                continue;

            if (configured >= max)
            {
                VehicleColliderUtility.PrepareTrafficObject(child.gameObject);
                continue;
            }

            var traffic = child.GetComponent<CityTrafficVehicle>();
            if (traffic == null)
                traffic = child.gameObject.AddComponent<CityTrafficVehicle>();

            traffic.ApplySettings(settings);
            traffic.PrepareForTraffic();
            if (!traffic.enabled)
                traffic.enabled = true;

            configured++;
        }

        return configured;
    }
}
