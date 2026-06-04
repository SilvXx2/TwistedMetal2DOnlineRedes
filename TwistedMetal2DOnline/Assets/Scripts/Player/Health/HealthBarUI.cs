using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);

    private Transform target;

    void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }

        if (slider == null)
        {
            slider = GetComponentInChildren<Slider>();
        }

        if (playerHealth != null)
        {
            target = playerHealth.transform;
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
        }
    }

    void Update()
    {
        if (playerHealth != null && slider != null)
        {
            slider.value = playerHealth.HealthPercent;
        }

        if (target != null)
        {
            transform.position = target.position + offset;
        }

        if (Camera.main != null)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }
}
