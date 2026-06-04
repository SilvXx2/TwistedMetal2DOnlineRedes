using UnityEngine;

internal readonly struct AlivePlayersSnapshot
{
    public int AliveCount { get; }
    public PlayerHealth LastAlive { get; }

    public AlivePlayersSnapshot(int aliveCount, PlayerHealth lastAlive)
    {
        AliveCount = aliveCount;
        LastAlive = lastAlive;
    }
}

internal static class PlayerAliveStateEvaluator
{
    public static AlivePlayersSnapshot Capture()
    {
        PlayerHealth[] players = Object.FindObjectsOfType<PlayerHealth>();

        int aliveCount = 0;
        PlayerHealth lastAlive = null;

        for (int i = 0; i < players.Length; i++)
        {
            PlayerHealth player = players[i];
            if (!player.IsDead)
            {
                aliveCount++;
                lastAlive = player;
            }
        }

        return new AlivePlayersSnapshot(aliveCount, lastAlive);
    }
}
