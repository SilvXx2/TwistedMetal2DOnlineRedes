using System;
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnityLoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static GameManager Instance { get; private set; }

    private const byte MatchResultEventCode = 20;
    private const byte ForceRestartEventCode = 21;
    private const byte TauntEventCode = 22;
    private const string RoomRoundIdPropertyKey = "tgRoundId";
    private const string RoomMatchEndedPropertyKey = "tgMatchEnded";

    [Header("Configuracion de resultado")]
    [SerializeField] private float endGameDelay = 2f;

    [SerializeField] private float matchCheckGracePeriod = 2.5f;
    [SerializeField] private string fallbackGameplaySceneName = "SampleScene";

    private MatchRoundState matchState = new MatchRoundState();
    private PhotonMatchResultEventBridge matchResultEventBridge;
    private PhotonTauntEventBridge tauntEventBridge;
    private Coroutine pendingResultRoutine;
    private bool isLeavingToLobby = false;

    public event Action<bool> LocalMatchResultResolved;
    public event Action MatchStateReset;

    public bool IsMatchEnded => matchState.GameEnded;

    public override void OnEnable()
    {
        base.OnEnable();
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDisable();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        matchResultEventBridge = new PhotonMatchResultEventBridge(MatchResultEventCode);
        tauntEventBridge = new PhotonTauntEventBridge(TauntEventCode);

        
        if (LiveOpsManager.Instance != null && LiveOpsManager.Instance.IsReady)
        {
            ApplyLiveOpsConfig();
        }
        else if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady += ApplyLiveOpsConfig;
        }
    }

    private void ApplyLiveOpsConfig()
    {
        if (LiveOpsManager.Instance != null)
        {
            matchCheckGracePeriod = LiveOpsManager.Instance.Config.MatchGracePeriod;
            Debug.Log($"[GameManager] LiveOps aplicado — matchCheckGracePeriod:{matchCheckGracePeriod}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyLiveOpsConfig;
        }
    }

    private void OnSceneLoaded(UnityScene scene, UnityLoadSceneMode mode)
    {
        isLeavingToLobby = false;
        Debug.Log($"[GameManager] OnSceneLoaded — scene:{scene.name} roundId antes:{matchState.RoundId}");
        StopPendingResultRoutine();
        matchState.OnSceneLoaded(Time.time);
        bool isEnded = false;
        if (PhotonNetwork.InRoom && IsGameplayScene(scene))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                SetRoomRoundId(matchState.RoundId);
                SetRoomMatchEnded(false);
            }
            else
            {
                TrySyncRoundIdFromRoom();
                TrySyncMatchEndedFromRoom();
                isEnded = IsMatchEnded;
            }
        }
        PhotonNetwork.AutomaticallySyncScene = true;
        if (!isEnded)
        {
            StartCoroutine(InvokeMatchStateResetNextFrame());
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case MatchResultEventCode:
                if (matchState.ResultProcessed) return;

                if (!PhotonMatchResultEventBridge.TryUnpack(photonEvent.CustomData, out int eventRoundId, out bool localPlayerWon))
                {
                    return;
                }

                Debug.Log($"[GameManager] OnEvent — eventRoundId:{eventRoundId} currentRoundId:{matchState.RoundId} won:{localPlayerWon}");

                if (eventRoundId != matchState.RoundId)
                {
                    TrySyncRoundIdFromRoom();
                }

                if (eventRoundId != matchState.RoundId)
                {
                    Debug.LogWarning($"[GameManager] RoundId desalineado, sincronizando desde evento. event:{eventRoundId} local:{matchState.RoundId}");
                    matchState.SetRoundId(eventRoundId);
                }

                StartResultResolution(localPlayerWon);
                break;

            case ForceRestartEventCode:
                string sceneName = PhotonMatchResultEventBridge.ExtractSceneName(photonEvent.CustomData);
                Debug.Log($"[GameManager] ForceRestart recibido — scene:{sceneName}");
                if (!string.IsNullOrEmpty(sceneName))
                {
                    PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
                    UnitySceneManager.LoadScene(sceneName);
                }
                break;

            case TauntEventCode:
                if (PhotonTauntEventBridge.TryUnpack(photonEvent.CustomData, out int attackerActor, out int victimActor, out int tauntId))
                {
                    string attackerName = GetPlayerName(attackerActor);
                    string victimName = GetPlayerName(victimActor);

                    TauntList.Instance.ObtenerTauntPorId(tauntId, (tauntText) =>
                    {
                        if (TauntNotifier.Instance != null)
                        {
                            TauntNotifier.Instance.ShowTaunt(attackerName, victimName, tauntText);
                        }
                    });
                }
                break;
        }
    }

    private string GetPlayerName(int actorNumber)
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player != null && !string.IsNullOrEmpty(player.NickName))
            {
                return player.NickName;
            }
        }
        return $"Jugador {actorNumber}";
    }

    public override void OnJoinedRoom()
    {
        isLeavingToLobby = false;
        StopPendingResultRoutine();
        matchState.ResetForRoomJoin(Time.time);
        TrySyncRoundIdFromRoom();
        TrySyncMatchEndedFromRoom();
        if (!IsMatchEnded)
        {
            StartCoroutine(InvokeMatchStateResetNextFrame());
        }
    }

    public override void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null)
        {
            return;
        }

        if (TryReadRoundId(propertiesThatChanged, out int roomRoundId))
        {
            ApplyRoomRoundId(roomRoundId);
        }
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom && matchState.GameEnded && pendingResultRoutine == null && !isLeavingToLobby)
        {
            int playerCount = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
            if (playerCount < 2)
            {
                Debug.Log($"[GameManager] Solo queda {playerCount} jugador en la sala durante la pantalla de resultados. Volviendo al lobby.");
                isLeavingToLobby = true;
                if (PhotonManager.Instance != null)
                {
                    PhotonManager.Instance.LeaveCurrentRoom();
                }
                return;
            }
        }

        if (!matchState.CanCheckMatch(Time.time, matchCheckGracePeriod))
        {
            return;
        }

        AlivePlayersSnapshot snapshot = PlayerAliveStateEvaluator.Capture();
        if (!matchState.TryBeginResultResolution(snapshot.AliveCount, snapshot.LastAlive != null))
        {
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                matchResultEventBridge.BroadcastMatchResult(snapshot.LastAlive.photonView.OwnerActorNr, matchState.RoundId);
                SetRoomMatchEnded(true);
            }

            return;
        }

        StartResultResolution(snapshot.LastAlive.photonView.IsMine);
    }

    private void StartResultResolution(bool localPlayerWon)
    {
        if (matchState.ResultProcessed)
        {
            return;
        }

        StopPendingResultRoutine();
        pendingResultRoutine = StartCoroutine(ResolveResultAfterDelay(localPlayerWon));
    }

    private IEnumerator ResolveResultAfterDelay(bool localPlayerWon)
    {
        matchState.MarkResultProcessed();
        matchState.MarkGameEnded();
        Debug.Log($"[GameManager] ResolveResultAfterDelay — won:{localPlayerWon} AutoSyncScene:{PhotonNetwork.AutomaticallySyncScene} InRoom:{PhotonNetwork.InRoom}");
        Debug.Log($"[GameManager] ResolveResultAfterDelay — won:{localPlayerWon}");
        Debug.Log(localPlayerWon ? "Ganaste." : "Perdiste.");

        
        if (localPlayerWon && PhotonNetwork.InRoom)
        {
            int score = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Score") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Score"] : 0;
            PhotonHashtable hash = new PhotonHashtable();
            hash["Score"] = score + 500;
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }

        if (endGameDelay > 0f)
        {
            yield return new WaitForSeconds(endGameDelay);
        }

        
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
        {
            Player p = PhotonNetwork.LocalPlayer;
            string name = string.IsNullOrEmpty(p.NickName) ? $"Jugador {p.ActorNumber}" : p.NickName;
            int score = p.CustomProperties.ContainsKey("Score") ? (int)p.CustomProperties["Score"] : 0;
            string macId = LeaderBoardAPI.Instance.GetMacAddress();
            LeaderBoardAPI.Instance.EnviarScore(name, score, macId);
        }

        pendingResultRoutine = null;
        LocalMatchResultResolved?.Invoke(localPlayerWon);
    }

    private IEnumerator InvokeMatchStateResetNextFrame()
    {
        yield return null;

        
        if (PhotonNetwork.InRoom)
        {
            PhotonHashtable hash = new PhotonHashtable();
            hash["Kills"] = 0;
            hash["Deaths"] = 0;
            hash["Score"] = 0;
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }

        Debug.Log($"[GameManager] MatchStateReset invocado — listeners: {MatchStateReset?.GetInvocationList().Length ?? 0}");
        MatchStateReset?.Invoke();
        matchState.MarkResultPending();
    }

    private void StopPendingResultRoutine()
    {
        if (pendingResultRoutine == null)
        {
            return;
        }

        StopCoroutine(pendingResultRoutine);
        pendingResultRoutine = null;
    }

    private void TrySyncRoundIdFromRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (TryReadRoundId(PhotonNetwork.CurrentRoom.CustomProperties, out int roomRoundId))
        {
            ApplyRoomRoundId(roomRoundId);
        }
    }

    private void ApplyRoomRoundId(int roomRoundId)
    {
        if (roomRoundId < 0 || matchState.RoundId == roomRoundId)
        {
            return;
        }

        matchState.SetRoundId(roomRoundId);
        Debug.Log($"[GameManager] RoundId sincronizado desde room: {roomRoundId}");
    }

    private void SetRoomRoundId(int roundId)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        PhotonHashtable properties = new PhotonHashtable
        {
            { RoomRoundIdPropertyKey, roundId }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private void SetRoomMatchEnded(bool ended)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        PhotonHashtable properties = new PhotonHashtable
        {
            { RoomMatchEndedPropertyKey, ended }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private void TrySyncMatchEndedFromRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (matchState.GameEnded)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomMatchEndedPropertyKey, out object val))
        {
            if (val is bool isEnded && isEnded)
            {
                Debug.Log($"[GameManager] Sincronizando MatchEnded como TRUE desde room.");
                matchState.MarkGameEnded();
                matchState.MarkResultProcessed();
                LocalMatchResultResolved?.Invoke(false);
            }
        }
    }

    private bool IsGameplayScene(UnityScene scene)
    {
        string expectedSceneName = ResolveGameplaySceneName();
        return !string.IsNullOrWhiteSpace(expectedSceneName)
            && string.Equals(scene.name, expectedSceneName, StringComparison.Ordinal);
    }

    private string ResolveGameplaySceneName()
    {
        PhotonManager manager = PhotonManager.Instance;
        if (manager != null && !string.IsNullOrWhiteSpace(manager.GameplaySceneName))
        {
            return manager.GameplaySceneName;
        }

        return fallbackGameplaySceneName;
    }

    private static bool TryReadRoundId(PhotonHashtable properties, out int roundId)
    {
        roundId = -1;

        if (properties == null || !properties.TryGetValue(RoomRoundIdPropertyKey, out object rawValue))
        {
            return false;
        }

        if (rawValue is int intValue)
        {
            roundId = intValue;
            return true;
        }

        if (rawValue is byte byteValue)
        {
            roundId = byteValue;
            return true;
        }

        if (rawValue is short shortValue)
        {
            roundId = shortValue;
            return true;
        }

        if (rawValue is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            roundId = (int)longValue;
            return true;
        }

        return false;
    }

}