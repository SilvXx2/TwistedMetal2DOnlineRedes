using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviour
{
    private GameObject ownerGo;
    private int damage = 10;
    private float lifetime = 2f;

    public void Initialize(GameObject owner, int damageAmount, float bulletLifetime)
    {
        ownerGo = owner;
        damage = damageAmount;
        lifetime = bulletLifetime;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == ownerGo || (ownerGo != null && other.transform.IsChildOf(ownerGo.transform)))
            return;

        if (other.isTrigger)
            return;

        CarHealth carHealth = other.GetComponent<CarHealth>();

        if (carHealth == null)
            carHealth = other.GetComponentInParent<CarHealth>();

        if (carHealth != null)
        {
            if (carHealth.photonView != null)
            {
                carHealth.photonView.RPC("TakeDamage", RpcTarget.All, damage);
            }
            else
            {
                carHealth.TakeDamage(damage);
            }
        }
        else
        {
            ObstacleHealth obstacle = other.GetComponent<ObstacleHealth>();
            if (obstacle == null)
                obstacle = other.GetComponentInParent<ObstacleHealth>();

            if (obstacle != null)
            {
                if (obstacle.photonView != null)
                {
                    obstacle.photonView.RPC("TakeDamage", RpcTarget.All, damage);
                }
                else
                {
                    obstacle.TakeDamage(damage);
                }
            }
        }

        Destroy(gameObject);
    }
}
