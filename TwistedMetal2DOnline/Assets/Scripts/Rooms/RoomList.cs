using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomList : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Room roomButtonPrefab;

    private readonly Dictionary<string, Room> roomButtons = new Dictionary<string, Room>();
    private PhotonManager photonManager;

    private void Awake()
    {
        if (contentRoot == null)
        {
            contentRoot = transform;
        }

        photonManager = PhotonManager.Instance;
    }

    public override void OnJoinedLobby()
    {
        ClearAllButtons();
    }

    public override void OnLeftLobby()
    {
        ClearAllButtons();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        if (roomButtonPrefab == null || contentRoot == null)
        {
            return;
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo roomInfo = roomList[i];

            if (roomInfo.RemovedFromList || !roomInfo.IsOpen || !roomInfo.IsVisible)
            {
                RemoveRoomButton(roomInfo.Name);
                continue;
            }

            if (!roomButtons.TryGetValue(roomInfo.Name, out Room roomButton) || roomButton == null)
            {
                roomButton = Instantiate(roomButtonPrefab, contentRoot);
                roomButtons[roomInfo.Name] = roomButton;
            }

            roomButton.Setup(roomInfo.Name, this);
        }
    }

    public void JoinRoomByName(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return;
        }

        if (photonManager == null)
        {
            photonManager = PhotonManager.Instance;
        }

        if (photonManager != null)
        {
            photonManager.JoinExistingRoom(roomName);
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    private void RemoveRoomButton(string roomName)
    {
        if (!roomButtons.TryGetValue(roomName, out Room roomButton))
        {
            return;
        }

        roomButtons.Remove(roomName);

        if (roomButton != null)
        {
            Destroy(roomButton.gameObject);
        }
    }

    private void ClearAllButtons()
    {
        foreach (Room roomButton in roomButtons.Values)
        {
            if (roomButton != null)
            {
                Destroy(roomButton.gameObject);
            }
        }

        roomButtons.Clear();
    }
}
