using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

public class MapPartitionLoader : MonoBehaviourPunCallbacks
{
    private const string MAP_SEED_KEY = "map_seed";

    [Header("Referencias de Spawn")]
    [Tooltip("El PlayerSpawner de la escena. LiveOps sobreescribirá sus posiciones de spawn.")]
    [SerializeField] private PlayerSpawner playerSpawner;

    [Tooltip("(Alternativo) Transforms directos si no se usa PlayerSpawner. Deben estar en el mismo orden que spawnPoints en el JSON.")]
    [SerializeField] private Transform[] spawnPointTransforms;

    [Header("Debug")]
    [Tooltip("Si es true, dibuja gizmos con los spawn points en el editor.")]
    [SerializeField] private bool debugDrawSpawns = true;

    private Dictionary<string, ObstacleZoneMarker> zoneCache;
    private bool mapRandomized = false;

    private void Start()
    {
        BuildZoneCache();
        if (LiveOpsManager.Instance != null && LiveOpsManager.Instance.IsReady)
        {
            InitializeMap();
        }
        else if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady += OnLiveOpsReadyHandler;
        }
        else
        {
            InitializeMap();
        }
    }

    private void OnLiveOpsReadyHandler()
    {
        if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady -= OnLiveOpsReadyHandler;
        }
        InitializeMap();
    }

    private void InitializeMap()
    {
        ApplySpawnAndPickups();
        TryApplyRandomMap();
    }

    private void OnDestroy()
    {
        if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady -= OnLiveOpsReadyHandler;
        }
    }

    private void TryApplyRandomMap()
    {
        if (LiveOpsManager.Instance == null || LiveOpsManager.Instance.MapConfig == null)
        {
            Debug.Log("[MapPartitionLoader] Sin LiveOps/MapConfig. Mapa por defecto.");
            return;
        }

        MapPartitionConfig cfg = LiveOpsManager.Instance.MapConfig;

        if (cfg.randomSeed >= 0)
        {
            Debug.Log($"[MapPartitionLoader] Usando seed fijo del Remote Config: {cfg.randomSeed}");
            RandomizeObstacleZones(cfg.randomSeed, cfg.activeRatio, cfg);
            return;
        }

        if (!PhotonNetwork.IsConnected || PhotonNetwork.CurrentRoom == null)
        {
            int offlineSeed = Random.Range(0, int.MaxValue);
            Debug.Log($"[MapPartitionLoader] Sin Photon. Seed offline: {offlineSeed}");
            RandomizeObstacleZones(offlineSeed, cfg.activeRatio, cfg);
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(MAP_SEED_KEY, out object existingSeed))
        {
            int seed = (int)existingSeed;
            Debug.Log($"[MapPartitionLoader] Seed leído de Room Properties: {seed}");
            RandomizeObstacleZones(seed, cfg.activeRatio, cfg);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            int newSeed = Random.Range(0, int.MaxValue);
            Hashtable props = new Hashtable { { MAP_SEED_KEY, newSeed } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"[MapPartitionLoader] Master Client publicó seed: {newSeed}");
        }
        else
        {
            Debug.Log("[MapPartitionLoader] Esperando seed del Master Client...");
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (mapRandomized) return;
        if (!propertiesThatChanged.ContainsKey(MAP_SEED_KEY)) return;
        if (LiveOpsManager.Instance == null || LiveOpsManager.Instance.MapConfig == null) return;

        int seed = (int)propertiesThatChanged[MAP_SEED_KEY];
        MapPartitionConfig cfg = LiveOpsManager.Instance.MapConfig;

        Debug.Log($"[MapPartitionLoader] OnRoomPropertiesUpdate → seed: {seed}");
        RandomizeObstacleZones(seed, cfg.activeRatio, cfg);
    }

    private void RandomizeObstacleZones(int seed, float activeRatio, MapPartitionConfig cfg)
    {
        if (mapRandomized) return;
        mapRandomized = true;

        if (cfg.obstacleZones == null || cfg.obstacleZones.Length == 0)
        {
            Debug.LogWarning("[MapPartitionLoader] No hay obstacleZones en la config. Nada que randomizar.");
            return;
        }

        System.Random rng = new System.Random(seed);

        int activatedCount = 0;
        int totalCount = cfg.obstacleZones.Length;

        foreach (ObstacleZoneData zoneData in cfg.obstacleZones)
        {
            if (!zoneCache.TryGetValue(zoneData.zoneId, out ObstacleZoneMarker marker))
            {
                Debug.LogWarning($"[MapPartitionLoader] Zona '{zoneData.zoneId}' no encontrada en la escena.");
                continue;
            }

            bool isActive = rng.NextDouble() < activeRatio;
            marker.gameObject.SetActive(isActive);

            if (isActive) activatedCount++;
        }

        Debug.Log($"[MapPartitionLoader] Mapa randomizado (seed:{seed}) — " +
                  $"{activatedCount}/{totalCount} buildings activos ({activeRatio * 100:F0}% ratio).");
    }

    private void ApplySpawnAndPickups()
    {
        if (LiveOpsManager.Instance == null)
        {
            Debug.Log("[MapPartitionLoader] No se encontró LiveOpsManager. Usando mapa por defecto.");
            return;
        }

        MapPartitionConfig mapConfig = LiveOpsManager.Instance.MapConfig;

        if (mapConfig == null)
        {
            Debug.Log("[MapPartitionLoader] Sin partición de mapa configurada remotamente. Usando mapa por defecto.");
            return;
        }

        ApplySpawnPoints(mapConfig);
        ApplyPickupZones(mapConfig);
    }

    private void ApplySpawnPoints(MapPartitionConfig config)
    {
        if (config.spawnPoints == null || config.spawnPoints.Length == 0)
            return;

        if (playerSpawner != null)
        {
            Vector3[] positions = new Vector3[config.spawnPoints.Length];
            for (int i = 0; i < config.spawnPoints.Length; i++)
                positions[i] = config.spawnPoints[i].AsVector3();

            playerSpawner.SetSpawnPositionsFromLiveOps(positions);
            Debug.Log($"[MapPartitionLoader] {positions.Length} spawn point(s) aplicados via PlayerSpawner.");
            return;
        }

        if (spawnPointTransforms == null || spawnPointTransforms.Length == 0)
        {
            Debug.LogWarning("[MapPartitionLoader] No hay PlayerSpawner ni SpawnPointTransforms asignados.");
            return;
        }

        int count = Mathf.Min(spawnPointTransforms.Length, config.spawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (spawnPointTransforms[i] == null) continue;
            SpawnPointData data = config.spawnPoints[i];
            spawnPointTransforms[i].position = data.AsVector3();
            spawnPointTransforms[i].rotation = Quaternion.Euler(0f, 0f, data.rotation);
            Debug.Log($"[MapPartitionLoader] Spawn[{i}] → ({data.x}, {data.y}) rot:{data.rotation}°");
        }
    }

    private void ApplyPickupZones(MapPartitionConfig config)
    {
        if (config.pickupZones == null)
            return;

        foreach (PickupZoneData zoneData in config.pickupZones)
        {
            if (!zoneCache.TryGetValue(zoneData.zoneId, out ObstacleZoneMarker marker))
            {
                Debug.LogWarning($"[MapPartitionLoader] Zona de pickup '{zoneData.zoneId}' no encontrada en la escena.");
                continue;
            }

            marker.gameObject.SetActive(zoneData.active);

            if (zoneData.active && zoneData.spawnRate > 0f)
            {
                PickUpFloat pickUp = marker.GetComponentInChildren<PickUpFloat>(includeInactive: true);
                if (pickUp != null)
                {
                    pickUp.SetSpawnInterval(zoneData.spawnRate);
                    Debug.Log($"[MapPartitionLoader] Pickup '{zoneData.zoneId}' → spawnRate:{zoneData.spawnRate}s");
                }
            }
        }
    }

    private void BuildZoneCache()
    {
        zoneCache = new Dictionary<string, ObstacleZoneMarker>();
        ObstacleZoneMarker[] allMarkers = FindObjectsOfType<ObstacleZoneMarker>(includeInactive: true);

        foreach (ObstacleZoneMarker marker in allMarkers)
        {
            if (string.IsNullOrEmpty(marker.ZoneId))
                continue;

            if (zoneCache.ContainsKey(marker.ZoneId))
            {
                Debug.LogWarning($"[MapPartitionLoader] Zone ID duplicado: '{marker.ZoneId}'. " +
                                 $"Se usará el primer encontrado.", marker);
                continue;
            }

            zoneCache[marker.ZoneId] = marker;
        }

        Debug.Log($"[MapPartitionLoader] {zoneCache.Count} zona(s) descubiertas en la escena.");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!debugDrawSpawns || spawnPointTransforms == null)
            return;

        Gizmos.color = Color.green;
        foreach (Transform sp in spawnPointTransforms)
        {
            if (sp == null) continue;
            Gizmos.DrawWireSphere(sp.position, 0.4f);
            Gizmos.DrawLine(sp.position, sp.position + sp.right * 0.8f);
        }
    }
#endif
}
