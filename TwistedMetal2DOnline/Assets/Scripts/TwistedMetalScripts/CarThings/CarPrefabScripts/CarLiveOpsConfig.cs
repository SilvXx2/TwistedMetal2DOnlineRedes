using UnityEngine;

public class CarLiveOpsConfig : MonoBehaviour
{
    private CarMovement carMovement;

    private void Awake()
    {
        carMovement = GetComponent<CarMovement>();
    }

    private void OnDestroy()
    {
        if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyLiveOpsConfig;
        }
    }

    public void Initialize()
    {
        if (LiveOpsManager.Instance != null && LiveOpsManager.Instance.IsReady)
        {
            ApplyLiveOpsConfig();
        }
        else if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady += ApplyLiveOpsConfig;
            Debug.Log($"[CarLiveOpsConfig] '{gameObject.name}' esperando OnLiveOpsReady para aplicar config de velocidad.");
        }
    }

    private void ApplyLiveOpsConfig()
    {
        if (LiveOpsManager.Instance != null)
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyLiveOpsConfig;

        if (LiveOpsManager.Instance == null || !LiveOpsManager.Instance.IsReady)
        {
            Debug.Log($"[CarLiveOpsConfig] ApplyLiveOpsConfig omitido en '{gameObject.name}' — LiveOpsManager listo: {(LiveOpsManager.Instance != null && LiveOpsManager.Instance.IsReady)}");
            return;
        }

        float remoteMoveSpeed = LiveOpsManager.Instance.Config.CarMoveSpeed;
        float remoteTurnSpeed = LiveOpsManager.Instance.Config.CarTurnSpeed;

        if (carMovement != null)
        {
            carMovement.SetSpeed(remoteMoveSpeed, remoteTurnSpeed);
        }

        Debug.Log($"[CarLiveOpsConfig] LiveOps aplicado en '{gameObject.name}' — moveSpeed:{remoteMoveSpeed} turnSpeed:{remoteTurnSpeed}");
    }
}
