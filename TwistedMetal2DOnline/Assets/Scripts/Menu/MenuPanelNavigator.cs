using UnityEngine;

internal enum MenuPanel
{
    MainMenu,
    RoomSelect,
    CreateRoom,
    JoinRoom,
    Lobby,
    Loading
}

internal sealed class MenuPanelNavigator
{
    private readonly GameObject panelMainMenu;
    private readonly GameObject panelRoomSelect;
    private readonly GameObject panelCreateRoom;
    private readonly GameObject panelJoinRoom;
    private readonly GameObject panelLobby;
    private readonly GameObject panelLoading;

    public MenuPanel CurrentPanel { get; private set; } = MenuPanel.MainMenu;

    public MenuPanelNavigator(
        GameObject panelMainMenu,
        GameObject panelRoomSelect,
        GameObject panelCreateRoom,
        GameObject panelJoinRoom,
        GameObject panelLobby,
        GameObject panelLoading)
    {
        this.panelMainMenu = panelMainMenu;
        this.panelRoomSelect = panelRoomSelect;
        this.panelCreateRoom = panelCreateRoom;
        this.panelJoinRoom = panelJoinRoom;
        this.panelLobby = panelLobby;
        this.panelLoading = panelLoading;
    }

    public void Show(MenuPanel panel)
    {
        CurrentPanel = panel;

        SetPanelActive(panelMainMenu, panel == MenuPanel.MainMenu);
        SetPanelActive(panelRoomSelect, panel == MenuPanel.RoomSelect);
        SetPanelActive(panelCreateRoom, panel == MenuPanel.CreateRoom);
        SetPanelActive(panelJoinRoom, panel == MenuPanel.JoinRoom);
        SetPanelActive(panelLobby, panel == MenuPanel.Lobby);
        SetPanelActive(panelLoading, panel == MenuPanel.Loading);
    }

    public bool HideLoading()
    {
        SetPanelActive(panelLoading, false);
        return CurrentPanel == MenuPanel.Loading;
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
}
