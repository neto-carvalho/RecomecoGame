using UnityEngine.SceneManagement;

public static class RecomecoSceneNames
{
    public const string MenuInicial = "MenuInicial";
    public const string FerroVelho = "FerroVelho";
    public const string Cidade = "Cidade";

    public static bool IsMenuScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;
        return scene.name == MenuInicial;
    }

    public const string CityDemo = Cidade;
}
