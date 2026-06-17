#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class MainMenuGameViewFix
{
    const string MenuPath = "Recomeco/Cenas/Corrigir zoom da Game View (1x)";

    static MainMenuGameViewFix()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        EditorApplication.delayCall += () => SetAllGameViewScales(1f);
        EditorApplication.delayCall += () => SetAllGameViewScales(1f);
    }

    [MenuItem(MenuPath)]
    static void FixGameViewScaleMenu()
    {
        SetAllGameViewScales(1f);
        EditorUtility.DisplayDialog("Recomeco",
            "Zoom da Game View ajustado para 1x.\n\n" +
            "Dicas se voltar a acontecer:\n" +
            "• Aspect → 1920×1080 (não Free Aspect)\n" +
            "• Desmarque \"Low Resolution Aspect Ratios\" no menu Aspect\n" +
            "• Feche e reabra a aba Game",
            "OK");
    }

    static void SetAllGameViewScales(float scale)
    {
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
            return;

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var zoomField = gameViewType.GetField("m_ZoomArea", flags);
        if (zoomField == null)
            return;

        foreach (var window in Resources.FindObjectsOfTypeAll(gameViewType))
        {
            if (window is not EditorWindow)
                continue;

            var editorWindow = (EditorWindow)window;
            var zoomArea = zoomField.GetValue(editorWindow);
            if (zoomArea == null)
                continue;

            var scaleField = zoomArea.GetType().GetField("m_Scale", flags);
            if (scaleField == null)
                continue;

            scaleField.SetValue(zoomArea, new Vector2(scale, scale));
            editorWindow.Repaint();
        }
    }
}
#endif
