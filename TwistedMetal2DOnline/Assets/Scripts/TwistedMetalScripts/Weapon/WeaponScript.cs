using UnityEngine;
using Photon.Pun;

public class WeaponScript : MonoBehaviourPun
{
    private bool picked;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked)
            return;

        PhotonView carView =
            other.GetComponent<PhotonView>();

        if (carView == null || !carView.IsMine)
            return;

        WeaponState weaponState =
            other.GetComponent<WeaponState>();

        if (weaponState == null || weaponState.HasWeapon())
            return;

        picked = true;

        photonView.RPC(
            nameof(RPC_GiveMachineGun),
            RpcTarget.AllBuffered,
            carView.ViewID
        );

        gameObject.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
        else
            photonView.RPC(
                nameof(RPC_RequestDestroy),
                RpcTarget.MasterClient
            );
    }

    [PunRPC]
    private void RPC_GiveMachineGun(int carViewId)
    {
        PhotonView carView =
            PhotonView.Find(carViewId);

        if (carView == null)
            return;

        carView.GetComponent<WeaponState>()
            ?.SetWeapon(WeaponType.MachineGun);

        carView.GetComponent<CarWeaponController>()
            ?.PickWeapon();
    }

    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}
