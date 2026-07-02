using UnityEngine;
using Photon.Pun;

public class PedestrianController : MonoBehaviourPun, IPunObservable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Second Chance Settings")]
    [SerializeField] private float returnToCarDelay = 3.0f;
    [SerializeField] private string carPrefabName = "CAR";

    private Rigidbody2D rb;
    private PlayerHealth health;
    private float timer;
    private bool isLocal;
    private WeaponType hadWeaponType = WeaponType.None;
    private Animator animator;

    private NetworkInterpolator remoteInterpolator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();
        remoteInterpolator = new NetworkInterpolator(transform);
        animator = GetComponent<Animator>();
    }

    public void SetHadWeapon(bool value)
    {
        hadWeaponType = value ? WeaponType.MachineGun : WeaponType.None;
    }

    public void SetHadWeaponType(WeaponType value)
    {
        hadWeaponType = value;
    }

    private void Start()
    {
        isLocal = photonView == null || photonView.IsMine;

        if (photonView != null && photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
        {
            object data = photonView.InstantiationData[0];
            if (data is bool b)
            {
                hadWeaponType = b ? WeaponType.MachineGun : WeaponType.None;
            }
            else if (data is int i)
            {
                hadWeaponType = (WeaponType)i;
            }
        }

        if (isLocal)
        {
            timer = returnToCarDelay;

            
            CameraFollow camFollow = FindObjectOfType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTarget(transform);
            }
        }
        else
        {
            
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    private void Update()
    {
        if (!isLocal)
        {
            remoteInterpolator.ApplyRemoteStep(Time.deltaTime);
            return;
        }

        
        if (health != null && health.IsDead)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveX, moveY).normalized * moveSpeed;

        if (rb != null)
        {
            rb.velocity = movement;
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", movement != Vector2.zero);
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            RespawnAsCar();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLocal) return;

        
        CarController car = collision.gameObject.GetComponent<CarController>();
        if (car != null && health != null && !health.IsDead)
        {
            if (photonView != null)
            {
                
                health.photonView.RPC("TakeDamage", RpcTarget.All, 999, car.photonView.OwnerActorNr);
            }
            else
            {
                health.TakeDamage(999, 0);
            }
        }
    }

    private void RespawnAsCar()
    {
        if (photonView != null && !photonView.IsMine) return;

        Vector3 spawnPos = transform.position;
        spawnPos.z = 0f; 

        if (PhotonNetwork.InRoom)
        {
            
            PhotonNetwork.Destroy(gameObject);

            
            PhotonNetwork.Instantiate(carPrefabName, spawnPos, Quaternion.identity, 0, new object[] { (int)hadWeaponType });
        }
        else
        {
            
            Destroy(gameObject);
            GameObject spawnedCar = Instantiate(Resources.Load<GameObject>(carPrefabName), spawnPos, Quaternion.identity);
            
            CarController cc = spawnedCar.GetComponent<CarController>();
            if (cc != null)
            {
                cc.SetLocalPlayer(true);
            }

            CarWeaponController weaponCtrl = spawnedCar.GetComponent<CarWeaponController>();
            if (weaponCtrl != null)
            {
                weaponCtrl.SetInitialWeapon(hadWeaponType);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        remoteInterpolator.Serialize(stream);
    }
}
