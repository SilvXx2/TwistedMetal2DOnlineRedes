using UnityEngine;
using Photon.Pun;

public class CarController : MonoBehaviourPun, IPunObservable
{
    [Header("Player Ownership")]
    [SerializeField] private bool isLocalPlayer = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 240f;

    private NitroSystem nitroSystem;

    [Header("Weapon")]
    [SerializeField] private bool hasWeapon = false;
    [SerializeField] private Sprite weaponSprite;
    [SerializeField] private Vector3 weaponLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 weaponLocalScale = Vector3.one;
    [SerializeField] private float weaponAngleOffset = 0f;

    private GameObject weaponObject;
    private SpriteRenderer weaponSpriteRenderer;
    private float weaponAngle;

    [Header("Bullet Settings")]
    [SerializeField] private Sprite bulletCustomSprite;
    [SerializeField] private float bulletSpeed = 500f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float bulletSpawnOffset = 8f;
    [SerializeField] private Vector3 bulletScale = new Vector3(10f, 10f, 1f);

    private float nextFireTime = 0f;
    private static Sprite defaultBulletSprite;

    [Header("Network Sync")]
    [SerializeField] private float remoteLerpSpeed = 12f;
    private RemoteTransformSynchronizer remoteSynchronizer;

    private Rigidbody2D rb;

    private float moveInput;
    private float turnInput;

    private bool isInConfrontation;
    public bool IsInConfrontation
    {
        get => isInConfrontation;
        set
        {
            isInConfrontation = value;
            if (rb != null)
            {
                if (isInConfrontation)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.isKinematic = true;
                }
                else
                {
                    rb.isKinematic = !isLocalPlayer;
                }
            }
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        nitroSystem = GetComponent<NitroSystem>();
        if (photonView != null)
        {
            remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        }
    }

    private void Start()
    {
        if (photonView != null)
        {
            Debug.Log($"[CarController] Start en GameObject '{gameObject.name}' — photonView.IsMine: {photonView.IsMine}");
            SetLocalPlayer(photonView.IsMine);
        }
        else
        {
            Debug.LogWarning($"[CarController] Start en GameObject '{gameObject.name}' — photonView es null!");
        }

        InitializeWeaponObject();

        // LiveOps: sobreescribir velocidades con los valores remotos si están disponibles.
        // Si LiveOpsManager no cargó todavía, se usan los valores del inspector como fallback.
        ApplyLiveOpsConfig();
    }

    private void ApplyLiveOpsConfig()
    {
        if (LiveOpsManager.Instance == null || !LiveOpsManager.Instance.IsReady)
        {
            Debug.Log($"[CarController] ApplyLiveOpsConfig omitido en '{gameObject.name}' — LiveOpsManager listo: {(LiveOpsManager.Instance != null && LiveOpsManager.Instance.IsReady)}");
            return;
        }

        float remoteMoveSpeed = LiveOpsManager.Instance.Config.CarMoveSpeed;
        float remoteTurnSpeed = LiveOpsManager.Instance.Config.CarTurnSpeed;

        if (remoteMoveSpeed > 0f)
        {
            moveSpeed = remoteMoveSpeed;
        }
        else
        {
            Debug.LogWarning($"[CarController] Valor de moveSpeed remoto inválido o cero ({remoteMoveSpeed}) para '{gameObject.name}', se mantiene el valor local: {moveSpeed}");
        }

        if (remoteTurnSpeed > 0f)
        {
            turnSpeed = remoteTurnSpeed;
        }
        else
        {
            Debug.LogWarning($"[CarController] Valor de turnSpeed remoto inválido o cero ({remoteTurnSpeed}) para '{gameObject.name}', se mantiene el valor local: {turnSpeed}");
        }

        Debug.Log($"[CarController] LiveOps aplicado en '{gameObject.name}' — moveSpeed:{moveSpeed} turnSpeed:{turnSpeed}");
    }

    private void Update()
    {
        if (isInConfrontation)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            return;
        }

        if (photonView != null && !photonView.IsMine)
        {
            remoteSynchronizer?.ApplyRemoteStep(Time.deltaTime);
            if (weaponObject != null && weaponObject.activeSelf)
            {
                weaponObject.transform.rotation = Quaternion.Euler(0f, 0f, weaponAngle);
            }
            return;
        }

        if (!isLocalPlayer)
            return;

        ReadInput();

        if (nitroSystem != null)
            nitroSystem.Tick(moveInput);

