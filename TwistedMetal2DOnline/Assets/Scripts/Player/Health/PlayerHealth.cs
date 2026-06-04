using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerDamageInvulnerability))]
[RequireComponent(typeof(PlayerDamageBlink))]
[RequireComponent(typeof(PlayerKnockback))]
public class PlayerHealth : MonoBehaviourPun
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField, Min(0.01f)] private float fallbackBlinkDuration = 0.15f;

    private int currentHealth;
    private bool isDead = false;
    private PlayerDamageBlink damageBlink;
    private PlayerDamageInvulnerability damageInvulnerability;
    private PlayerKnockback knockback;

    public int CurrentHealth => currentHealth;
    public float HealthPercent => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;

        damageBlink = GetComponent<PlayerDamageBlink>();
        if (damageBlink == null)
        {
            damageBlink = GetComponentInChildren<PlayerDamageBlink>(true);
        }

        damageInvulnerability = GetComponent<PlayerDamageInvulnerability>();
        if (damageInvulnerability == null)
        {
            damageInvulnerability = GetComponentInChildren<PlayerDamageInvulnerability>(true);
        }

        knockback = GetComponent<PlayerKnockback>();
        if (knockback == null)
        {
            knockback = gameObject.AddComponent<PlayerKnockback>();
        }
    }

    [PunRPC]
    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        if (damageInvulnerability != null && !damageInvulnerability.CanReceiveDamage)
        {
            return;
        }

        if (damageInvulnerability != null)
        {
            damageInvulnerability.Trigger();
        }

        if (damageBlink != null)
        {
            float blinkDuration = damageInvulnerability != null
                ? damageInvulnerability.Duration
                : fallbackBlinkDuration;
            damageBlink.Play(blinkDuration);
        }

        currentHealth -= amount;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
