using UnityEngine;
using System.Collections;

public class CarPowerUpHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController carController;
    [SerializeField] private CarHealth carHealth;

    [Header("Speed Boost")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float speedDuration = 5f;

    private Coroutine speedRoutine;

    private void Awake()
    {
        if (carController == null)
            carController = GetComponent<CarController>();

        if (carHealth == null)
            carHealth = GetComponent<CarHealth>();
    }

    public void ApplyPowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.SpeedBoost:
                ApplySpeedBoost();
                break;

            case PowerUpType.Heal:
                ApplyHeal();
                break;
        }
    }

    private void ApplySpeedBoost()
    {
        if (speedRoutine != null)
            StopCoroutine(speedRoutine);

        speedRoutine =
            StartCoroutine(SpeedBoostRoutine());
    }

    private IEnumerator SpeedBoostRoutine()
    {
        carController.SetMoveSpeedMultiplier(speedMultiplier);

        Debug.Log("Velocidad aumentada");

        yield return new WaitForSeconds(speedDuration);

        carController.SetMoveSpeedMultiplier(1f);

        Debug.Log("Velocidad normal");
    }

    private void ApplyHeal()
    {
        if (carHealth == null)
            return;

        carHealth.HealToFull();

        Debug.Log("Vida recuperada al máximo");
    }
}
