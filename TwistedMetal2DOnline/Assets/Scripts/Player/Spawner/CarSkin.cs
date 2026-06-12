using UnityEngine;
using Photon.Pun;

public class CarSkin : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private SpriteRenderer carSpriteRenderer;
    [SerializeField] private Sprite[] carSprites;

    private int skinIndex;

    private void Awake()
    {
        if (carSpriteRenderer == null)
            carSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (photonView != null && photonView.IsMine)
        {
            int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            SetSkin(actorNumber - 1);

            photonView.RPC(nameof(RPC_SetSkin), RpcTarget.AllBuffered, skinIndex);
        }
    }

    public void SetSkin(int index)
    {
        if (carSprites == null || carSprites.Length == 0)
            return;

        skinIndex = index % carSprites.Length;

        if (carSpriteRenderer != null)
            carSpriteRenderer.sprite = carSprites[skinIndex];
    }

    [PunRPC]
    private void RPC_SetSkin(int index)
    {
        SetSkin(index);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(skinIndex);
        }
        else
        {
            skinIndex = (int)stream.ReceiveNext();
            SetSkin(skinIndex);
        }
    }
}
