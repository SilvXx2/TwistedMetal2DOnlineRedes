using UnityEngine;
using Photon.Pun;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnityLoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3[] spawnPositions;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float retrySpawnInterval = 0.5f;

    private bool hasSpawned = false;
    private float nextSpawnRetryTime = 0f;

    private void OnEnable()
    {
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (!spawnOnStart)
        {
            return;
        }

        TrySpawnPlayer();
    }

    private void Update()
    {
        if (!spawnOnStart || hasSpawned || !PhotonNetwork.InRoom)
        {
            return;
        }

        if (Time.unscaledTime < nextSpawnRetryTime)
        {
            return;
        }

        nextSpawnRetryTime = Time.unscaledTime + retrySpawnInterval;
        TrySpawnPlayer();
    }

    public override void OnJoinedRoom()
    {
        hasSpawned = false;
        nextSpawnRetryTime = 0f;

        if (!spawnOnStart)
        {
            return;
        }

        TrySpawnPlayer();
    }

    public override void OnLeftRoom()
    {
        hasSpawned = false;
        nextSpawnRetryTime = 0f;
    }

    private void OnSceneLoaded(UnityScene scene, UnityLoadSceneMode mode)
    {
        hasSpawned = false;
        nextSpawnRetryTime = 0f;

        if (!spawnOnStart) return;
        TrySpawnPlayer();
    }

    public void SpawnPlayer()
    {
        hasSpawned = false;
        nextSpawnRetryTime = 0f;
        TrySpawnPlayer();
    }

    private void TrySpawnPlayer()
    {
        if (hasSpawned)
        {
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner sin playerPrefab asignado.");
            return;
        }

        if (HasLocalPlayerInstance())
        {
            hasSpawned = true;
            return;
        }

        PhotonPlayerSlotRegistry.EnsureLocalStableIdentity();

        if (PhotonNetwork.IsMasterClient)
        {
            int preferredSlotCount = (spawnPositions != null && spawnPositions.Length > 0)
                ? spawnPositions.Length
                : 0;

            PhotonPlayerSlotRegistry.EnsureSlotsAssignedForCurrentPlayers(preferredSlotCount);
        }

        PhotonPlayerSlotRegistry.TryApplyLocalPlayerSlotProperty();

        if (!PhotonPlayerSlotRegistry.TryGetLocalStableSlot(out int slotIndex))
        {
            return;
        }

        Vector3 spawnPosition = (spawnPositions != null && spawnPositions.Length > 0)
            ? spawnPositions[slotIndex % spawnPositions.Length]
            : transform.position;

        PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        hasSpawned = true;
    }

    private bool HasLocalPlayerInstance()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>();
        for (int i = 0; i < views.Length; i++)
        {
            PhotonView view = views[i];
            if (view == null || !view.IsMine)
            {
                continue;
            }

            if (view.gameObject.name.StartsWith(playerPrefab.name))
            {
                return true;
            }
        }

        return false;
    }
}