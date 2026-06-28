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
    private bool hasPassword;
    private RoomList roomList;

    public string RoomName => roomName;
    public bool HasPassword => hasPassword;

    public void Setup(string newRoomName, bool roomHasPassword, RoomList owner)
    {
        roomName = newRoomName;
        hasPassword = roomHasPassword;
        roomList = owner;

        string displayName = roomHasPassword ? $"🔒 {newRoomName}" : newRoomName;

        if (roomNameText != null)
        {
            roomNameText.text = displayName;
        }

        if (roomNameTMPText != null)
        {
            roomNameTMPText.text = displayName;
        }
    }

    public void JoinRoom()
    {
        if (roomList == null)
        {
            Debug.LogWarning("RoomList no asignado en Room.");
            return;
        }

        roomList.JoinRoomByName(roomName, hasPassword);
    }
}
