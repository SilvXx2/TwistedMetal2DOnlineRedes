using Photon.Realtime;
using System;

internal interface IPhotonMenuService
{
    string CurrentStatusMessage { get; }
    bool IsInRoom { get; }
    bool IsMasterClient { get; }

    event Action<string> StatusChanged;
    event Action LobbyReady;
    event Action<string, int, int> RoomJoined;
    event Action<int, int> RoomPlayerCountChanged;
    event Action RoomLeft;
    event Action<DisconnectCause> ConnectionFailed;

    bool JoinSelectedRoom(string roomName);
    bool JoinSelectedRoom(string roomName, string password);
    bool JoinExistingRoom(string roomName);
    bool RequestStartGame();
    bool RequestRestartGame();
    void LeaveCurrentRoom();
}

internal sealed class PhotonMenuServiceAdapter : IPhotonMenuService
{
    public PhotonManager Source { get; }

    public PhotonMenuServiceAdapter(PhotonManager source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Source = source;
    }

    public string CurrentStatusMessage => Source.CurrentStatusMessage;
    public bool IsInRoom => Source.IsInRoom;
    public bool IsMasterClient => Source.IsMasterClient;

    public event Action<string> StatusChanged
    {
        add { Source.StatusChanged += value; }
        remove { Source.StatusChanged -= value; }
    }

    public event Action LobbyReady
    {
        add { Source.LobbyReady += value; }
        remove { Source.LobbyReady -= value; }
    }

    public event Action<string, int, int> RoomJoined
    {
        add { Source.RoomJoined += value; }
        remove { Source.RoomJoined -= value; }
    }

    public event Action<int, int> RoomPlayerCountChanged
    {
        add { Source.RoomPlayerCountChanged += value; }
        remove { Source.RoomPlayerCountChanged -= value; }
    }

    public event Action RoomLeft
    {
        add { Source.RoomLeft += value; }
        remove { Source.RoomLeft -= value; }
    }

    public event Action<DisconnectCause> ConnectionFailed
    {
        add { Source.ConnectionFailed += value; }
        remove { Source.ConnectionFailed -= value; }
    }

    public bool JoinSelectedRoom(string roomName)
    {
        return Source.JoinSelectedRoom(roomName);
    }

    public bool JoinSelectedRoom(string roomName, string password)
    {
        return Source.JoinSelectedRoom(roomName, password);
    }

    public bool JoinExistingRoom(string roomName)
    {
        return Source.JoinExistingRoom(roomName);
    }

    public bool RequestStartGame()
    {
        return Source.RequestStartGame();
    }

    public bool RequestRestartGame()
    {
        return Source.RequestRestartGame();
    }

    public void LeaveCurrentRoom()
    {
        Source.LeaveCurrentRoom();
    }
}
