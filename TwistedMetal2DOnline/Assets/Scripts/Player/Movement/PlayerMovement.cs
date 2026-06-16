using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private float movSpeed       = 3f;
    [SerializeField] private float rotSpeed       = 120f;
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private float remoteLerpSpeed = 12f;

    [Header("Nitro Settings")]
    [SerializeField] private float nitroMultiplier = 1.5f;
    [SerializeField] private KeyCode nitroKey = KeyCode.LeftShift;

    private bool remoteNitroActive;

    public bool IsNitroActive => photonView != null && photonView.IsMine 
        ? (localMovement != null && localMovement.IsNitroActive) 
        : remoteNitroActive;

    private Rigidbody rb;
    private PhotonView photonView;
    private PlayerInputMovement localMovement;
    private NetworkInterpolator remoteSynchronizer;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>(); // agregar esto

        if (playerRenderer == null)
            playerRenderer = GetComponent<Renderer>();

        if (photonView == null) return;

        PhotonViewMovementConfigurator.Configure(photonView, this);
        PlayerOwnerColorAssigner.ApplyOwnerColor(playerRenderer, photonView);
        localMovement = new PlayerInputMovement(transform, rb, movSpeed, rotSpeed, nitroMultiplier, nitroKey); // agregar rb y nitro
        remoteSynchronizer = new NetworkInterpolator(transform);
    }

    private void Update()
    {
        if (remoteSynchronizer == null) return;

        if (!photonView.IsMine)
        {
            remoteSynchronizer.ApplyRemoteStep(Time.deltaTime);
            return;
        }

        localMovement.Tick(Time.deltaTime);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (remoteSynchronizer == null) return;
        remoteSynchronizer.Serialize(stream);

        if (stream.IsWriting)
        {
            stream.SendNext(localMovement != null && localMovement.IsNitroActive);
        }
        else
        {
            if (stream.Count >= 3)
            {
                remoteNitroActive = (bool)stream.ReceiveNext();
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (photonView == null || photonView.Owner == null)
        {
            return;
        }

        if (targetPlayer == null || photonView.Owner.ActorNumber != targetPlayer.ActorNumber || changedProps == null)
        {
            return;
        }

        if (changedProps.ContainsKey(PhotonPlayerSlotRegistry.PlayerSlotPropertyKey) ||
            changedProps.ContainsKey(PhotonPlayerSlotRegistry.PlayerStableIdPropertyKey))
        {
            PlayerOwnerColorAssigner.ApplyOwnerColor(playerRenderer, photonView);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (photonView == null || photonView.Owner == null || propertiesThatChanged == null)
        {
            return;
        }

        if (propertiesThatChanged.ContainsKey(PhotonPlayerSlotRegistry.RoomSlotMapPropertyKey))
        {
            PlayerOwnerColorAssigner.ApplyOwnerColor(playerRenderer, photonView);
        }
    }
}