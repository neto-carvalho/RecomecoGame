#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterGroundSnapMenu
{
    [MenuItem("Recomeco/Personagens/Encostar Player e NPCs ao chão")]
    static void SnapAllCharactersToGround()
    {
        var count = 0;
        foreach (var cc in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
        {
            var snap = cc.GetComponent<CharacterGroundSnap>();
            if (snap != null)
            {
                snap.SnapNow();
                count++;
                continue;
            }

            var isNpc = cc.GetComponent<SidewalkNpcWalker>() != null;
            if (isNpc)
                CharacterGroundSnap.FitControllerToWorldScale(cc, 2f, new Vector3(0f, 1f, 0f), 0.35f, 0.25f, 0.08f);
            else
                CharacterGroundSnap.FitControllerToWorldScale(cc);

            if (CharacterGroundSnap.TrySnap(cc.transform, cc))
                count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Chão",
            $"Ajustados {count} personagem(ns) com CharacterController.\n\nGuarda a cena (Ctrl+S).",
            "OK");
    }
}
#endif
