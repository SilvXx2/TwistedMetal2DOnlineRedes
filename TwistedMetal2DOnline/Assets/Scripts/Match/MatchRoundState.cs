internal sealed class MatchRoundState
{
    public bool GameEnded { get; private set; }
    public bool MatchStartedProperly { get; private set; }
    public bool ResultProcessed { get; private set; } = true;
    public int RoundId { get; private set; }
    public float SceneLoadTime { get; private set; }

    public void OnSceneLoaded(float sceneLoadTime)
    {
        RoundId++;
        SceneLoadTime = sceneLoadTime;
        GameEnded = false;
        MatchStartedProperly = false;
        ResultProcessed = true;
    }

    public void ResetForRoomJoin(float sceneLoadTime)
    {
        SceneLoadTime = sceneLoadTime;
        GameEnded = false;
        MatchStartedProperly = false;
        ResultProcessed = false;
    }

    public bool CanCheckMatch(float currentTime, float gracePeriod)
    {
        return !GameEnded && currentTime - SceneLoadTime >= gracePeriod;
    }

    public bool TryBeginResultResolution(int aliveCount, bool hasLastAlive)
    {
        if (aliveCount > 1)
        {
            MatchStartedProperly = true;
        }

        if (MatchStartedProperly && aliveCount == 1 && hasLastAlive)
        {
            GameEnded = true;
            return true;
        }

        return false;
    }

    public void MarkResultProcessed()
    {
        ResultProcessed = true;
    }

    public void MarkResultPending()
    {
        ResultProcessed = false;
    }

    public void MarkGameEnded()
    {
        GameEnded = true;
    }

    public void SetRoundId(int roundId)
    {
        if (roundId < 0)
        {
            return;
        }

        RoundId = roundId;
    }
}
