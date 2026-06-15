using UnityEngine;
using Photon.Pun;

public class CarConfrontation : MonoBehaviourPun
{
    private CarController carController;
    private NitroSystem nitroSystem;

    private void Awake()
    {
        carController = GetComponent<CarController>();
        nitroSystem = GetComponent<NitroSystem>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!photonView.IsMine) return;
        if (carController.IsInConfrontation) return;

        CarController otherCar = collision.gameObject.GetComponent<CarController>();
        if (otherCar == null) return;
        if (otherCar.IsInConfrontation) return;
        if (nitroSystem == null || !nitroSystem.IsNitroActive) return;

        NitroSystem otherNitro = otherCar.GetComponent<NitroSystem>();
        if (otherNitro == null || !otherNitro.IsNitroActive) return;

        float dot = Vector2.Dot(transform.right, otherCar.transform.right);
        if (dot > -0.6f) return;

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_StartConfrontation), RpcTarget.All, otherCar.photonView.ViewID);
        else
            StartConfrontationLocal(otherCar);
    }

    [PunRPC]
    private void RPC_StartConfrontation(int otherViewId)
    {
        PhotonView otherView = PhotonView.Find(otherViewId);
        if (otherView == null) return;

        CarController otherCar = otherView.GetComponent<CarController>();
        if (otherCar == null) return;

        StartConfrontationLocal(otherCar);
    }

    private void StartConfrontationLocal(CarController otherCar)
    {
        ConfrontationManager.Instance?.StartConfrontation(carController, otherCar);
    }

    public void SendConfrontationScore(float score, bool pressedSpace)
    {
        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_SubmitConfrontationScore), RpcTarget.All, score, pressedSpace);
        else
            ConfrontationManager.Instance?.OnScoreSubmitted(photonView.ViewID, score, pressedSpace);
    }

    [PunRPC]
    private void RPC_SubmitConfrontationScore(float score, bool pressedSpace)
    {
        ConfrontationManager.Instance?.OnScoreSubmitted(photonView.ViewID, score, pressedSpace);
    }

    public void TransformToPedestrian(GameObject pedestrianPrefab, float spawnOffset)
    {
        if (pedestrianPrefab == null)
        {
            Debug.LogError("[CarConfrontation] TransformToPedestrian: pedestrianPrefab is null!");
            return;
        }

        Vector3 spawnPos = transform.position - transform.right * spawnOffset;
        spawnPos.z = -1f;

        WeaponState weaponState = GetComponent<WeaponState>();
        CarWeaponController weaponController = GetComponent<CarWeaponController>();
        WeaponType activeWeapon = weaponState != null
            ? weaponState.CurrentWeapon()
            : (weaponController != null && weaponController.HasWeapon() ? WeaponType.MachineGun : WeaponType.None);

        if (PhotonNetwork.InRoom)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
                PhotonNetwork.Instantiate(pedestrianPrefab.name, spawnPos, Quaternion.identity, 0, new object[] { (int)activeWeapon });
            }
        }
        else
        {
            Destroy(gameObject);
            GameObject pedestrianGo = Instantiate(pedestrianPrefab, spawnPos, Quaternion.identity);
            pedestrianGo.GetComponent<PedestrianController>()?.SetHadWeaponType(activeWeapon);
        }
    }
}