using UnityEngine;
using Photon.Pun;

public class CarWeaponController : MonoBehaviourPun
{
    [Header("Weapon")]
    [SerializeField] private Sprite weaponSprite;
    [SerializeField] private Vector3 weaponLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 weaponLocalScale = Vector3.one;
    [SerializeField] private float weaponAngleOffset = 0f;

    [Header("Bullet Settings")]
    [SerializeField] private Sprite bulletCustomSprite;
    [SerializeField] private float bulletSpeed = 500f;
    [SerializeField] private int bulletDamage = 15;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private float bulletSpawnOffset = 8f;
    [SerializeField] private Vector3 bulletScale = new Vector3(10f, 10f, 1f);

    private GameObject weaponObject;
    private SpriteRenderer weaponSpriteRenderer;
    private float weaponAngle;
    private float nextFireTime = 0f;
    private bool hasWeapon = false;
    private static Sprite defaultBulletSprite;

    public float WeaponAngle => weaponAngle;
    public bool HasWeaponEquipped => hasWeapon;

    public void Initialize(bool startWithWeapon)
    {
        hasWeapon = startWithWeapon;
        InitializeWeaponObject();
    }

    public void Tick(bool isLocalPlayer)
    {
        if (!isLocalPlayer)
            return;

        if (hasWeapon && weaponObject != null)
        {
            RotateWeaponToMouse();
            HandleShootingInput();
        }
    }

    public void ApplyRemoteWeaponAngle(float angle)
    {
        weaponAngle = angle;
        if (weaponObject != null && weaponObject.activeSelf)
        {
            weaponObject.transform.rotation = Quaternion.Euler(0f, 0f, weaponAngle);
        }
    }

    public void SerializeWeaponData(PhotonStream stream)
    {
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

    [PunRPC]
    public void PickWeapon()
    {
        hasWeapon = true;
        if (weaponObject != null)
        {
            weaponObject.SetActive(true);
        }

        WeaponState weaponState = GetComponent<WeaponState>();
        if (weaponState != null)
        {
            weaponState.SetWeapon(WeaponType.MachineGun);
        }

        Debug.Log("Arma recogida");
    }

    public void SetInitialWeapon(WeaponType weaponType)
    {
        hasWeapon = (weaponType == WeaponType.MachineGun);
        SetInitialWeaponState(weaponType);
    }

    public bool HasWeapon()
    {
        return hasWeapon;
    }

    private void SetInitialWeaponState(WeaponType weaponType)
    {
        WeaponState weaponState = GetComponent<WeaponState>();
        if (weaponState != null)
        {
            weaponState.SetWeapon(weaponType);
        }

        if (weaponType == WeaponType.RayGun)
        {
            RayGunWeapon rayGun = GetComponent<RayGunWeapon>();
            if (rayGun != null)
            {
                rayGun.Activate();
            }
        }
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
}
