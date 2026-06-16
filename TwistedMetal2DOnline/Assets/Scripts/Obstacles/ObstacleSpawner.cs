using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnityLoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class ObstacleSpawner : MonoBehaviourPunCallbacks
{
    private const string RoomRoundIdPropertyKey = "tgRoundId";
    private const string ObstaclesSpawnedRoundPropertyKey = "obstaclesSpawnedRound";

    [Header("Prefab")]
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Spawn")]
    [SerializeField] private int obstacleCount = 5;
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 0f, 20f);
    [SerializeField] private float spawnY = 0f;

    [Header("Control")]
    [SerializeField] private float retryDelay = 0.25f;
    [SerializeField] private int maxRetries = 20;

    private Coroutine spawnRoutine;
    private bool alreadySpawnedLocally = false;

    private new void OnEnable()
    {
        base.OnEnable();
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    private new void OnDisable()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDisable();

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void Awake()
    {
        RegisterPrefabWithPhoton();
    }

    private void Start()
    {
        RegisterPrefabWithPhoton();
        TryStartSpawnRoutine();
    }

    private void RegisterPrefabWithPhoton()
    {
        if (obstaclePrefab != null)
        {
            if (PhotonNetwork.PrefabPool is DefaultPool defaultPool)
            {
                if (!defaultPool.ResourceCache.ContainsKey(obstaclePrefab.name))
                {
                    defaultPool.ResourceCache.Add(obstaclePrefab.name, obstaclePrefab);
                    Debug.Log($"[ObstacleSpawner] Prefab '{obstaclePrefab.name}' registrado exitosamente en el ResourceCache de Photon.");
                }
            }
        }
    }

    public override void OnJoinedRoom()
    {
        TryStartSpawnRoutine();
    }

    private void OnSceneLoaded(UnityScene scene, UnityLoadSceneMode mode)
    {
        TryStartSpawnRoutine();
    }

    private void TryStartSpawnRoutine()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(WaitAndSpawnObstacles());
    }

    private IEnumerator WaitAndSpawnObstacles()
    {
        int retries = 0;

        while (retries < maxRetries)
        {
            retries++;

            if (CanTrySpawn())
            {
                int roundId = GetCurrentRoundId();

                if (!alreadySpawnedLocally && !WereObstaclesAlreadySpawned(roundId))
                {
                    alreadySpawnedLocally = true;
                    MarkObstaclesAsSpawned(roundId);
                    SpawnObstacles();
                }

                spawnRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(retryDelay);
        }

        spawnRoutine = null;
    }

    private bool CanTrySpawn()
    {
        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        if (obstaclePrefab == null)
        {
            Debug.LogError("[ObstacleSpawner] Falta asignar obstaclePrefab.");
            return false;
        }

        return true;
    }

    private void SpawnObstacles()
    {
        RegisterPrefabWithPhoton();
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();

            PhotonNetwork.InstantiateRoomObject(
                obstaclePrefab.name,
                spawnPosition,
                Quaternion.identity
            );
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(
            spawnAreaCenter.x - spawnAreaSize.x * 0.5f,
            spawnAreaCenter.x + spawnAreaSize.x * 0.5f
        );

        float randomZ = Random.Range(
            spawnAreaCenter.z - spawnAreaSize.z * 0.5f,
            spawnAreaCenter.z + spawnAreaSize.z * 0.5f
        );

        return new Vector3(randomX, spawnY, randomZ);
    }

    private int GetCurrentRoundId()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return 0;
        }

        if (TryReadIntProperty(RoomRoundIdPropertyKey, out int roundId))
        {
            return roundId;
        }

        return 0;
    }

    private bool WereObstaclesAlreadySpawned(int roundId)
    {
        if (TryReadIntProperty(ObstaclesSpawnedRoundPropertyKey, out int spawnedRoundId))
        {
            return spawnedRoundId == roundId;
        }

        return false;
    }

    private void MarkObstaclesAsSpawned(int roundId)
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        PhotonHashtable properties = new PhotonHashtable
        {
            { ObstaclesSpawnedRoundPropertyKey, roundId }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private bool TryReadIntProperty(string key, out int value)
    {
        value = 0;

        if (PhotonNetwork.CurrentRoom == null)
        {
            return false;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out object rawValue))
        {
            return false;
        }

        if (rawValue is int intValue)
        {
            value = intValue;
            return true;
        }

        if (rawValue is byte byteValue)
        {
            value = byteValue;
            return true;
        }

        if (rawValue is short shortValue)
        {
            value = shortValue;
            return true;
        }

        return false;
    }
}
