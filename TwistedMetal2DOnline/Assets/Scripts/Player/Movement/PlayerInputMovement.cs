using UnityEngine;

internal sealed class PlayerInputMovement
{
    private readonly Transform targetTransform;
    private readonly Rigidbody rb;
    private readonly float movementSpeed;
    private readonly float rotationSpeed;

    public PlayerInputMovement(Transform targetTransform, Rigidbody rb, float movementSpeed, float rotationSpeed)
    {
        this.targetTransform = targetTransform;
        this.rb = rb;
        this.movementSpeed = movementSpeed;
        this.rotationSpeed = rotationSpeed;
    }

    public void Tick(float deltaTime)
    {
        float rotationInput = Input.GetAxis("Horizontal");
        float movementInput = Input.GetAxis("Vertical");

        Quaternion deltaRotation = Quaternion.Euler(0f, rotationInput * rotationSpeed * deltaTime, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);

        Vector3 move = targetTransform.forward * (movementInput * movementSpeed * deltaTime);
        rb.MovePosition(rb.position + move);

        
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
