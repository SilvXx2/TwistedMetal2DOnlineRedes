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

    private Rigidbody rb;
    private PhotonView photonView;
    private PlayerInputMovement localMovement;
    private RemoteTransformSynchronizer remoteSynchronizer;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>(); // agregar esto

        if (playerRenderer == null)
            playerRenderer = GetComponent<Renderer>();

        if (photonView == null) return;

        PhotonViewMovementConfigurator.Configure(photonView, this);
        PlayerOwnerColorAssigner.ApplyOwnerColor(playerRenderer, photonView);
        localMovement = new PlayerInputMovement(transform, rb, movSpeed, rotSpeed); // agregar rb
        remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
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