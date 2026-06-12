using UnityEngine;
using Photon.Pun;

public class WeaponScript : MonoBehaviourPun
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        CarController car =
            other.GetComponent<CarController>();

        if (car == null)
            return;

        // In a networked session, only the owner of the colliding car should trigger the pickup detection
        if (car.photonView != null && !car.photonView.IsMine)
            return;

        if (car.HasWeapon())
            return;

        // Apply pickup state to the car via RPC if in a network session, or call directly as fallback
        if (car.photonView != null)
        {
            car.photonView.RPC("PickWeapon", RpcTarget.AllBuffered);
        }
        else
        {
            car.PickWeapon();
        }

        // Sync destruction of this pickup object
        if (photonView != null)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                photonView.RPC("DestroyPickup", RpcTarget.MasterClient);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [PunRPC]
    public void DestroyPickup()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
