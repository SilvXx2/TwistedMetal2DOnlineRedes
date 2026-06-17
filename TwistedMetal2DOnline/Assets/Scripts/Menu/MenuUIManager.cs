using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class MenuUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelNickname;
    [SerializeField] private GameObject panelColorSelect;
    [SerializeField] private GameObject panelRoomSelect;
    [SerializeField] private GameObject panelCreateRoom;
    [SerializeField] private GameObject panelJoinRoom;
    [SerializeField] private GameObject panelLobby;
    [SerializeField] private GameObject panelLoading;

    [Header("Nickname Inputs")]
    [SerializeField] private TMP_InputField nicknameInput;

    [Header("Texts")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text lobbyInfoText;

    [Header("Room Inputs")]
    [SerializeField] private TMP_InputField createRoomInput;
    [SerializeField] private TMP_InputField joinRoomInput;
    [SerializeField] private string defaultCreateRoomName = "Room1";
    [SerializeField] private string defaultJoinRoomName   = "Room1";

    [Header("Lobby Buttons (Opcional)")]
    [SerializeField] private Button lobbyStartGameButton;
    [SerializeField] private Button lobbyRestartGameButton;

    private IPhotonMenuService photonService;
    private MenuPanelNavigator panelNavigator;
    private LobbyMenuView lobbyView;
    private MenuBackNavigationPolicy backNavigationPolicy;
    private ConnectionTimeoutWatcher connectionTimeout;
    private string currentRoomName = string.Empty;
    private bool leavingRoom = false;
    private bool waitingForMatchSceneLoad = false;

    private const float ConnectionTimeoutSeconds = 15f;
    private const string ConnectionTimeoutMessage = "No tiene conexión a internet o Server time out";

    private void Awake()
    {
        panelNavigator = new MenuPanelNavigator(
            panelMainMenu,
            panelNickname,
            panelColorSelect,
            panelRoomSelect,
            panelCreateRoom,
            panelJoinRoom,
            panelLobby,
            panelLoading);

        lobbyView = new LobbyMenuView(
            statusText,
            lobbyInfoText,
            lobbyStartGameButton,
            lobbyRestartGameButton);

        backNavigationPolicy = new MenuBackNavigationPolicy();
        connectionTimeout = new ConnectionTimeoutWatcher(ConnectionTimeoutSeconds);
    }

    private void OnEnable()
    {
        TryBindPhotonService();
    }

    private void OnDisable()
    {
        UnbindPhotonService();
    }

    private void Start()
    {
        ShowMainMenu();
        RefreshLobbyButtonsInteractivity();

        if (photonService != null && !string.IsNullOrEmpty(photonService.CurrentStatusMessage))
        {
            SetStatus(photonService.CurrentStatusMessage);
        }

        SyncLobbyFromPhotonState();

        if (nicknameInput != null)
        {
            nicknameInput.text = SaveManager.Instance.CurrentSave.PlayerNickname;
        }
    }

    private void Update()
    {
        if (photonService == null)
        {
            TryBindPhotonService();
        }

        if (connectionTimeout.CheckTimeout(Time.unscaledTime))
        {
            OnConnectionTimeout();
        }

        SyncLobbyFromPhotonState();
    }

    public void HideLoading()
    {
        if (panelNavigator.HideLoading())
        {
            ShowMainMenu();
        }
    }

    public void ShowMainMenu()  => panelNavigator.Show(MenuPanel.MainMenu);
    public void ShowNickname()  => panelNavigator.Show(MenuPanel.Nickname);
    public void ShowColorSelect() => panelNavigator.Show(MenuPanel.ColorSelect);
    public void ShowRoomSelect() => panelNavigator.Show(MenuPanel.RoomSelect);
    public void ShowCreateRoom() => panelNavigator.Show(MenuPanel.CreateRoom);
    public void ShowJoinRoom()   => panelNavigator.Show(MenuPanel.JoinRoom);
    public void ShowLobby()      => panelNavigator.Show(MenuPanel.Lobby);

    public void ShowLoading(string message)
    {
        panelNavigator.Show(MenuPanel.Loading);
        SetStatus(message);
    }

    public void SetStatus(string message)
    {
        lobbyView.SetStatus(message);
    }

    public void OnClickStart()
    {
       
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            connectionTimeout.Start(Time.unscaledTime);
        }

        ShowNickname();
    }

    public void OnClickNicknameConfirm()
    {
        if (nicknameInput == null)
        {
            ShowColorSelect();
            return;
        }

        string nickname = nicknameInput.text;
        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = "Player_" + Random.Range(1000, 9999);
        }
        else
        {
            nickname = nickname.Trim();
        }

        PhotonNetwork.NickName = nickname;
        SaveManager.Instance.CurrentSave.PlayerNickname = nickname;
        SaveManager.Instance.Save();

        ShowColorSelect();
    }

    public void OnClickSelectCarColor(int colorIndex)
    {
        PlayerPrefs.SetInt("SelectedCarSkin", colorIndex);
        PlayerPrefs.Save();

        ExitGames.Client.Photon.Hashtable props =
            new ExitGames.Client.Photon.Hashtable();

        props["CarSkin"] = colorIndex;

        Photon.Pun.PhotonNetwork.LocalPlayer
            .SetCustomProperties(props);

        ShowRoomSelect();
    }


    public void OnClickOpenCreateRoom()   => ShowCreateRoom();
    public void OnClickOpenJoinRoom()     => ShowJoinRoom();

    public void OnClickExit()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }

    public void OnClickBack()
    {
        switch (backNavigationPolicy.Resolve(panelNavigator.CurrentPanel))
        {
            case MenuBackNavigationDecision.ShowNickname:
                ShowNickname();
                break;
            case MenuBackNavigationDecision.ShowRoomSelect:
                ShowRoomSelect();
                break;
            case MenuBackNavigationDecision.LeaveLobby:
                OnClickLobbyLeave();
                break;
            default:
                ShowMainMenu();
                break;
        }
    }

    public void OnClickCreateRoomConfirm()
    {
        string roomName = GetRoomName(createRoomInput, defaultCreateRoomName);
        TryJoinRoom(roomName, $"Creando room {roomName}...");
    }

    public void OnClickJoinRoomConfirm()
    {
        string roomName = GetRoomName(joinRoomInput, defaultJoinRoomName);
        TryJoinRoom(roomName, $"Uniéndose a {roomName}...");
    }

    public void OnClickLobbyStartGame()
    {
        if (!EnsurePhotonService("PhotonManager no encontrado.")) return;

        RefreshLobbyButtonsInteractivity();

        if (photonService.RequestStartGame())
        {
            waitingForMatchSceneLoad = true;
            ShowLoading("Iniciando partida...");
        }
    }

    public void OnClickLobbyRestartGame()
    {
        if (!EnsurePhotonService("PhotonManager no encontrado.")) return;

        RefreshLobbyButtonsInteractivity();

        if (photonService.RequestRestartGame())
        {
            waitingForMatchSceneLoad = true;
            ShowLoading("Reiniciando partida...");
        }
    }

    public void OnClickLobbyLeave()
    {
        if (!EnsurePhotonService("PhotonManager no encontrado.", showRoomSelectOnFail: true)) return;

        waitingForMatchSceneLoad = false;
        leavingRoom = true;
        ShowLoading("Saliendo de la room...");
        photonService.LeaveCurrentRoom();
    }

    private void TryJoinRoom(string roomName, string loadingMessage)
    {
        if (!EnsurePhotonService("PhotonManager no encontrado.")) return;

        ShowLoading(loadingMessage);
        photonService.JoinSelectedRoom(roomName);
    }

    private bool EnsurePhotonService(string failureMessage, bool showRoomSelectOnFail = false)
    {
        TryBindPhotonService();

        if (photonService != null) return true;

        if (showRoomSelectOnFail)
            ShowRoomSelect();
        else
            ShowLoading(failureMessage);

        return false;
    }

    private void TryBindPhotonService()
    {
        PhotonManager manager = PhotonManager.Instance;

        if (manager == null)
        {
            UnbindPhotonService();
            return;
        }

        if (photonService is PhotonMenuServiceAdapter adapter && ReferenceEquals(adapter.Source, manager))
            return;

        UnbindPhotonService();
        photonService = new PhotonMenuServiceAdapter(manager);

        photonService.StatusChanged          += OnPhotonStatusChanged;
        photonService.LobbyReady             += OnLobbyReady;
        photonService.RoomJoined             += OnRoomJoined;
        photonService.RoomPlayerCountChanged += OnRoomPlayerCountChanged;
        photonService.RoomLeft               += OnRoomLeft;
        photonService.ConnectionFailed       += OnConnectionFailed;
    }

    private void UnbindPhotonService()
    {
        if (photonService == null) return;

        photonService.StatusChanged          -= OnPhotonStatusChanged;
        photonService.LobbyReady             -= OnLobbyReady;
        photonService.RoomJoined             -= OnRoomJoined;
        photonService.RoomPlayerCountChanged -= OnRoomPlayerCountChanged;
        photonService.RoomLeft               -= OnRoomLeft;
        photonService.ConnectionFailed       -= OnConnectionFailed;
        photonService = null;
    }

    private void OnPhotonStatusChanged(string message)
    {
        if (ShouldRenderStatusInline())
        {
            SetStatus(message);
            return;
        }

        ShowLoading(message);
    }

    private bool ShouldRenderStatusInline()
    {
        MenuPanel currentPanel = panelNavigator.CurrentPanel;
        return currentPanel == MenuPanel.Lobby
            || currentPanel == MenuPanel.RoomSelect
            || currentPanel == MenuPanel.Nickname
            || currentPanel == MenuPanel.MainMenu;
    }

    private void OnLobbyReady()
    {
        waitingForMatchSceneLoad = false;
        connectionTimeout.Stop();

        if (leavingRoom) return;

        if (panelNavigator.CurrentPanel == MenuPanel.Loading || panelNavigator.CurrentPanel == MenuPanel.MainMenu)
            ShowMainMenu();

        RefreshLobbyButtonsInteractivity();
    }

    private void OnRoomJoined(string roomName, int playerCount, int maxPlayers)
    {
        waitingForMatchSceneLoad = false;
        leavingRoom     = false;
        currentRoomName = roomName;
        ShowLobby();
        lobbyView.UpdateLobbyInfo(currentRoomName, playerCount, maxPlayers);
        RefreshLobbyButtonsInteractivity();
    }

    private void OnRoomPlayerCountChanged(int playerCount, int maxPlayers)
    {
        lobbyView.UpdateLobbyInfo(currentRoomName, playerCount, maxPlayers);
        RefreshLobbyButtonsInteractivity();
    }

    private void OnRoomLeft()
    {
        waitingForMatchSceneLoad = false;
        leavingRoom     = false;
        currentRoomName = string.Empty;
        lobbyView.ClearLobbyInfo();
        ShowRoomSelect();
        RefreshLobbyButtonsInteractivity();
    }

    private static string GetRoomName(TMP_InputField inputField, string fallback)
    {
        if (inputField == null) return fallback;
        string value = inputField.text;
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private void RefreshLobbyButtonsInteractivity()
    {
        bool canControlMatch = photonService != null && photonService.IsInRoom && photonService.IsMasterClient;
        lobbyView.SetCanControlMatch(canControlMatch);
    }

    private void SyncLobbyFromPhotonState()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (panelNavigator.CurrentPanel == MenuPanel.Lobby)
        {
            return;
        }

        if (panelNavigator.CurrentPanel == MenuPanel.Loading && (leavingRoom || waitingForMatchSceneLoad))
        {
            return;
        }

        leavingRoom = false;
        currentRoomName = PhotonNetwork.CurrentRoom.Name;

        ShowLobby();
        lobbyView.UpdateLobbyInfo(currentRoomName, PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);
        RefreshLobbyButtonsInteractivity();
    }

    private void OnConnectionFailed(DisconnectCause cause)
    {
        Debug.Log($"[MenuUIManager] OnConnectionFailed — causa: {cause}");
        connectionTimeout.Stop();
        ShowMainMenu();
        SetStatus(ConnectionTimeoutMessage);
    }

    private void OnConnectionTimeout()
    {
        Debug.Log("[MenuUIManager] Connection timeout alcanzado.");
        ShowMainMenu();
        SetStatus(ConnectionTimeoutMessage);
    }
}