internal enum MenuBackNavigationDecision
{
    ShowMainMenu,
    ShowNickname,
    ShowRoomSelect,
    LeaveLobby
}

internal sealed class MenuBackNavigationPolicy
{
    public MenuBackNavigationDecision Resolve(MenuPanel currentPanel)
    {
        switch (currentPanel)
        {
            case MenuPanel.RoomSelect:
                return MenuBackNavigationDecision.ShowNickname;
            case MenuPanel.CreateRoom:
            case MenuPanel.JoinRoom:
                return MenuBackNavigationDecision.ShowRoomSelect;
            case MenuPanel.Lobby:
                return MenuBackNavigationDecision.LeaveLobby;
            case MenuPanel.Nickname:
            case MenuPanel.Loading:
            case MenuPanel.MainMenu:
            default:
                return MenuBackNavigationDecision.ShowMainMenu;
        }
    }
}
