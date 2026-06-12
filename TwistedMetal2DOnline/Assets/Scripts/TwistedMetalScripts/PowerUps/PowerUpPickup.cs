using UnityEngine;
using Photon.Pun;

public class PowerUpPickup : MonoBehaviourPun
{
    [SerializeField] private PowerUpType powerUpType;
    [SerializeField] private float invulnerabilityDuration = 4f;

    private bool wasPicked;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (wasPicked)
            return;

        PhotonView carPhotonView = other.GetComponent<PhotonView>();

        if (carPhotonView == null)
            return;

        if (!carPhotonView.IsMine)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        wasPicked = true;

        ApplyPowerUp(playerHealth);

        photonView.RPC(nameof(RequestDestroy), RpcTarget.MasterClient);
    }

    private void ApplyPowerUp(PlayerHealth playerHealth)
    {
        switch (powerUpType)
        {
            case PowerUpType.FullHeal:
                playerHealth.photonView.RPC("FullHeal", RpcTarget.All);
                break;

            case PowerUpType.Invulnerability:
                playerHealth.photonView.RPC(
                    "SetPowerUpInvulnerability",
                    RpcTarget.All,
                    invulnerabilityDuration
                );
                break;
        }
    }

    [PunRPC]
    private void RequestDestroy()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonNetwork.Destroy(gameObject);
    }
}
