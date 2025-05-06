using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LeaderboardManager : MonoBehaviour
{
    public TextMeshProUGUI leaderboardText;
    public bool isFinalLeaderboard = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(ShowLeaderboardDelayed());
    }

    private IEnumerator ShowLeaderboardDelayed()
    {
        yield return new WaitForSeconds(1f);

        if (leaderboardText == null || LeaderboardCache.scores == null)
        {
            leaderboardText.text = "Leaderboard data not found.";
            yield break;
        }

        leaderboardText.text = "";

        foreach (var entry in LeaderboardCache.scores)
        {
            string playerName = entry.Key;
            int points = entry.Value;

            float time = LeaderboardCache.times.ContainsKey(playerName)
                ? LeaderboardCache.times[playerName]
                : 0f;

            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt((time * 100f) % 100f);

            leaderboardText.text += $"{playerName}: {points} pts - {minutes:00}:{seconds:00}.{milliseconds:00}\n";
        }
    }



    public void ContinueButton()
    {
        if (isFinalLeaderboard)
        {
            Debug.Log("Tournament complete!");
            Application.Quit(); // Or just stop the game
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #endif
        }
        else
        {
            Debug.Log("Loading next race...");
            GameManager.Instance.LoadNextRace();
        }
    }

}
