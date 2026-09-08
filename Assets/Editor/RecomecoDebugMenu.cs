#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RecomecoDebugMenu
{
    const string SettingsPath = "Assets/Resources/RecomecoGameplaySettings.asset";

    [MenuItem("Recomeco/Debug/Ativar dinheiro de teste (R$ 5.000 ao iniciar)")]
    static void EnableTestMoney()
    {
        var settings = LoadSettings();
        if (settings == null)
            return;

        settings.enableDebugCheats = true;
        settings.useTestStartingMoney = true;
        settings.testStartingMoneyCents = 500000;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Modo teste",
            "Ativado.\n\n" +
            "• Nova partida começa com R$ 5.000\n" +
            "• Durante o Play (Editor):\n" +
            "  F9  +R$ 500\n" +
            "  F10 dinheiro máximo\n" +
            "  F11 liberar Casa elegante\n" +
            "  F12 mostrar atalhos\n\n" +
            "Desative em: Recomeco → Debug → Desativar dinheiro de teste",
            "OK");
    }

    [MenuItem("Recomeco/Debug/Desativar dinheiro de teste (volta R$ 4,20)")]
    static void DisableTestMoney()
    {
        var settings = LoadSettings();
        if (settings == null)
            return;

        settings.useTestStartingMoney = false;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Modo teste",
            "Dinheiro inicial voltou ao normal (R$ 4,20).\n\n" +
            "Os atalhos F9–F12 no Play continuam disponíveis no Editor.",
            "OK");
    }

    [MenuItem("Recomeco/Debug/Selecionar RecomecoGameplaySettings")]
    static void SelectSettings()
    {
        var settings = LoadSettings();
        if (settings != null)
            Selection.activeObject = settings;
    }

    static RecomecoGameplaySettings LoadSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(SettingsPath);
        if (settings != null)
            return settings;

        EditorUtility.DisplayDialog("Recomeco", "Não encontrei:\n" + SettingsPath, "OK");
        return null;
    }
}
#endif
