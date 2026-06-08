using UnityEngine;

public class PickUpFloat : MonoBehaviour
{
    [SerializeField] private float moveDistance = 0.8f;
    [SerializeField] private float moveSpeed = 2f;

    /// <summary>
    /// Intervalo de reaparición en segundos. LiveOps puede modificar este valor
    /// mediante MapPartitionLoader. El sistema de spawning externo debe leer esta propiedad.
    /// </summary>
    public float SpawnInterval { get; private set; } = 5f;

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

    /// <summary>
    /// Permite que el sistema LiveOps sobreescriba el intervalo de spawn de este pickup.
    /// </summary>
    public void SetSpawnInterval(float interval)
    {
        SpawnInterval = Mathf.Max(0.1f, interval);
    }
}
