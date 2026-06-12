using UnityEngine;
using Photon.Pun;

public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        SpawnPowerUps();
    }

    private void SpawnPowerUps()
    {
        foreach (Transform spawnPoint in spawnPoints)
        {
            int randomIndex = Random.Range(0, powerUpPrefabs.Length);

            PhotonNetwork.InstantiateRoomObject(
                "PowerUps/" + powerUpPrefabs[randomIndex].name,
                spawnPoint.position,
                Quaternion.identity
            );
        }
    }
}
