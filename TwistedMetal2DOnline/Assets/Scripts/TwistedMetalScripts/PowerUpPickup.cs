using UnityEngine;
using Photon.Pun;

public class PowerUpPickup : MonoBehaviourPun
{
    [SerializeField] private PowerUpType powerUpType;

    private bool wasPicked;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (wasPicked)
            return;

        PhotonView carPhotonView =
            other.GetComponent<PhotonView>();

        if (carPhotonView == null)
            return;

        if (!carPhotonView.IsMine)
            return;

        CarPowerUpHandler handler =
            other.GetComponent<CarPowerUpHandler>();

        if (handler == null)
            return;

        wasPicked = true;

        handler.ApplyPowerUp(powerUpType);

        photonView.RPC(
            nameof(RequestDestroy),
            RpcTarget.MasterClient
        );
    }

    [PunRPC]
    private void RequestDestroy()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonNetwork.Destroy(gameObject);
    }
}
