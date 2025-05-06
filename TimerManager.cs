using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    private float timer = 0f;
    private bool timerRunning = false;

    private Dictionary<string, float> playerTimes = new Dictionary<string, float>();

    void Start()
    {
        timer = 0f;
        timerRunning = false;

        // Find timer text in HUD
        //timerText = GameObject.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
        timerText = FindTimerText();
    }

    void Update()
    {
        if (timerRunning)
        {
            timer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            int milliseconds = Mathf.FloorToInt((timer * 100f) % 100f);
            timerText.text = $"{minutes:00}:{seconds:00}.{milliseconds:00}";
        }
    }

    public void StartTimer()
    {
        timer = 0f;
        timerRunning = true;
    }

    public void StopTimer(string playerName)
    {
        if (!GameManager.Instance.playerFinishTimes.ContainsKey(playerName))
        {
            GameManager.Instance.playerFinishTimes[playerName] = timer;
        }
    }

    public float GetPlayerTime(string playerName)
    {
        if (GameManager.Instance.playerFinishTimes.ContainsKey(playerName))
            return GameManager.Instance.playerFinishTimes[playerName];
        return 0f;
    }

    private TextMeshProUGUI FindTimerText()
    {
        GameObject found = GameObject.Find("TimerText");
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }
}
