using UnityEngine;
using UnityEngine.SceneManagement;

public static class RecomecoSceneNames
{
    public const string MenuInicial = "MenuInicial";
    public const string FerroVelho = "FerroVelho";
    public const string Cidade = "Cidade";

    public const string MoradiaInicial = "MoradiaInicial";
    public const string EntradaCidade = "EntradaCidade";
    public const string EntradaFerroVelho = "EntradaFerroVelho";

    public static bool IsMenuScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;
        return scene.name == MenuInicial;
    }

    public static bool IsMenuScene(string sceneName)
    {
        return sceneName == MenuInicial;
    }

    public static bool AllowsLatinhaSpawn(Scene scene)
    {
        return scene.IsValid() && scene.name == Cidade;
    }

    public static bool AllowsLatinhaSpawn(string sceneName)
    {
        return sceneName == Cidade;
    }

    public const string CityDemo = Cidade;
}
