// Merged from: MainMenuManager.cs, LevelMenu.cs

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResetGame();

        SceneManager.LoadScene("Level1");
    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level " + levelId;
        SceneManager.LoadScene(levelName);
    }

    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        Application.Quit();
    }
}
