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
        
        if (playerPrefab != null)
        {
            PhotonView[] allViews = FindObjectsOfType<PhotonView>(true);
            for (int i = 0; i < allViews.Length; i++)
            {
                PhotonView view = allViews[i];
                if (view != null && !view.isRuntimeInstantiated && view.gameObject.name.StartsWith(playerPrefab.name))
                {
                    Debug.Log($"[PlayerSpawner] Destruyendo coche de escena estático para evitar conflictos: {view.gameObject.name}");
                    Destroy(view.gameObject);
                }
            }
        }

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

    
    
    
    
    public void SetSpawnPositionsFromLiveOps(Vector3[] positions)
    {
        if (positions == null || positions.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawner] SetSpawnPositionsFromLiveOps: array vacío, ignorado.");
            return;
        }

        spawnPositions = positions;
        Debug.Log($"[PlayerSpawner] LiveOps: {positions.Length} spawn position(s) actualizadas.");
    }


    private void TrySpawnPlayer()
    {
        Debug.Log($"[PlayerSpawner] TrySpawnPlayer entered. hasSpawned={hasSpawned}, InRoom={PhotonNetwork.InRoom}, localPlayer.ActorNumber={PhotonNetwork.LocalPlayer?.ActorNumber}");

        if (GameManager.Instance != null && GameManager.Instance.IsMatchEnded)
        {
            Debug.Log("[PlayerSpawner] TrySpawnPlayer omitido: la partida ya ha terminado.");
            return;
        }

        if (LiveOpsManager.Instance != null && !LiveOpsManager.Instance.IsReady)
        {
            Debug.Log("[PlayerSpawner] TrySpawnPlayer omitido: LiveOpsManager está inicializándose.");
            return;
        }

        if (hasSpawned)
        {
            Debug.Log("[PlayerSpawner] TrySpawnPlayer: already spawned (hasSpawned is true).");
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.Log("[PlayerSpawner] TrySpawnPlayer omitido: no estamos en una room de Photon.");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner sin playerPrefab asignado.");
            return;
        }

        if (HasLocalPlayerInstance())
        {
            Debug.Log("[PlayerSpawner] TrySpawnPlayer: Ya existe una instancia local del jugador en escena. Marcando hasSpawned = true.");
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
            Debug.Log("[PlayerSpawner] TrySpawnPlayer: Aún no se ha asignado o sincronizado el slot del jugador estable.");
            return;
        }

        Vector3 spawnPosition = (spawnPositions != null && spawnPositions.Length > 0)
            ? spawnPositions[slotIndex % spawnPositions.Length]
            : transform.position;

        Debug.Log($"[PlayerSpawner] Instanciando jugador local '{playerPrefab.name}' en la posición de spawn [{slotIndex}] (mod {spawnPositions?.Length ?? 0}): {spawnPosition}");
        
        GameObject spawned = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        if (spawned == null)
        {
            Debug.LogError($"[PlayerSpawner] PhotonNetwork.Instantiate falló al retornar GameObject para {playerPrefab.name}!");
        }
        else
        {
            Debug.Log($"[PlayerSpawner] PhotonNetwork.Instantiate exitoso. Nombre={spawned.name}, Activo={spawned.activeSelf}, Layer={spawned.layer}");
        }
        
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

            
            if (!view.isRuntimeInstantiated || view.InstantiationId == 0)
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