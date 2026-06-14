using System;
using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

internal static class PhotonPlayerSlotRegistry
{
    public const string PlayerStableIdPropertyKey = "tgSid";
    public const string PlayerSlotPropertyKey = "tgSlot";
    public const string RoomSlotMapPropertyKey = "tgSlotMap";

    private const string LocalStableIdPrefsKey = "TankGame.StablePlayerId";
    private static string localSessionStableId = null;

    public static void EnsureLocalStableIdentity()
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("[SlotRegistry] EnsureLocalStableIdentity: localPlayer is null.");
            return;
        }

        string stableId = GetOrCreateLocalStableId();
        Debug.Log($"[SlotRegistry] EnsureLocalStableIdentity: localPlayer.ActorNumber={localPlayer.ActorNumber}, localPlayer.NickName={localPlayer.NickName}, stableId={stableId}");

        if (TryReadStringCustomProperty(localPlayer.CustomProperties, PlayerStableIdPropertyKey, out string currentStableId) &&
            string.Equals(currentStableId, stableId, StringComparison.Ordinal))
        {
            Debug.Log($"[SlotRegistry] Stable ID already set and matches: {currentStableId}");
            return;
        }

        Hashtable properties = new Hashtable
        {
            { PlayerStableIdPropertyKey, stableId }
        };

        Debug.Log($"[SlotRegistry] Setting stable ID property to: {stableId}");
        localPlayer.SetCustomProperties(properties);
    }

    public static bool EnsureSlotsAssignedForCurrentPlayers(int preferredSlotCount = 0)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("[SlotRegistry] EnsureSlotsAssigned: Not in room.");
            return false;
        }
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[SlotRegistry] EnsureSlotsAssigned: Not Master Client.");
            return false;
        }

        Dictionary<string, int> slotMap = ReadRoomSlotMap();
        bool mapChanged = false;

        Player[] players = PhotonNetwork.PlayerList;
        Debug.Log($"[SlotRegistry] EnsureSlotsAssigned: scanning {players.Length} players. Current slotMap keys count: {slotMap.Count}");
        
        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i];
            if (!TryGetPlayerIdentityKey(player, out string identityKey))
            {
                Debug.LogWarning($"[SlotRegistry] Could not get identity key for player {player.ActorNumber} (NickName: {player.NickName})");
                continue;
            }

            if (slotMap.ContainsKey(identityKey))
            {
                Debug.Log($"[SlotRegistry] Player {player.ActorNumber} ({identityKey}) already has slot: {slotMap[identityKey]}");
                continue;
            }

            int slotIndex = FindNextAvailableSlot(slotMap, preferredSlotCount);
            slotMap[identityKey] = slotIndex;
            mapChanged = true;
            Debug.Log($"[SlotRegistry] Assigned NEW slot {slotIndex} to player {player.ActorNumber} ({identityKey})");
        }

        if (!mapChanged)
        {
            Debug.Log("[SlotRegistry] EnsureSlotsAssigned: No changes to slot map.");
            return false;
        }

        string serializedMap = SerializeSlotMap(slotMap);
        Hashtable roomProperties = new Hashtable
        {
            { RoomSlotMapPropertyKey, serializedMap }
        };

        Debug.Log($"[SlotRegistry] Updating room slotMap property to: {serializedMap}");
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        return true;
    }

    public static bool TryApplyLocalPlayerSlotProperty()
    {
        if (!TryGetLocalStableSlot(out int slotIndex))
        {
            Debug.LogWarning("[SlotRegistry] TryApplyLocalPlayerSlotProperty: Could not get stable slot for local player.");
            return false;
        }

        Player localPlayer = PhotonNetwork.LocalPlayer;
        if (localPlayer == null)
        {
            return false;
        }

        if (TryReadIntCustomProperty(localPlayer.CustomProperties, PlayerSlotPropertyKey, out int currentSlot) &&
            currentSlot == slotIndex)
        {
            Debug.Log($"[SlotRegistry] Local player slot property already matches: {currentSlot}");
            return true;
        }

        Hashtable properties = new Hashtable
        {
            { PlayerSlotPropertyKey, slotIndex }
        };

        Debug.Log($"[SlotRegistry] Setting local player slot property to: {slotIndex}");
        localPlayer.SetCustomProperties(properties);
        return true;
    }

    public static bool TryGetLocalStableSlot(out int slotIndex)
    {
        slotIndex = -1;

        Player localPlayer = PhotonNetwork.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogWarning("[SlotRegistry] TryGetLocalStableSlot: localPlayer is null.");
            return false;
        }

        if (TryReadIntCustomProperty(localPlayer.CustomProperties, PlayerSlotPropertyKey, out slotIndex))
        {
            Debug.Log($"[SlotRegistry] TryGetLocalStableSlot: found {slotIndex} in player properties.");
            return slotIndex >= 0;
        }

        bool foundInMap = TryGetSlotFromRoomMap(localPlayer, out slotIndex);
        Debug.Log($"[SlotRegistry] TryGetLocalStableSlot: found in room map? {foundInMap}, slotIndex={slotIndex}");
        return foundInMap;
    }

    public static bool TryGetPlayerSlotIndex(Player player, out int slotIndex)
    {
        slotIndex = -1;

        if (player == null)
        {
            return false;
        }

        if (TryReadIntCustomProperty(player.CustomProperties, PlayerSlotPropertyKey, out slotIndex))
        {
            return slotIndex >= 0;
        }

        return TryGetSlotFromRoomMap(player, out slotIndex);
    }

    public static int GetFallbackSlotIndex(Player player)
    {
        if (player == null)
        {
            return 0;
        }

        return Mathf.Max(0, player.ActorNumber - 1);
    }

    private static bool TryGetSlotFromRoomMap(Player player, out int slotIndex)
    {
        slotIndex = -1;

        if (player == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return false;
        }

        if (!TryGetPlayerIdentityKey(player, out string identityKey))
        {
            return false;
        }

        Dictionary<string, int> slotMap = ReadRoomSlotMap();
        return slotMap.TryGetValue(identityKey, out slotIndex);
    }

    private static bool TryGetPlayerIdentityKey(Player player, out string identityKey)
    {
        identityKey = null;

        if (player == null)
        {
            return false;
        }

        if (TryReadStringCustomProperty(player.CustomProperties, PlayerStableIdPropertyKey, out string stableId) &&
            !string.IsNullOrWhiteSpace(stableId))
        {
            identityKey = "sid:" + stableId;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(player.UserId))
        {
            identityKey = "uid:" + player.UserId;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(player.NickName))
        {
            identityKey = "nick:" + player.NickName;
            return true;
        }

        return false;
    }

    private static Dictionary<string, int> ReadRoomSlotMap()
    {
        Dictionary<string, int> slotMap = new Dictionary<string, int>();

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return slotMap;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomSlotMapPropertyKey, out object rawValue))
        {
            return slotMap;
        }

        string serializedMap = rawValue as string;
        if (string.IsNullOrWhiteSpace(serializedMap))
        {
            return slotMap;
        }

        string[] entries = serializedMap.Split(';');
        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            int separatorIndex = entry.LastIndexOf('=');
            if (separatorIndex <= 0 || separatorIndex >= entry.Length - 1)
            {
                continue;
            }

            string encodedKey = entry.Substring(0, separatorIndex);
            string slotText = entry.Substring(separatorIndex + 1);

            if (!int.TryParse(slotText, out int slotIndex) || slotIndex < 0)
            {
                continue;
            }

            string identityKey = DecodeIdentityKey(encodedKey);
            if (string.IsNullOrWhiteSpace(identityKey))
            {
                continue;
            }

            if (!slotMap.ContainsKey(identityKey))
            {
                slotMap.Add(identityKey, slotIndex);
            }
        }

        return slotMap;
    }

    private static string SerializeSlotMap(Dictionary<string, int> slotMap)
    {
        if (slotMap == null || slotMap.Count == 0)
        {
            return string.Empty;
        }

        List<KeyValuePair<string, int>> entries = new List<KeyValuePair<string, int>>(slotMap);
        entries.Sort(CompareSlotEntries);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < entries.Count; i++)
        {
            KeyValuePair<string, int> entry = entries[i];
            if (i > 0)
            {
                builder.Append(';');
            }

            builder.Append(EncodeIdentityKey(entry.Key));
            builder.Append('=');
            builder.Append(entry.Value);
        }

        return builder.ToString();
    }

    private static int CompareSlotEntries(KeyValuePair<string, int> left, KeyValuePair<string, int> right)
    {
        int slotComparison = left.Value.CompareTo(right.Value);
        if (slotComparison != 0)
        {
            return slotComparison;
        }

        return string.Compare(left.Key, right.Key, StringComparison.Ordinal);
    }

    private static int FindNextAvailableSlot(Dictionary<string, int> slotMap, int preferredSlotCount)
    {
        int totalPreferredSlots = Mathf.Max(0, preferredSlotCount);
        if (totalPreferredSlots > 0)
        {
            bool[] usedSlots = new bool[totalPreferredSlots];
            foreach (int slotIndex in slotMap.Values)
            {
                if (slotIndex >= 0 && slotIndex < usedSlots.Length)
                {
                    usedSlots[slotIndex] = true;
                }
            }

            for (int slotIndex = 0; slotIndex < usedSlots.Length; slotIndex++)
            {
                if (!usedSlots[slotIndex])
                {
                    return slotIndex;
                }
            }
        }

        int nextSlot = 0;
        foreach (int slotIndex in slotMap.Values)
        {
            if (slotIndex >= nextSlot)
            {
                nextSlot = slotIndex + 1;
            }
        }

        return nextSlot;
    }

    private static string GetOrCreateLocalStableId()
    {
        if (!string.IsNullOrEmpty(localSessionStableId))
        {
            return localSessionStableId;
        }

        string stableId = PlayerPrefs.GetString(LocalStableIdPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(LocalStableIdPrefsKey, stableId);
            PlayerPrefs.Save();
        }

        string sessionSuffix = "";
        try
        {
            sessionSuffix = "_" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        }
        catch
        {
            sessionSuffix = "_" + UnityEngine.Random.Range(100000, 999999).ToString();
        }

        #if UNITY_EDITOR
        localSessionStableId = stableId + sessionSuffix + "_editor";
        #else
        localSessionStableId = stableId + sessionSuffix + "_build";
        #endif

        return localSessionStableId;
    }

    private static string EncodeIdentityKey(string identityKey)
    {
        if (string.IsNullOrWhiteSpace(identityKey))
        {
            return string.Empty;
        }

        byte[] keyBytes = Encoding.UTF8.GetBytes(identityKey);
        return Convert.ToBase64String(keyBytes);
    }

    private static string DecodeIdentityKey(string encodedIdentityKey)
    {
        if (string.IsNullOrWhiteSpace(encodedIdentityKey))
        {
            return string.Empty;
        }

        try
        {
            byte[] keyBytes = Convert.FromBase64String(encodedIdentityKey);
            return Encoding.UTF8.GetString(keyBytes);
        }
        catch (FormatException)
        {
            return string.Empty;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }

    private static bool TryReadStringCustomProperty(Hashtable customProperties, string propertyKey, out string value)
    {
        value = null;

        if (customProperties == null || string.IsNullOrWhiteSpace(propertyKey))
        {
            return false;
        }

        if (!customProperties.TryGetValue(propertyKey, out object rawValue))
        {
            return false;
        }

        value = rawValue as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryReadIntCustomProperty(Hashtable customProperties, string propertyKey, out int value)
    {
        value = -1;

        if (customProperties == null || string.IsNullOrWhiteSpace(propertyKey))
        {
            return false;
        }

        if (!customProperties.TryGetValue(propertyKey, out object rawValue))
        {
            return false;
        }

        switch (rawValue)
        {
            case int intValue:
                value = intValue;
                return true;
            case byte byteValue:
                value = byteValue;
                return true;
            case short shortValue:
                value = shortValue;
                return true;
            default:
                return false;
        }
    }
}