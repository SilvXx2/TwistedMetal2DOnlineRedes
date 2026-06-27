using Photon.Pun;
using UnityEngine;

public class ObstacleHealth : MonoBehaviourPun
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private string destructionEffectPath = "Effects/Demolition";

    private int currentHealth;
    private bool isDestroyed = false;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    [PunRPC]
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            isDestroyed = true;

            PlayDestructionEffect();

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    private void PlayDestructionEffect()
    {
        if (string.IsNullOrEmpty(destructionEffectPath)) return;

        GameObject effectPrefab = Resources.Load<GameObject>(destructionEffectPath);
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, transform.position, Quaternion.identity);
        }
    }
}
