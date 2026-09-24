using UnityEngine;
using UnityEngine.SceneManagement;

public static class CityLivingBootstrap
{
    public static void EnsureForActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != RecomecoSceneNames.Cidade)
            return;

        var vehiclesRoot = GameObject.Find("Vehicles");
        if (vehiclesRoot != null)
            vehiclesRoot.isStatic = false;

        CityTrafficStarter.Schedule();
    }
}
