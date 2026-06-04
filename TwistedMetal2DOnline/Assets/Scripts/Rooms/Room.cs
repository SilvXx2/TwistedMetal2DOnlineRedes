using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Room : MonoBehaviour
{
    [FormerlySerializedAs("Name")]
    [SerializeField] private Text roomNameText;
    [SerializeField] private TMP_Text roomNameTMPText;

    private string roomName;
    private RoomList roomList;

    public string RoomName => roomName;

    public void Setup(string newRoomName, RoomList owner)
    {
        roomName = newRoomName;
        roomList = owner;

        if (roomNameText != null)
        {
            roomNameText.text = newRoomName;
        }

        if (roomNameTMPText != null)
        {
            roomNameTMPText.text = newRoomName;
        }
    }

    public void JoinRoom()
    {
        if (roomList == null)
        {
            Debug.LogWarning("RoomList no asignado en Room.");
            return;
        }

        roomList.JoinRoomByName(roomName);
    }
}
