using Photon.Pun;
using UnityEngine;

public class ButtonLeave : MonoBehaviourPunCallbacks
{
    public void ButtonLeaveRoom()
    {
        PhotonManager manager = PhotonManager.Instance;
        if (manager != null)
        {
            manager.LeaveCurrentRoom();
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }
}
