using UnityEngine;
using Mirror;
using System.Linq;

public class FinishLineTrigger : NetworkBehaviour
{
    private RaceManager raceManager;

    private void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ensure only the server processes this
        if (!isServer) return;

        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || player.hasFinished) return;

        // Save timer
        TimerManager timerManager = FindObjectOfType<TimerManager>();
        if (timerManager != null)
        {
            timerManager.StopTimer(player.playerName);
        }

        player.FreezePlayer();

        // Award points (optional: move to RaceManager or GameManager logic)
        int placement = GetPlacement(); // simple local function
        int points = GetPointsForPlacement(placement);
        GameManager.Instance.AwardPoints(player.playerName, points);

        // Show win text on client
        player.TargetShowWinText(player.connectionToClient, placement);


        // Register as finished
        raceManager.PlayerFinished(player);

        player.TargetFreezeOnFinish(player.connectionToClient); // ❄ Freeze them visually

    }

    private int GetPlacement()
    {
        int finished = FindObjectsOfType<PlayerController>().Count(p => p.hasFinished);
        return finished + 1; // placement is 1-based
    }

    private int GetPointsForPlacement(int place)
    {
        switch (place)
        {
            case 1: return 3;
            case 2: return 2;
            case 3: return 1;
            default: return 0;
        }
    }
}
