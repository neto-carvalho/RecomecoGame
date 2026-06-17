using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayReturnToMenu
{
    public static void ResetPersistentGameplayState()
    {
        GameplayHudBootstrap.ResetForMenu();
        GameSession.Reset();

        if (MoneyManager.instance != null)
            Object.Destroy(MoneyManager.instance.gameObject);
    }

    public static void GoToMainMenu()
    {
        Time.timeScale = 1f;

        GameplayPauseMenu.ForceCloseIfOpen();
        SellMinigameUI.ForceCloseIfOpen();

        PlayerScenePersistence.ResetForMenuGameplayStart();
        ResetPersistentGameplayState();

        if (!Application.CanStreamedLevelBeLoaded(RecomecoSceneNames.MenuInicial))
        {
            Debug.LogError(
                "GameplayReturnToMenu: cena '" + RecomecoSceneNames.MenuInicial +
                "' não está em File → Build Settings.");
            return;
        }

        SceneManager.LoadScene(RecomecoSceneNames.MenuInicial);
    }
}
