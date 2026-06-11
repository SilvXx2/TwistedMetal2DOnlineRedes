using UnityEngine;
using Photon.Pun;

public class CarHealth : MonoBehaviourPun
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 200;
    [SerializeField] private int currentHealth = 100;

    private void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    [PunRPC]
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth < 0)
            currentHealth = 0;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    private void Die()
    {
        Debug.Log("Auto destruido");

        gameObject.SetActive(false);
    }
}