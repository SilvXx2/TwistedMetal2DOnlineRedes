using UnityEngine;

public class WeaponScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        CarController car =
            other.GetComponent<CarController>();

        if (car == null)
            return;

        if (car.HasWeapon())
            return;

        car.PickWeapon();

        Destroy(gameObject);
    }
}
