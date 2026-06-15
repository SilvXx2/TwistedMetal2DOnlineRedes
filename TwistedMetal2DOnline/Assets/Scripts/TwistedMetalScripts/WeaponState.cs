using UnityEngine;
using Photon.Pun;

public class WeaponState : MonoBehaviourPun
{
    [SerializeField] private WeaponType currentWeapon = WeaponType.None;

    public bool HasWeapon()
    {
        return currentWeapon != WeaponType.None;
    }

    public WeaponType CurrentWeapon()
    {
        return currentWeapon;
    }

    [PunRPC]
    public void SetWeapon(WeaponType weaponType)
    {
        currentWeapon = weaponType;
    }
}
