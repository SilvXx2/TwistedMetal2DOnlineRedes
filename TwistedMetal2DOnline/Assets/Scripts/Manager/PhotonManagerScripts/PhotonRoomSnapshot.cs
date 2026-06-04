using Photon.Pun;

internal readonly struct PhotonRoomSnapshot
{
    public string RoomName { get; }
    public int PlayerCount { get; }
    public int MaxPlayers { get; }

    public PhotonRoomSnapshot(string roomName, int playerCount, int maxPlayers)
    {
        RoomName = roomName;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
    }

    public static PhotonRoomSnapshot FromCurrentRoom()
    {
        if (PhotonNetwork.CurrentRoom == null)
        {
            return new PhotonRoomSnapshot(string.Empty, 0, 0);
        }

        return new PhotonRoomSnapshot(
            PhotonNetwork.CurrentRoom.Name,
            PhotonNetwork.CurrentRoom.PlayerCount,
            PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public string ToLobbyStatusMessage()
    {
        return $"Lobby: {RoomName}\nPlayers: {PlayerCount}/{MaxPlayers}";
    }
}
