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
        remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        weaponController = GetComponent<CarWeaponController>();
    }

    public void ApplyRemoteUpdate(float deltaTime, float weaponAngle)
    {
        remoteSynchronizer?.ApplyRemoteStep(deltaTime);
        weaponController?.ApplyRemoteWeaponAngle(weaponAngle);
    }

    public void TickRemote(float deltaTime)
    {
        remoteSynchronizer?.ApplyRemoteStep(deltaTime);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        remoteSynchronizer?.Serialize(stream);
        weaponController?.SerializeWeaponData(stream);
    }
}