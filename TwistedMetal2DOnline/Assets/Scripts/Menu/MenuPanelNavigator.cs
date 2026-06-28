using UnityEngine;

internal enum MenuPanel
{
    MainMenu,
    Nickname,
    ColorSelect,
    RoomSelect,
    CreateRoom,
    JoinRoom,
    Lobby,
    Loading,
    PasswordPrompt
}

internal sealed class MenuPanelNavigator
{
    private readonly GameObject panelMainMenu;
    private readonly GameObject panelNickname;
    private readonly GameObject panelColorSelect;
    private readonly GameObject panelRoomSelect;
    private readonly GameObject panelCreateRoom;
    private readonly GameObject panelJoinRoom;
    private readonly GameObject panelLobby;
    private readonly GameObject panelLoading;
    private readonly GameObject panelPasswordPrompt;

    public MenuPanel CurrentPanel { get; private set; } = MenuPanel.MainMenu;

    public MenuPanelNavigator(
        GameObject panelMainMenu,
        GameObject panelNickname,
        GameObject panelColorSelect,
        GameObject panelRoomSelect,
        GameObject panelCreateRoom,
        GameObject panelJoinRoom,
        GameObject panelLobby,
        GameObject panelLoading,
        GameObject panelPasswordPrompt)
    {
        this.panelMainMenu = panelMainMenu;
        this.panelNickname = panelNickname;
        this.panelColorSelect = panelColorSelect;
        this.panelRoomSelect = panelRoomSelect;
        this.panelCreateRoom = panelCreateRoom;
        this.panelJoinRoom = panelJoinRoom;
        this.panelLobby = panelLobby;
        this.panelLoading = panelLoading;
        this.panelPasswordPrompt = panelPasswordPrompt;
    }

    public void Show(MenuPanel panel)
    {
        CurrentPanel = panel;

        SetPanelActive(panelMainMenu, panel == MenuPanel.MainMenu);
        SetPanelActive(panelNickname, panel == MenuPanel.Nickname);
        SetPanelActive(panelColorSelect, panel == MenuPanel.ColorSelect);
        SetPanelActive(panelRoomSelect, panel == MenuPanel.RoomSelect);
        SetPanelActive(panelCreateRoom, panel == MenuPanel.CreateRoom);
        SetPanelActive(panelJoinRoom, panel == MenuPanel.JoinRoom);
        SetPanelActive(panelLobby, panel == MenuPanel.Lobby);
        SetPanelActive(panelLoading, panel == MenuPanel.Loading);
        SetPanelActive(panelPasswordPrompt, panel == MenuPanel.PasswordPrompt);
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
