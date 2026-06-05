using UnityEngine;

public class PickUpFloat : MonoBehaviour
{
    [SerializeField] private float moveDistance = 0.8f;
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 startPosition;
    private bool goingUp = true;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float direction = goingUp ? 1f : -1f;

        transform.position += Vector3.up * direction * moveSpeed * Time.deltaTime;

        if (transform.position.y >= startPosition.y + moveDistance)
        {
            goingUp = false;
        }

        if (transform.position.y <= startPosition.y - moveDistance)
        {
            goingUp = true;
        }
    }
}
