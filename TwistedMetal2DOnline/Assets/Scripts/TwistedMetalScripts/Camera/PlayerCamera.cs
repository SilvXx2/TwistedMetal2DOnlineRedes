using UnityEngine;
using Photon.Pun;

public class PlayerCamera : MonoBehaviourPun
{
    [SerializeField] private bool isLocalPlayer = true;

    private void Start()
    {
        if (photonView != null)
        {
            isLocalPlayer = photonView.IsMine;
        }
    }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }

    public void SetLocalPlayer(bool value)
    {
        isLocalPlayer = value;
    }
}
