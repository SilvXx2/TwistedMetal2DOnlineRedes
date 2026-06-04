using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

internal sealed class PhotonMatchResultEventBridge
{
    private readonly byte matchResultEventCode;

    public PhotonMatchResultEventBridge(byte matchResultEventCode)
    {
        this.matchResultEventCode = matchResultEventCode;
    }

    public void BroadcastMatchResult(int winnerActorNumber, int roundId)
    {
        Player[] roomPlayers = PhotonNetwork.PlayerList;
        for (int i = 0; i < roomPlayers.Length; i++)
        {
            Player roomPlayer = roomPlayers[i];
            bool playerWon = roomPlayer.ActorNumber == winnerActorNumber;
            int packedData = Pack(roundId, playerWon);

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                TargetActors = new[] { roomPlayer.ActorNumber }
            };

            PhotonNetwork.RaiseEvent(matchResultEventCode, packedData, raiseEventOptions, SendOptions.SendReliable);
        }
    }

    public static bool TryUnpack(object customData, out int eventRoundId, out bool localPlayerWon)
    {
        if (customData is int packedData)
        {
            eventRoundId = packedData >> 1;
            localPlayerWon = (packedData & 1) == 1;
            return true;
        }

        eventRoundId = 0;
        localPlayerWon = false;
        return false;
    }

    public static string ExtractSceneName(object customData)
    {
        return customData as string;
    }

    private static int Pack(int roundId, bool playerWon)
    {
        return (roundId << 1) | (playerWon ? 1 : 0);
    }
}
