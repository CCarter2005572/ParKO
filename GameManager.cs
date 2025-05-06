using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Dictionary<string, int> playerScores = new Dictionary<string, int>();
    public Dictionary<string, float> playerFinishTimes = new Dictionary<string, float>();

    public int currentRace = 1; // 1 = Level1, 2 = Level2, 3 = Level3

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AwardPoints(string playerName, int points)
    {
        if (!playerScores.ContainsKey(playerName))
            playerScores[playerName] = 0;

        playerScores[playerName] += points;
    }

    public void SaveFinishTime(string playerName, float time)
    {
        if (!playerFinishTimes.ContainsKey(playerName))
            playerFinishTimes[playerName] = time;
    }

    public void NextLevel()
    {
        // Cache scores and times
        LeaderboardCache.Clear();
        foreach (var kvp in playerScores)
        {
            LeaderboardCache.scores[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in playerFinishTimes)
        {
            LeaderboardCache.times[kvp.Key] = kvp.Value;
        }

        if (currentRace < 3)
            SceneManager.LoadScene("Leaderboard");
        else
            SceneManager.LoadScene("FinalLeaderboard");
    }


    public void LoadNextRace()
    {
        currentRace++;

        if (currentRace == 2)
        {
            NetworkManager.singleton.ServerChangeScene("Level2");
        }
        else if (currentRace == 3)
        {
            NetworkManager.singleton.ServerChangeScene("Level3");
        }
        else
        {
            NetworkManager.singleton.ServerChangeScene("MainMenu"); // Optional fallback
        }
    }

    public void ResetGame()
    {
        playerScores.Clear();
        playerFinishTimes.Clear();
        currentRace = 1;
    }
}
