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

    public const string InteriorCasaElegante = "Interior Casa elegante (player)";
    public const string EntradaCasaElegante = "EntradaCasaElegante";
    public const string SaidaCasaElegante = "SaidaCasaElegante";

    public const string CasaEleganteRootName = "Casa elegante (player)";

    public const string HospitalEntrada = "HospitalEntrada";
    public const string HospitalRootName = "Hospital_Cidade";

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
