using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RaceManager : NetworkBehaviour
{
    private List<PlayerController> players = new List<PlayerController>();
    private int playersFinished = 0;
    private int playersDead = 0;
    private bool raceEnded = false;

    [ServerCallback]
    void Start()
    {
        StartCoroutine(WaitAndRegisterPlayers());
    }

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"[RaceManager] Registered player: {player.playerName} (total: {players.Count})");
        }
    }


    [Server]
    private IEnumerator WaitAndRegisterPlayers()
    {
        int attempts = 0;
        while (players.Count < NetworkServer.connections.Count && attempts < 10)
        {
            yield return new WaitForSeconds(1f);
            players.Clear();
            players.AddRange(FindObjectsOfType<PlayerController>());
            Debug.Log($"[RaceManager] Attempt {attempts + 1}: Found {players.Count} players, expected {NetworkServer.connections.Count}.");
            attempts++;
        }

        Debug.Log($"[RaceManager] Final Registered Players: {players.Count}");
    }

    [Server]
    public void PlayerFinished(PlayerController player)
    {
        if (raceEnded || player.hasFinished) return;

        player.hasFinished = true;
        playersFinished++;

        Debug.Log($"[RaceManager] {player.playerName} finished. Finished: {playersFinished}, Dead: {playersDead}");
        CheckRaceEnd();
    }

    [Server]
    public void PlayerDied(PlayerController player)
    {
        if (raceEnded || player.hasFinished) return;

        player.hasFinished = true;
        playersDead++;

        Debug.Log($"[RaceManager] {player.playerName} died. Finished: {playersFinished}, Dead: {playersDead}");
        CheckRaceEnd();
    }

    [Server]
    private void CheckRaceEnd()
    {
        int totalPlayers = players.Count;
        if (playersFinished + playersDead >= totalPlayers)
        {
            raceEnded = true;
            Debug.Log("[RaceManager] All players finished or dead — advancing to leaderboard.");
            Invoke(nameof(AdvanceLevel), 3f);
        }
    }

    [Server]
    private void AdvanceLevel()
    {
        // Cache data for client Leaderboard access
        LeaderboardCache.scores = new Dictionary<string, int>(GameManager.Instance.playerScores);
        LeaderboardCache.times = new Dictionary<string, float>(GameManager.Instance.playerFinishTimes);

        GameManager.Instance.NextLevel(); // This should use ServerChangeScene internally
    }
}
