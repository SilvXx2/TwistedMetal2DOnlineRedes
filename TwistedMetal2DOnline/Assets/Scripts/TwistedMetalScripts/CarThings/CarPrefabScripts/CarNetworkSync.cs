using UnityEngine;
using Photon.Pun;

public class CarNetworkSync : MonoBehaviourPun, IPunObservable
{
    [Header("Network Sync")]
    [SerializeField] private float remoteLerpSpeed = 12f;

    private RemoteTransformSynchronizer remoteSynchronizer;
    private CarWeaponController weaponController;

    private void Awake()
    {
        if (photonView != null)
        {
            remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        }

        weaponController = GetComponent<CarWeaponController>();
    }

    public void ApplyRemoteUpdate(float deltaTime, float weaponAngle)
    {
        remoteSynchronizer?.ApplyRemoteStep(deltaTime);

        if (weaponController != null)
        {
            weaponController.ApplyRemoteWeaponAngle(weaponAngle);
        }
    }

    public void TickRemote(float deltaTime)
    {
        remoteSynchronizer?.ApplyRemoteStep(deltaTime);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (remoteSynchronizer == null && photonView != null)
        {
            remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        }

        remoteSynchronizer?.Serialize(stream);

        if (weaponController != null)
        {
            weaponController.SerializeWeaponData(stream);
        }
    }
}
