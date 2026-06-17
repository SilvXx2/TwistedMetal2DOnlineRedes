using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance { get; private set; }

    [Header("Escenas")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private string lobbySceneName = "Menu";

    [Header("Configuracion de room")]
    [SerializeField] private bool closeAndHideRoomOnMatchStart = false;

    public bool IsConnectedToLobby => connectionState != null && connectionState.IsConnectedToLobby;
    public string CurrentStatusMessage => currentStatusMessage;
    public bool IsInRoom => PhotonNetwork.InRoom;
    public bool IsMasterClient => PhotonNetwork.IsMasterClient;
    public string GameplaySceneName => gameplaySceneName;

    public event Action<string> StatusChanged;
    public event Action LobbyReady;
    public event Action<string, int, int> RoomJoined;
    public event Action<int, int> RoomPlayerCountChanged;
    public event Action RoomLeft;
    public event Action<Photon.Realtime.DisconnectCause> ConnectionFailed;

    private const byte ForceRestartEventCode = 21;

    private PhotonConnectionState connectionState;
    private PhotonRoomState roomState;
    private PhotonRoomService roomService;
    private PhotonMatchService matchService;
    private string currentStatusMessage = string.Empty;
    private bool wasOutOfFocus = false;
    private Coroutine resetFocusFlagsCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonPlayerSlotRegistry.EnsureLocalStableIdentity();
        InitializeServices();

        // Precargar la lista de taunts desde la API al arrancar
        var _ = TauntList.Instance;

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Conectando a Photon...");
            EmitStatus("Conectando a Photon...");
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        connectionState.InitializeFromPhoton();
        EmitStatus(connectionState.IsInLobby ? "Conectado al lobby." : "Conectado a Photon.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al Master");
        connectionState.MarkConnectedToMaster();
        PhotonPlayerSlotRegistry.EnsureLocalStableIdentity();
        EmitStatus("Conectado. Entrando al lobby...");

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        connectionState.MarkJoinedLobby();
        EmitStatus("Conectado al lobby.");
        LobbyReady?.Invoke();
    }

    public bool JoinSelectedRoom(string roomName)
    {
        return roomService.JoinSelectedRoom(roomName);
    }

    public bool JoinExistingRoom(string roomName)
    {
        return roomService.JoinExistingRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        PhotonPlayerSlotRegistry.EnsureLocalStableIdentity();

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonPlayerSlotRegistry.EnsureSlotsAssignedForCurrentPlayers(GetPreferredSlotCount());
        }

        PhotonPlayerSlotRegistry.TryApplyLocalPlayerSlotProperty();

        matchService.ResetMatchState();
        roomState.ResetRoomFlags();

        StartCoroutine(CleanUpOrphanedPhotonViewsDelayed());

        PhotonRoomSnapshot snapshot = PhotonRoomSnapshot.FromCurrentRoom();

        Debug.Log($"Entró a la room: {snapshot.RoomName}, jugadores: {snapshot.PlayerCount}");
        EmitStatus(snapshot.ToLobbyStatusMessage());

        RoomJoined?.Invoke(snapshot.RoomName, snapshot.PlayerCount, snapshot.MaxPlayers);
        RoomPlayerCountChanged?.Invoke(snapshot.PlayerCount, snapshot.MaxPlayers);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonPlayerSlotRegistry.EnsureSlotsAssignedForCurrentPlayers(GetPreferredSlotCount());
        }

        PhotonRoomSnapshot snapshot = PhotonRoomSnapshot.FromCurrentRoom();

        Debug.Log($"Un jugador se ha unido: {newPlayer.ActorNumber}");
        Debug.Log($"Jugadores en room: {snapshot.PlayerCount}");

        EmitStatus(snapshot.ToLobbyStatusMessage());
        RoomPlayerCountChanged?.Invoke(snapshot.PlayerCount, snapshot.MaxPlayers);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        StartCoroutine(CleanUpOrphanedPhotonViewsDelayed());

        PhotonRoomSnapshot snapshot = PhotonRoomSnapshot.FromCurrentRoom();

        Debug.Log($"Un jugador salió: {otherPlayer.ActorNumber}");
        Debug.Log($"Jugadores en room: {snapshot.PlayerCount}");

        EmitStatus(snapshot.ToLobbyStatusMessage());
        RoomPlayerCountChanged?.Invoke(snapshot.PlayerCount, snapshot.MaxPlayers);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonPlayerSlotRegistry.EnsureSlotsAssignedForCurrentPlayers(GetPreferredSlotCount());
        }

        PhotonPlayerSlotRegistry.TryApplyLocalPlayerSlotProperty();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!PhotonNetwork.InRoom || targetPlayer == null || changedProps == null)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey(PhotonPlayerSlotRegistry.PlayerStableIdPropertyKey))
        {
            PhotonPlayerSlotRegistry.EnsureSlotsAssignedForCurrentPlayers(GetPreferredSlotCount());
        }

        if (targetPlayer.IsLocal && changedProps.ContainsKey(PhotonPlayerSlotRegistry.PlayerSlotPropertyKey))
        {
            PhotonPlayerSlotRegistry.TryApplyLocalPlayerSlotProperty();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged == null)
        {
            return;
        }

        if (propertiesThatChanged.ContainsKey(PhotonPlayerSlotRegistry.RoomSlotMapPropertyKey))
        {
            PhotonPlayerSlotRegistry.TryApplyLocalPlayerSlotProperty();
        }
    }

    public bool RequestStartGame()
    {
        return matchService.RequestStartGame(EmitStatus);
    }

    public bool RequestRestartGame()
    {
        return matchService.RequestRestartGame(EmitStatus);
    }

    public void LeaveCurrentRoom()
    {
        roomService.LeaveCurrentRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PhotonManager] OnLeftRoom");
        matchService.ResetMatchState();
        roomState.ResetRoomFlags();
        RoomLeft?.Invoke();
        EmitStatus("Saliste de la room.");

        // Volver a la pantalla del menú/lobby si salimos de la room durante el gameplay
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != lobbySceneName)
        {
            Debug.Log($"[PhotonManager] Cargando escena de lobby/menu '{lobbySceneName}' debido a que salimos de la room.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[PhotonManager] OnDisconnected — causa:{cause}");
        connectionState.MarkDisconnected();
        matchService.ResetMatchState();
        roomState.ResetRoomFlags();

        string statusMsg = $"Desconectado: {cause}";
        if (wasOutOfFocus)
        {
            statusMsg = $"Desconectado por pérdida de foco (AppOutOfFocus) - Causa: {cause}";
            wasOutOfFocus = false;
        }

        EmitStatus(statusMsg);
        ConnectionFailed?.Invoke(cause);

        // Volver a la pantalla del menú/lobby si nos desconectamos durante el gameplay
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != lobbySceneName)
        {
            Debug.Log($"[PhotonManager] Cargando escena de lobby/menu '{lobbySceneName}' debido a desconexión.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            wasOutOfFocus = true;
            if (resetFocusFlagsCoroutine != null)
            {
                StopCoroutine(resetFocusFlagsCoroutine);
                resetFocusFlagsCoroutine = null;
            }
            Debug.Log("[PhotonManager] Aplicación fuera de foco. Se marca flag wasOutOfFocus = true.");
        }
        else
        {
            // Esperar 2 segundos antes de limpiar la bandera por si se produce desconexión diferida
            if (gameObject.activeInHierarchy)
            {
                if (resetFocusFlagsCoroutine != null)
                {
                    StopCoroutine(resetFocusFlagsCoroutine);
                }
                resetFocusFlagsCoroutine = StartCoroutine(ResetFocusFlagsDelayed());
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            wasOutOfFocus = true;
            if (resetFocusFlagsCoroutine != null)
            {
                StopCoroutine(resetFocusFlagsCoroutine);
                resetFocusFlagsCoroutine = null;
            }
            Debug.Log("[PhotonManager] Aplicación pausada. Se marca flag wasOutOfFocus = true.");
        }
        else
        {
            if (gameObject.activeInHierarchy)
            {
                if (resetFocusFlagsCoroutine != null)
                {
                    StopCoroutine(resetFocusFlagsCoroutine);
                }
                resetFocusFlagsCoroutine = StartCoroutine(ResetFocusFlagsDelayed());
            }
        }
    }

    private IEnumerator ResetFocusFlagsDelayed()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (PhotonNetwork.IsConnected)
        {
            wasOutOfFocus = false;
            Debug.Log("[PhotonManager] Foco recuperado y sigue conectado. Flag wasOutOfFocus restablecido a false.");
        }
        resetFocusFlagsCoroutine = null;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"No se pudo entrar a la room. Code: {returnCode} - {message}");
        EmitStatus($"No se pudo entrar a la room: {message}");
    }

    public void Disconnect()
    {
        if (!PhotonNetwork.IsConnected) return;

        PhotonNetwork.Disconnect();
    }

    public void DisableSceneSync()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
    }

    private void EmitStatus(string message)
    {
        currentStatusMessage = message;
        StatusChanged?.Invoke(message);
    }

    private void InitializeServices()
    {
        connectionState = new PhotonConnectionState();
        roomState = new PhotonRoomState();
        roomService = new PhotonRoomService(connectionState, roomState, EmitStatus);
        matchService = new PhotonMatchService(gameplaySceneName, lobbySceneName, closeAndHideRoomOnMatchStart, ForceRestartEventCode);
    }

    private static int GetPreferredSlotCount()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return 4;
        }

        return PhotonNetwork.CurrentRoom.MaxPlayers > 0 ? PhotonNetwork.CurrentRoom.MaxPlayers : 4;
    }

 
    private IEnumerator CleanUpOrphanedPhotonViewsDelayed()
    {
        yield return null;
        yield return null;

        if (!PhotonNetwork.InRoom)
        {
            yield break;
        }

        PhotonView[] allViews = FindObjectsOfType<PhotonView>();
        for (int i = 0; i < allViews.Length; i++)
        {
            PhotonView view = allViews[i];
            if (view == null)
            {
                continue;
            }

            if (view.CreatorActorNr == 0)
            {
                continue;
            }

            if (view.Owner != null)
            {
                continue;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[PhotonManager] Destroying orphaned PhotonView {view.ViewID} (creatorActor:{view.CreatorActorNr})");
                PhotonNetwork.Destroy(view.gameObject);
            }
            else if (view.IsMine)
            {
                Debug.Log($"[PhotonManager] Destroying own orphaned PhotonView {view.ViewID}");
                PhotonNetwork.Destroy(view.gameObject);
            }
            else
            {
                Debug.Log($"[PhotonManager] Hiding orphaned PhotonView {view.ViewID} (creatorActor:{view.CreatorActorNr})");
                view.gameObject.SetActive(false);
            }
        }
    }
}
