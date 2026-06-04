using Photon.Pun;

internal sealed class PhotonConnectionState
{
    public bool IsConnected { get; private set; }
    public bool IsInLobby { get; private set; }

    public bool IsConnectedToLobby => IsConnected && IsInLobby;
    public bool CanUseLobbyOperations => IsConnected && IsInLobby;

    public void InitializeFromPhoton()
    {
        IsConnected = PhotonNetwork.IsConnected;
        IsInLobby = PhotonNetwork.InLobby;
    }

    public void MarkConnectedToMaster()
    {
        IsConnected = true;
    }

    public void MarkJoinedLobby()
    {
        IsConnected = true;
        IsInLobby = true;
    }

    public void MarkDisconnected()
    {
        IsConnected = false;
        IsInLobby = false;
    }
}
