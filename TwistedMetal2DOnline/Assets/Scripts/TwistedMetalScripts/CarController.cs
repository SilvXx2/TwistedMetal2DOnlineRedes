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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (photonView != null)
        {
            remoteSynchronizer = new RemoteTransformSynchronizer(transform, remoteLerpSpeed);
        }
    }

    private void Start()
    {
        if (photonView != null)
        {
            SetLocalPlayer(photonView.IsMine);
        }

        InitializeWeaponObject();
    }

    private void Update()
    {
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

        if (hasWeapon && weaponObject != null)
        {
            RotateWeaponToMouse();
            HandleShootingInput();
        }
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

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = multiplier;
    }
}
