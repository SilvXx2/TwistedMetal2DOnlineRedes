[System.Serializable]
public struct GameConfig
{
    public int PlayerMaxHealth;
    public float CarMoveSpeed;
    public float CarTurnSpeed;
    public float PickupSpawnInterval;
    public float MatchGracePeriod;
    public bool EventModeActive;
    public string EventModeName;
    public string ActiveMapPartitionJson;

    public static GameConfig Default => new GameConfig
    {
        PlayerMaxHealth       = 100,
        CarMoveSpeed          = 12f,
        CarTurnSpeed          = 240f,
        PickupSpawnInterval   = 5f,
        MatchGracePeriod      = 2.5f,
        EventModeActive       = false,
        EventModeName         = string.Empty,
        ActiveMapPartitionJson = string.Empty
    };
}
