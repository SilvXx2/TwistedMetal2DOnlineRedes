using UnityEngine;

internal sealed class PlayerInputMovement
{
    private readonly Transform targetTransform;
    private readonly Rigidbody rb;
    private readonly float movementSpeed;
    private readonly float rotationSpeed;
    private readonly float nitroMultiplier;
    private readonly KeyCode nitroKey;
    
    private bool isNitroActive;

    public bool IsNitroActive => isNitroActive;

    public PlayerInputMovement(Transform targetTransform, Rigidbody rb, float movementSpeed, float rotationSpeed, float nitroMultiplier, KeyCode nitroKey)
    {
        this.targetTransform = targetTransform;
        this.rb = rb;
        this.movementSpeed = movementSpeed;
        this.rotationSpeed = rotationSpeed;
        this.nitroMultiplier = nitroMultiplier;
        this.nitroKey = nitroKey;
    }

    public void Tick(float deltaTime)
    {
        float rotationInput = Input.GetAxis("Horizontal");
        float movementInput = Input.GetAxis("Vertical");

        isNitroActive = Input.GetKey(nitroKey);

        Quaternion deltaRotation = Quaternion.Euler(0f, rotationInput * rotationSpeed * deltaTime, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);

        float currentSpeed = movementSpeed;
        if (isNitroActive && movementInput > 0f)
        {
            currentSpeed *= nitroMultiplier;
        }

        Vector3 move = targetTransform.forward * (movementInput * currentSpeed * deltaTime);
        rb.MovePosition(rb.position + move);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
