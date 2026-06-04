using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

internal sealed class PhotonMatchService
{
    private readonly string gameplaySceneName;
    private readonly string lobbySceneName;
    private readonly bool closeAndHideRoomOnMatchStart;
    private readonly byte forceRestartEventCode;

    private bool matchStarting;

    public PhotonMatchService(string gameplaySceneName, string lobbySceneName, bool closeAndHideRoomOnMatchStart, byte forceRestartEventCode)
    {
        this.gameplaySceneName = gameplaySceneName;
        this.lobbySceneName = lobbySceneName;
        this.closeAndHideRoomOnMatchStart = closeAndHideRoomOnMatchStart;
        this.forceRestartEventCode = forceRestartEventCode;
    }

    public bool RequestStartGame(Action<string> emitStatus)
    {
        if (!PhotonNetwork.InRoom)
        {
            emitStatus("No estás en una room.");
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            emitStatus("Solo el host puede iniciar la partida.");
            return false;
        }

        if (matchStarting)
        {
            emitStatus("La partida ya se está iniciando.");
            return false;
        }

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        if (playerCount < 2)
        {
            emitStatus("Se necesitan al menos 2 jugadores para iniciar.");
            return false;
        }

        Debug.Log("Host inició la partida");
        StartMatch(emitStatus);
        return true;
    }

    public bool RequestRestartGame(Action<string> emitStatus)
    {
        if (!PhotonNetwork.InRoom)
        {
            emitStatus("No estás en una room.");
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            emitStatus("Solo el host puede reiniciar la partida.");
            return false;
        }

        matchStarting = false;

        int playerCount = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        bool shouldReturnToLobby = playerCount < 2;

        string targetSceneName = shouldReturnToLobby
            ? ResolveTargetLobbySceneName()
            : gameplaySceneName;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            emitStatus("No se pudo reiniciar: escena destino inválida.");
            return false;
        }

        if (shouldReturnToLobby)
        {
            Debug.Log("Host solicitó reinicio con 1 jugador: volver al lobby.");
            emitStatus("Solo queda 1 jugador. Volviendo al lobby...");

            if (closeAndHideRoomOnMatchStart && PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = true;
                PhotonNetwork.CurrentRoom.IsVisible = true;
            }
        }
        else
        {
            Debug.Log("Host reinició la partida");
            emitStatus("Reiniciando partida...");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(forceRestartEventCode, targetSceneName, options, SendOptions.SendReliable);
        return true;
    }

    private string ResolveTargetLobbySceneName()
    {
        if (!string.IsNullOrWhiteSpace(lobbySceneName))
        {
            return lobbySceneName;
        }

        return "Menu";
    }

    public void ResetMatchState()
    {
        matchStarting = false;
    }

    private void StartMatch(Action<string> emitStatus)
    {
        if (matchStarting)
        {
            return;
        }

        matchStarting = true;
        StartGame(emitStatus);
    }

    private void StartGame(Action<string> emitStatus)
    {
        Debug.Log(">>> StartGame ejecutado");
        emitStatus("Iniciando partida...");

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (closeAndHideRoomOnMatchStart && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
        }

        PhotonNetwork.LoadLevel(gameplaySceneName);
    }
}
