using TMPro;
using UnityEngine.UI;

internal sealed class LobbyMenuView
{
    private readonly TMP_Text statusText;
    private readonly TMP_Text lobbyInfoText;
    private readonly Button lobbyStartGameButton;
    private readonly Button lobbyRestartGameButton;

    public LobbyMenuView(
        TMP_Text statusText,
        TMP_Text lobbyInfoText,
        Button lobbyStartGameButton,
        Button lobbyRestartGameButton)
    {
        this.statusText = statusText;
        this.lobbyInfoText = lobbyInfoText;
        this.lobbyStartGameButton = lobbyStartGameButton;
        this.lobbyRestartGameButton = lobbyRestartGameButton;
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void UpdateLobbyInfo(string roomName, int playerCount, int maxPlayers)
    {
        if (lobbyInfoText == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(roomName) || maxPlayers <= 0)
        {
            lobbyInfoText.text = string.Empty;
            return;
        }

        lobbyInfoText.text = $"Room: {roomName}\nPlayers: {playerCount}/{maxPlayers}";
    }

    public void ClearLobbyInfo()
    {
        if (lobbyInfoText != null)
        {
            lobbyInfoText.text = string.Empty;
        }
    }

    public void SetCanControlMatch(bool canControlMatch)
    {
        if (lobbyStartGameButton != null)
        {
            lobbyStartGameButton.interactable = canControlMatch;
        }

        if (lobbyRestartGameButton != null)
        {
            lobbyRestartGameButton.interactable = canControlMatch;
        }
    }
}
