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

    // Sincronización de posición para clientes remotos
    private Vector3 remotePos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();
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

            // Bloquear la cámara en este peatón
            CameraFollow camFollow = FindObjectOfType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTarget(transform);
            }
        }
        else
        {
            // Peatón remoto es kinematic
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
            // Interpolación de posición para peatones remotos
            transform.position = Vector3.Lerp(transform.position, remotePos, Time.deltaTime * 12f);
            return;
        }

        // Si el peatón está muerto, no hacer nada (dejar que PlayerHealth maneje la muerte)
        if (health != null && health.IsDead)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            return;
        }

        // Movimiento simple 2D con WASD / Flechas
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 movement = new Vector2(moveX, moveY).normalized * moveSpeed;

        if (rb != null)
        {
            rb.velocity = movement;
        }

        // Temporizador para volver al auto
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            RespawnAsCar();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLocal) return;

        // Si choca contra un coche, muere de inmediato
        CarController car = collision.gameObject.GetComponent<CarController>();
        if (car != null && health != null && !health.IsDead)
        {
            if (photonView != null)
            {
                // Enviar daño masivo por RPC
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
        spawnPos.z = 0f; // Force car Z position to 0

        if (PhotonNetwork.InRoom)
        {
            // Destruir peatón
            PhotonNetwork.Destroy(gameObject);

            // Spawnear el auto
            PhotonNetwork.Instantiate(carPrefabName, spawnPos, Quaternion.identity, 0, new object[] { (int)hadWeaponType });
        }
        else
        {
            // Fallback offline
            Destroy(gameObject);
            GameObject spawnedCar = Instantiate(Resources.Load<GameObject>(carPrefabName), spawnPos, Quaternion.identity);
            
            CarController cc = spawnedCar.GetComponent<CarController>();
            if (cc != null)
            {
                cc.SetLocalPlayer(true);
                cc.SetInitialWeapon(hadWeaponType);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
        }
        else
        {
            remotePos = (Vector3)stream.ReceiveNext();
        }
    }
}
