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
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyLiveOpsConfig;
    }

    public void Initialize()
    {
        var lom = LiveOpsManager.Instance;
        if (lom == null) return;

        if (lom.IsReady)
            ApplyLiveOpsConfig();
        else
        {
            lom.OnLiveOpsReady += ApplyLiveOpsConfig;
            Debug.Log($"[CarLiveOpsConfig] '{gameObject.name}' esperando OnLiveOpsReady para aplicar config de velocidad.");
        }
    }

    private void ApplyLiveOpsConfig()
    {
        var lom = LiveOpsManager.Instance;

        if (lom != null)
            lom.OnLiveOpsReady -= ApplyLiveOpsConfig;

        if (lom == null || !lom.IsReady)
        {
            Debug.Log($"[CarLiveOpsConfig] ApplyLiveOpsConfig omitido en '{gameObject.name}' — LiveOpsManager listo: {lom?.IsReady ?? false}");
            return;
        }

        float moveSpeed = lom.Config.CarMoveSpeed;
        float turnSpeed = lom.Config.CarTurnSpeed;
        carMovement?.SetSpeed(moveSpeed, turnSpeed);
        Debug.Log($"[CarLiveOpsConfig] LiveOps aplicado en '{gameObject.name}' — moveSpeed:{moveSpeed} turnSpeed:{turnSpeed}");
    }
}