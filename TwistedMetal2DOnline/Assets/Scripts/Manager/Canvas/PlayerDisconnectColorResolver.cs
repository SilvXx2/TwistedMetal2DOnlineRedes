using Photon.Realtime;
using UnityEngine;


internal static class PlayerDisconnectColorResolver
{
    public static string GetColorName(Player player)
    {
        return PlayerOwnerColorAssigner.GetPlayerColorName(player);
    }

    public static Color GetColor(Player player)
    {
        return PlayerOwnerColorAssigner.GetPlayerColor(player);
    }
}
