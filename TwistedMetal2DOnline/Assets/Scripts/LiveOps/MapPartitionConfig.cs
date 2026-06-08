using System;

[Serializable]
public class MapPartitionConfig
{
    public string partitionId;
    public string displayName;
    public SpawnPointData[] spawnPoints;
    public ObstacleZoneData[] obstacleZones;
    public PickupZoneData[] pickupZones;
    public int randomSeed;
    public float activeRatio;

    public static MapPartitionConfig TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            MapPartitionConfig cfg = UnityEngine.JsonUtility.FromJson<MapPartitionConfig>(json);

            if (cfg.activeRatio <= 0f)
                cfg.activeRatio = 0.5f;

            if (cfg.randomSeed == 0 && !json.Contains("randomSeed"))
                cfg.randomSeed = -1;

            return cfg;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[MapPartitionConfig] Error al parsear JSON: {e.Message}");
            return null;
        }
    }
}

[Serializable]
public struct SpawnPointData
{
    public float x;
    public float y;
    public float rotation;

    public UnityEngine.Vector2 AsVector2() => new UnityEngine.Vector2(x, y);
    public UnityEngine.Vector3 AsVector3() => new UnityEngine.Vector3(x, y, 0f);
}

[Serializable]
public struct ObstacleZoneData
{
    public string zoneId;
    public bool active;
}

[Serializable]
public struct PickupZoneData
{
    public string zoneId;
    public bool active;
    public float spawnRate;
}
