using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class CarLiveOpsConfig : MonoBehaviourPun
{
    private const string RoomPropMoveSpeed = "cfg_moveSpeed";
    private const string RoomPropTurnSpeed = "cfg_turnSpeed";

    private CarMovement carMovement;

    private void Awake()
    {
        carMovement = GetComponent<CarMovement>();
    }

    public void Initialize()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomPropMoveSpeed))
        {
            ApplyFromRoomProperties();
            return;
        }

        var lom = LiveOpsManager.Instance;
        if (lom == null) return;

        if (lom.IsReady)
            ApplyLiveOpsConfig();
        else
        {
            lom.OnLiveOpsReady += ApplyLiveOpsConfig;
            Debug.Log($"[CarLiveOpsConfig] '{gameObject.name}' esperando OnLiveOpsReady.");
        }
    }

    private void OnDestroy()
    {
        if (LiveOpsManager.Instance != null)
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyLiveOpsConfig;
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

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable
            {
                { RoomPropMoveSpeed, moveSpeed },
                { RoomPropTurnSpeed, turnSpeed }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            Debug.Log($"[CarLiveOpsConfig] MasterClient publicó config en Room Properties — moveSpeed:{moveSpeed} turnSpeed:{turnSpeed}");
        }

        carMovement?.SetSpeed(moveSpeed, turnSpeed);
        Debug.Log($"[CarLiveOpsConfig] LiveOps aplicado en '{gameObject.name}' — moveSpeed:{moveSpeed} turnSpeed:{turnSpeed}");
    }

    private void ApplyFromRoomProperties()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        float moveSpeed = props.ContainsKey(RoomPropMoveSpeed) ? (float)props[RoomPropMoveSpeed] : 0f;
        float turnSpeed = props.ContainsKey(RoomPropTurnSpeed) ? (float)props[RoomPropTurnSpeed] : 0f;

        if (moveSpeed > 0f || turnSpeed > 0f)
        {
            carMovement?.SetSpeed(moveSpeed, turnSpeed);
            Debug.Log($"[CarLiveOpsConfig] Config aplicada desde Room Properties en '{gameObject.name}' — moveSpeed:{moveSpeed} turnSpeed:{turnSpeed}");
        }
        else
        {
            Debug.Log($"[CarLiveOpsConfig] Room Properties sin valores válidos, usando fallback LiveOps para '{gameObject.name}'");
            ApplyLiveOpsConfig();
        }
    }
}