        if (hasWeapon && weaponObject != null)
        {
            RotateWeaponToMouse();
            HandleShootingInput();
        }
    }

    private void FixedUpdate()
    {
        if (isInConfrontation || !isLocalPlayer)
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
        float currentSpeed = moveSpeed;
        if (nitroSystem != null && nitroSystem.IsNitroActive && nitroSystem.CanUseNitro && moveInput > 0f)
        {
            currentSpeed *= nitroSystem.NitroSpeedMultiplier;
        }
        rb.velocity = transform.right * moveInput * currentSpeed;
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
        Debug.Log($"[CarController] SetLocalPlayer({value}) en '{gameObject.name}' — isLocalPlayer ahora es: {isLocalPlayer}");

        if (rb != null)
        {
            if (!isLocalPlayer)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.isKinematic = true;
                Debug.Log($"[CarController] '{gameObject.name}' Rigidbody2D configurado como KINEMATIC (jugador remoto).");
            }
            else
            {
                rb.isKinematic = false;
                Debug.Log($"[CarController] '{gameObject.name}' Rigidbody2D configurado como DINÁMICO (jugador local).");
            }
        }
        else
        {
            Debug.LogWarning($"[CarController] SetLocalPlayer en '{gameObject.name}' — rb (Rigidbody2D) es null!");
        }
    }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }

    private void InitializeWeaponObject()
    {
        if (weaponObject != null)
            return;

        weaponObject = new GameObject("CarWeapon");
        weaponObject.transform.SetParent(transform, false);
        weaponObject.transform.localPosition = weaponLocalPosition;
        weaponObject.transform.localScale = weaponLocalScale;

        weaponSpriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
        weaponSpriteRenderer.sprite = weaponSprite;
        weaponSpriteRenderer.sortingOrder = 5;

        weaponObject.SetActive(hasWeapon);
    }

    private void RotateWeaponToMouse()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        Vector3 mouseWorldPosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        Vector3 direction = mouseWorldPosition - weaponObject.transform.position;
        weaponAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + weaponAngleOffset;

        weaponObject.transform.rotation = Quaternion.Euler(0f, 0f, weaponAngle);
    }

    private void HandleShootingInput()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector3 dir = Quaternion.Euler(0, 0, weaponAngle) * Vector3.right;
        Vector3 spawnPos = weaponObject.transform.position + dir * bulletSpawnOffset;

        if (photonView != null && PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_Shoot", RpcTarget.All, spawnPos, weaponAngle);
        }
        else
        {
            RPC_Shoot(spawnPos, weaponAngle);
        }
    }

    [PunRPC]
    private void RPC_Shoot(Vector3 position, float angle)
    {
        GameObject bulletGo = new GameObject("Bullet");
        bulletGo.transform.position = position;
        bulletGo.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        bulletGo.transform.localScale = bulletScale;

        SpriteRenderer sr = bulletGo.AddComponent<SpriteRenderer>();
        sr.sprite = bulletCustomSprite != null ? bulletCustomSprite : GetDefaultBulletSprite();
        sr.sortingOrder = 6;

        Rigidbody2D bulletRb = bulletGo.AddComponent<Rigidbody2D>();
        bulletRb.gravityScale = 0f;
        bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D col = bulletGo.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Bullet bulletComponent = bulletGo.AddComponent<Bullet>();
        bulletComponent.Initialize(gameObject, bulletDamage, bulletLifetime);

        Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        bulletRb.velocity = direction * bulletSpeed;
    }

    private static Sprite GetDefaultBulletSprite()
    {
        if (defaultBulletSprite == null)
        {
            Texture2D texture = new Texture2D(8, 4);
            Color[] colors = new Color[32];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.yellow;
            }
            texture.SetPixels(colors);
            texture.Apply();
            defaultBulletSprite = Sprite.Create(texture, new Rect(0, 0, 8, 4), new Vector2(0.5f, 0.5f));
        }
        return defaultBulletSprite;
    }

    [PunRPC]
    public void PickWeapon()
    {
        hasWeapon = true;
        if (weaponObject != null)
        {
            weaponObject.SetActive(true);
        }

        Debug.Log("Arma recogida");
    }

    public bool HasWeapon()
    {
        return hasWeapon;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (remoteSynchronizer == null && photonView != null)
        {
            remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        }

        remoteSynchronizer?.Serialize(stream);
        if (stream.IsWriting)
        {
            stream.SendNext(hasWeapon);
            stream.SendNext(weaponAngle);
        }
        else
        {
            bool previousHasWeapon = hasWeapon;
            if (stream.Count >= 4)
            {
                hasWeapon = (bool)stream.ReceiveNext();
                weaponAngle = (float)stream.ReceiveNext();

                if (hasWeapon != previousHasWeapon && weaponObject != null)
                {
                    weaponObject.SetActive(hasWeapon);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (photonView == null || !photonView.IsMine)
            return;

        if (isInConfrontation)
            return;

        CarController otherCar = collision.gameObject.GetComponent<CarController>();
        if (otherCar == null)
            return;

        if (otherCar.IsInConfrontation)
            return;

        if (nitroSystem == null || !nitroSystem.IsNitroActive)
            return;

        NitroSystem otherNitro = otherCar.GetComponent<NitroSystem>();
        if (otherNitro == null || !otherNitro.IsNitroActive)
            return;

        // Must be a head-on collision (facing opposite directions, dot product < -0.6f)
        float dot = Vector2.Dot(transform.right, otherCar.transform.right);
        if (dot > -0.6f)
            return;

        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_StartConfrontation", RpcTarget.All, otherCar.photonView.ViewID);
        }
        else
        {
            StartConfrontationLocal(otherCar);
        }
    }

    [PunRPC]
    private void RPC_StartConfrontation(int otherViewId)
    {
        PhotonView otherView = PhotonView.Find(otherViewId);
        if (otherView == null) return;

        CarController otherCar = otherView.GetComponent<CarController>();
        if (otherCar == null) return;

        StartConfrontationLocal(otherCar);
    }

    private void StartConfrontationLocal(CarController otherCar)
    {
        if (ConfrontationManager.Instance != null)
        {
            ConfrontationManager.Instance.StartConfrontation(this, otherCar);
        }
    }

    public void SendConfrontationScore(float score)
    {
        if (photonView != null && PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_SubmitConfrontationScore", RpcTarget.All, score);
        }
        else
        {
            if (ConfrontationManager.Instance != null)
            {
                ConfrontationManager.Instance.OnScoreSubmitted(photonView != null ? photonView.ViewID : 0, score);
            }
        }
    }

    [PunRPC]
    private void RPC_SubmitConfrontationScore(float score)
    {
        if (ConfrontationManager.Instance != null)
        {
            ConfrontationManager.Instance.OnScoreSubmitted(photonView.ViewID, score);
        }
    }
}
