using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Player Ownership")]
    [SerializeField] private bool isLocalPlayer = true;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 240f;

    [Header("Weapon")]
    [SerializeField] private bool hasWeapon = false;

    private Rigidbody2D rb;

    private float moveInput;
    private float turnInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
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
        rb.velocity = transform.right * moveInput * moveSpeed;
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

        if (!isLocalPlayer && rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }

    public void PickWeapon()
    {
        hasWeapon = true;

        Debug.Log("Arma recogida");
    }

    public bool HasWeapon()
    {
        return hasWeapon;
    }
}
