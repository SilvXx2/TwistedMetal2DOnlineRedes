using UnityEngine;
using Photon.Pun;

public class RayGunPickup : MonoBehaviourPun
{
    private bool picked;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked)
            return;

        PhotonView carView = other.GetComponent<PhotonView>();

        if (carView == null || !carView.IsMine)
            return;

        WeaponState weaponState =
            other.GetComponent<WeaponState>();

        if (weaponState == null || weaponState.HasWeapon())
            return;

        picked = true;

        photonView.RPC(
            nameof(RPC_GiveRayGun),
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
    private void RPC_GiveRayGun(int carViewId)
    {
        PhotonView carView = PhotonView.Find(carViewId);

        if (carView == null)
            return;

        WeaponState weaponState =
            carView.GetComponent<WeaponState>();

        RayGunWeapon rayGun =
            carView.GetComponent<RayGunWeapon>();

        weaponState?.SetWeapon(WeaponType.RayGun);
        rayGun?.Activate();
    }

    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);
    }
}
