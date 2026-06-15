using UnityEngine;
using Photon.Pun;

public class CarMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 240f;

    private NitroSystem nitroSystem;
    private Rigidbody2D rb;
    private float moveInput;
    private float turnInput;

    public float MoveInput => moveInput;
    public float MoveSpeed => moveSpeed;
    public float TurnSpeed => turnSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        nitroSystem = GetComponent<NitroSystem>();
    }

    public void SetSpeed(float newMoveSpeed, float newTurnSpeed)
    {
        if (newMoveSpeed > 0f)
            moveSpeed = newMoveSpeed;

        if (newTurnSpeed > 0f)
            turnSpeed = newTurnSpeed;
    }

    public void Tick(bool isLocalPlayer, bool isInConfrontation)
    {
        if (isInConfrontation || !isLocalPlayer)
            return;

        ReadInput();

        if (nitroSystem != null)
            nitroSystem.Tick(moveInput);
    }

    public void PhysicsTick(bool isLocalPlayer, bool isInConfrontation)
    {
        if (isInConfrontation || !isLocalPlayer)
            return;

        Move();
        Rotate();
    }

    public void StopPhysics()
    {
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public void ConfigureRigidbody(bool isLocalPlayer)
    {
        if (rb == null)
        {
            Debug.LogWarning($"[CarMovement] ConfigureRigidbody en '{gameObject.name}' — rb es null!");
            return;
        }

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

    public void SetKinematic(bool value)
    {
        if (rb != null)
            rb.isKinematic = value;
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

        float rotationAmount = turnInput * turnSpeed * Time.fixedDeltaTime;
        rb.rotation += rotationAmount;
    }
}
