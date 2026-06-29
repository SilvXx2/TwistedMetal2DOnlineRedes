internal enum MenuBackNavigationDecision
{
    ShowMainMenu,
    ShowLanguage,
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
            case MenuPanel.PasswordPrompt:
                return MenuBackNavigationDecision.ShowRoomSelect;
            case MenuPanel.Lobby:
                return MenuBackNavigationDecision.LeaveLobby;
            case MenuPanel.Nickname:
                return MenuBackNavigationDecision.ShowLanguage;
            case MenuPanel.Language:
            case MenuPanel.Loading:
            case MenuPanel.MainMenu:
            default:
                return MenuBackNavigationDecision.ShowMainMenu;
        }
    }
}
