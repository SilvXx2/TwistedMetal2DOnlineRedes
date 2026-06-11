using UnityEngine;
using Photon.Pun;

public class CarController : MonoBehaviourPun, IPunObservable
{
    [Header("Player Ownership")]
    [SerializeField] private bool isLocalPlayer = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 240f;
    [SerializeField] private float moveSpeedMultiplier = 1f;

    [Header("Weapon")]
    [SerializeField] private bool hasWeapon = false;

    [Header("Network Sync")]
    [SerializeField] private float remoteLerpSpeed = 12f;
    private RemoteTransformSynchronizer remoteSynchronizer;

    private Rigidbody2D rb;

    private float moveInput;
    private float turnInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (photonView != null)
        {
            SetLocalPlayer(photonView.IsMine);
            remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        }

        // LiveOps: sobreescribir velocidades con los valores remotos si están disponibles.
        // Si LiveOpsManager no cargó todavía, se usan los valores del inspector como fallback.
        ApplyLiveOpsConfig();
    }

    private void ApplyLiveOpsConfig()
    {
        if (LiveOpsManager.Instance == null || !LiveOpsManager.Instance.IsReady)
            return;

        moveSpeed = LiveOpsManager.Instance.Config.CarMoveSpeed;
        turnSpeed = LiveOpsManager.Instance.Config.CarTurnSpeed;

        Debug.Log($"[CarController] LiveOps aplicado — moveSpeed:{moveSpeed} turnSpeed:{turnSpeed}");
    }

    private void Update()
    {
        if (photonView != null && !photonView.IsMine)
        {
            remoteSynchronizer?.ApplyRemoteStep(Time.deltaTime);
            return;
        }

        if (!isLocalPlayer)
            return;

        ReadInput();
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        Move();
        Rotate();
    }

    private void ReadInput()
    {
        moveInput = 0f;
        turnInput = 0f;

        if (Input.GetKey(KeyCode.W))
            moveInput = 1f;

        if (Input.GetKey(KeyCode.S))
            moveInput = -1f;

        if (Input.GetKey(KeyCode.A))
            turnInput = 1f;

        if (Input.GetKey(KeyCode.D))
            turnInput = -1f;
    }

    private void Move()
    {
        rb.velocity =
        transform.right *
        moveInput *
        moveSpeed *
        moveSpeedMultiplier;
    }

    private void Rotate()
    {
        if (moveInput == 0f)
        {
            rb.angularVelocity = 0f;
            return;
        }

        float rotationAmount =
            turnInput *
            turnSpeed *
            Time.fixedDeltaTime;

        rb.rotation += rotationAmount;
    }

    public void SetLocalPlayer(bool value)
    {
        isLocalPlayer = value;

        if (rb != null)
        {
            if (!isLocalPlayer)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = false;
            }
        }
    }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }

    [PunRPC]
    public void PickWeapon()
    {
        hasWeapon = true;

        Debug.Log("Arma recogida");
    }

    public bool HasWeapon()
    {
        return hasWeapon;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        remoteSynchronizer?.Serialize(stream);
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = multiplier;
    }
}
