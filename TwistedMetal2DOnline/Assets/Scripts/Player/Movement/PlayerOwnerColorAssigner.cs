using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

internal static class PlayerOwnerColorAssigner
{
    private static readonly Color[] OwnerColors =
    {
        Color.green,
        Color.red,
        Color.blue,
        Color.yellow
    };

    private static readonly string[] OwnerColorNames =
    {
        "Verde",
        "Rojo",
        "Azul",
        "Amarillo"
    };

    public static void ApplyOwnerColor(Renderer playerRenderer, PhotonView photonView)
    {
        if (playerRenderer == null || photonView == null || photonView.Owner == null)
        {
            return;
        }

        if (TryGetOwnerColor(photonView.Owner, out Color ownerColor))
        {
            playerRenderer.material.color = ownerColor;
        }
    }

    public static Color GetPlayerColor(Player player)
    {
        int slotIndex = ResolveSlotIndex(player);
        return OwnerColors[Mathf.Abs(slotIndex) % OwnerColors.Length];
    }

    public static string GetPlayerColorName(Player player)
    {
        int slotIndex = ResolveSlotIndex(player);
        return OwnerColorNames[Mathf.Abs(slotIndex) % OwnerColorNames.Length];
    }

    private static bool TryGetOwnerColor(Player owner, out Color ownerColor)
    {
        if (OwnerColors.Length == 0)
        {
            ownerColor = Color.white;
            return false;
        }

        int slotIndex = ResolveSlotIndex(owner);
        ownerColor = OwnerColors[Mathf.Abs(slotIndex) % OwnerColors.Length];
        return true;
    }

    private static int ResolveSlotIndex(Player player)
    {
        int slotIndex = PhotonPlayerSlotRegistry.GetFallbackSlotIndex(player);
        if (PhotonPlayerSlotRegistry.TryGetPlayerSlotIndex(player, out int stableSlotIndex))
        {
            slotIndex = stableSlotIndex;
        }

        return slotIndex;
    }
}
