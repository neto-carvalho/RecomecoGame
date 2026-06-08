using UnityEngine.SceneManagement;

/// <summary>
/// Nomes das cenas usados em LoadScene (devem coincidir com o nome no topo da Hierarchy + Build Settings).
/// </summary>
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

    /// <summary>Alias legado — use <see cref="Cidade"/>.</summary>
    public const string CityDemo = Cidade;
}
