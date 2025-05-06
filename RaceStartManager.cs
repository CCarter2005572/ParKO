using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using UnityEngine.SceneManagement;

public class RaceStartManager : NetworkBehaviour
{
    public TextMeshProUGUI countdownText;
    public AudioSource countdownBeep;
    public float countdownTime = 5f;
    public Button startRaceButton;

    private float currentCountdown;
    private bool isCountingDown = false;
    private bool shown = false;

    void Start()
    {
        if (!isClient) return;

        countdownText = GameObject.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
        countdownBeep = GameObject.Find("countdownBeep")?.GetComponent<AudioSource>();

        if (startRaceButton == null)
        {
            GameObject foundButton = GameObject.Find("StartButton");
            if (foundButton != null)
            {
                startRaceButton = foundButton.GetComponent<Button>();
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene != "Level1" && startRaceButton != null)
        {
            startRaceButton.gameObject.SetActive(false);
        }

    }

    void Update()
    {
        if (shown || startRaceButton == null) return;

        // ✅ Only the server (Host) should see the button and trigger countdown
        if (NetworkServer.active && NetworkClient.active)
        {
            startRaceButton.gameObject.SetActive(true);
            startRaceButton.onClick.RemoveAllListeners();
            startRaceButton.onClick.AddListener(OnStartButtonPressed);
            Debug.Log("StartButton listener added on host.");
            shown = true;
        }
    }


    void OnStartButtonPressed()
    {
        LockAndHideCursor(); // local lock

        if (isServer)
        {
            StartCountdown(); // Only host triggers countdown
        }
    }



    public void StartCountdown()
    {
        Debug.Log("StartCountdown triggered.");

        if (NetworkServer.active && !isCountingDown)
        {
            RpcHideStartButton(); // hide button for everyone
            RpcHideCursor(); // hide cursor for everyone
            StartCoroutine(CountdownRoutine());
        }

        if (startRaceButton != null)
        {
            startRaceButton.gameObject.SetActive(false); // hide for local player
        }
    }

    [ClientRpc]
    private void RpcHideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private IEnumerator CountdownRoutine()
    {
        isCountingDown = true;
        currentCountdown = countdownTime;

        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            player.FreezePlayer();
        }

        while (currentCountdown > 0)
        {
            RpcUpdateCountdown(Mathf.CeilToInt(currentCountdown).ToString());

            if (countdownBeep != null)
                countdownBeep.Play();

            yield return new WaitForSeconds(1f);
            currentCountdown--;
        }

        RpcUpdateCountdown("GO!");
        yield return new WaitForSeconds(0.5f);
        RpcHideCountdown();

        // Unfreeze players via TargetRpc
        foreach (var player in players)
        {
            player.TargetUnfreeze(player.connectionToClient);
        }

        // Start race timer
        TimerManager timerManager = FindObjectOfType<TimerManager>();
        if (timerManager != null)
        {
            timerManager.StartTimer();
        }

        isCountingDown = false;
    }

    [ClientRpc]
    private void RpcUpdateCountdown(string text)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = text;
        }
    }

    [ClientRpc]
    private void RpcHideCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void RpcHideStartButton()
    {
        Debug.Log("RpcHideStartButton() called");

        if (startRaceButton != null)
        {
            startRaceButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("startRaceButton is NULL on this client.");
        }
    }



    public static void LockAndHideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
