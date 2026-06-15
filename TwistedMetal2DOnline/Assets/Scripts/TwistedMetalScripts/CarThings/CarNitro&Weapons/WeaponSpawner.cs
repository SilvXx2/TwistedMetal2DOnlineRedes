using UnityEngine;
using Photon.Pun;

public class WeaponSpawner : MonoBehaviourPunCallbacks
{
    [Header("Weapon Prefabs")]
    public GameObject rayGunPrefab;
    public GameObject machineGunPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        SpawnWeapons();
    }

    private void SpawnWeapons()
    {
        if (spawnPoints.Length < 4)
        {
            Debug.LogError("Necesitas al menos 4 spawn points.");
            return;
        }

        PhotonNetwork.InstantiateRoomObject(
        "Weapons/" + rayGunPrefab.name,
         spawnPoints[0].position,
         Quaternion.identity
         );

        PhotonNetwork.InstantiateRoomObject(
        "Weapons/" + rayGunPrefab.name,
        spawnPoints[1].position,
        Quaternion.identity
        );

        PhotonNetwork.InstantiateRoomObject(
        "Weapons/" + machineGunPrefab.name,
        spawnPoints[2].position,
        Quaternion.identity
        );

        PhotonNetwork.InstantiateRoomObject(
        "Weapons/" + machineGunPrefab.name,
        spawnPoints[3].position,
        Quaternion.identity
        );
    }
}
