using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

internal sealed class PhotonRoomService
{
    private readonly PhotonConnectionState connectionState;
    private readonly PhotonRoomState roomState;
    private readonly Action<string> emitStatus;

    public PhotonRoomService(PhotonConnectionState connectionState, PhotonRoomState roomState, Action<string> emitStatus)
    {
        this.connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
        this.roomState = roomState ?? throw new ArgumentNullException(nameof(roomState));
        this.emitStatus = emitStatus ?? throw new ArgumentNullException(nameof(emitStatus));
    }

    public bool JoinSelectedRoom(string roomName)
    {
        if (!connectionState.CanUseLobbyOperations)
        {
            Debug.LogWarning("Todavía no estás conectado o no entraste al lobby.");
            emitStatus("Todavía no se conectó al lobby...");
            return false;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 4
        };

        Debug.Log($"Intentando unirse a la room: {roomName}");
        emitStatus($"Uniéndose a {roomName}...");

        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        return true;
    }

    public bool JoinExistingRoom(string roomName)
    {
        if (!connectionState.CanUseLobbyOperations)
        {
            Debug.LogWarning("Todavía no estás conectado o no entraste al lobby.");
            emitStatus("Todavía no se conectó al lobby...");
            return false;
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            emitStatus("Nombre de room inválido.");
            return false;
        }

        Debug.Log($"Intentando entrar a room existente: {roomName}");
        emitStatus($"Uniéndose a {roomName}...");

        PhotonNetwork.JoinRoom(roomName);
        return true;
    }

    public void LeaveCurrentRoom()
    {
        if (roomState.IsLeavingRoom)
        {
            emitStatus("Saliendo de la room...");
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            emitStatus("No estás dentro de una room.");
            return;
        }

        roomState.MarkLeavingRoom();
        emitStatus("Saliendo de la room...");
        PhotonNetwork.LeaveRoom();
    }
}
