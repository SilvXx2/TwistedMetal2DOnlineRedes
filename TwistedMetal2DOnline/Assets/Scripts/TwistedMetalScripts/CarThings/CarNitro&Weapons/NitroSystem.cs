using UnityEngine;
using Photon.Pun;

public class NitroSystem : MonoBehaviourPun, IPunObservable
{
    [Header("Nitro Settings")]
    [SerializeField] private float maxNitro = 100f;
    [SerializeField] private float consumeRate = 30f;
    [SerializeField] private float rechargeRate = 15f;
    [SerializeField] private float nitroMultiplier = 1.5f;
    [SerializeField] private KeyCode nitroKey = KeyCode.LeftShift;

    private float currentNitro;
    private bool isNitroActive;

    public float NitroPercent => maxNitro > 0f ? currentNitro / maxNitro : 0f;
    public bool IsNitroActive => isNitroActive;
    public bool CanUseNitro => currentNitro > 0f;
    public float NitroSpeedMultiplier => nitroMultiplier;

    private void Awake()
    {
        currentNitro = maxNitro;
    }

    public void Tick(float moveInput)
    {
        if (photonView != null && !photonView.IsMine)
            return;

        isNitroActive = Input.GetKey(nitroKey) && moveInput > 0f && currentNitro > 0f;

        if (isNitroActive)
        {
            currentNitro -= consumeRate * Time.deltaTime;
            if (currentNitro < 0f)
                currentNitro = 0f;
        }
        else
        {
            currentNitro += rechargeRate * Time.deltaTime;
            if (currentNitro > maxNitro)
                currentNitro = maxNitro;
        }
    }

    [PunRPC]
    public void FullNitroRecharge()
    {
        currentNitro = maxNitro;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentNitro);
            stream.SendNext(isNitroActive);
        }
        else
        {
            currentNitro = (float)stream.ReceiveNext();
            isNitroActive = (bool)stream.ReceiveNext();
        }
    }
}
