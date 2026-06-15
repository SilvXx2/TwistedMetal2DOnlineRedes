using UnityEngine;
using Photon.Pun;

public class CarController : MonoBehaviourPun
{
    [Header("Player Ownership")]
    [SerializeField] private bool isLocalPlayer = true;

    private CarMovement movement;
    private CarWeaponController weaponController;
    private CarConfrontation confrontation;
    private CarNetworkSync networkSync;
    private CarLiveOpsConfig liveOpsConfig;

    private bool isInConfrontation;
    public bool IsInConfrontation
    {
        get => isInConfrontation;
        set
        {
            isInConfrontation = value;
            if (movement != null)
            {
                if (isInConfrontation)
                {
                    movement.StopPhysics();
                    movement.SetKinematic(true);
                }
                else
                {
                    movement.SetKinematic(!isLocalPlayer);
                }
            }
        }
    }

    private void Awake()
    {
        movement        = GetComponent<CarMovement>();
        weaponController = GetComponent<CarWeaponController>();
        confrontation   = GetComponent<CarConfrontation>();
        networkSync     = GetComponent<CarNetworkSync>();
        liveOpsConfig   = GetComponent<CarLiveOpsConfig>();
    }

    private void Start()
    {
        Debug.Log($"[CarController] Start en GameObject '{gameObject.name}' — photonView.IsMine: {photonView.IsMine}");
        SetLocalPlayer(photonView.IsMine);

        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
        {
            object data = photonView.InstantiationData[0];
            if (data is bool b)
                weaponController?.SetInitialWeapon(b ? WeaponType.MachineGun : WeaponType.None);
            else if (data is int i)
                weaponController?.SetInitialWeapon((WeaponType)i);
        }

        weaponController?.Initialize(weaponController.HasWeapon());
        liveOpsConfig?.Initialize();
    }

    private void Update()
    {
        if (isInConfrontation)
        {
            movement?.StopPhysics();
            return;
        }

        if (!photonView.IsMine)
        {
            networkSync?.TickRemote(Time.deltaTime);
            weaponController?.ApplyRemoteWeaponAngle(weaponController.WeaponAngle);
            return;
        }

        if (!isLocalPlayer) return;

        movement?.Tick(isLocalPlayer, isInConfrontation);
        weaponController?.Tick(isLocalPlayer);
    }

    private void FixedUpdate()
    {
        movement?.PhysicsTick(isLocalPlayer, isInConfrontation);
    }

    public void SetLocalPlayer(bool value)
    {
        isLocalPlayer = value;
        Debug.Log($"[CarController] SetLocalPlayer({value}) en '{gameObject.name}' — isLocalPlayer ahora es: {isLocalPlayer}");
        movement?.ConfigureRigidbody(isLocalPlayer);
    }

    public bool IsLocalPlayer() => isLocalPlayer;

    public void SendConfrontationScore(float score, bool pressedSpace) =>
        confrontation?.SendConfrontationScore(score, pressedSpace);

    public void TransformToPedestrian(GameObject pedestrianPrefab, float spawnOffset) =>
        confrontation?.TransformToPedestrian(pedestrianPrefab, spawnOffset);

    public void SetInitialWeapon(WeaponType weaponType) =>
        weaponController?.SetInitialWeapon(weaponType);

    [PunRPC]
    public void PickWeapon() => weaponController?.PickWeapon();

    public bool HasWeapon() => weaponController != null && weaponController.HasWeapon();
}