using UnityEngine;

public class VehicleStatsAdapter : MonoBehaviour, IVehicleStats
{
    private PlayerHealth playerHealth;
    private NitroSystem nitroSystem;

    public float HealthPercent => playerHealth != null ? playerHealth.HealthPercent : 1f;
    public float NitroPercent => nitroSystem != null ? nitroSystem.NitroPercent : 0f;
    public bool IsNitroActive => nitroSystem != null && nitroSystem.IsNitroActive;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        nitroSystem = GetComponent<NitroSystem>();
    }
}
