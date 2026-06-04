using UnityEngine;
using Photon.Pun;

public class SpearDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 50;
    public float hitCooldown = 1f;

    [Header("Shield Clash")]
    [SerializeField] private bool enableShieldClashKnockback = true;
    [SerializeField, Min(0f)] private float shieldClashCooldown = 0.35f;

    private float lastHitTime = 0f;
    private float lastShieldClashTime = -999f;
    private PhotonView ownerPhotonView;
    private Transform ownerRoot;

    private void Awake()
    {
        ownerPhotonView = GetComponentInParent<PhotonView>();
        ownerRoot = ownerPhotonView != null ? ownerPhotonView.transform : transform.root;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ownerPhotonView != null && !ownerPhotonView.IsMine)
            return;

        if (other.transform.root == transform.root)
            return;

        if (other.CompareTag("Shield"))
        {
            TryApplyShieldClashKnockback(other);
            return;
        }

        ObstacleHealth obstacle = other.GetComponentInParent<ObstacleHealth>();

        if (obstacle != null)
        {
            obstacle.photonView.RPC("TakeDamage", RpcTarget.All, damage);
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();

            if (health != null && Time.time - lastHitTime > hitCooldown)
            {
                lastHitTime = Time.time;
                health.photonView.RPC(nameof(PlayerHealth.TakeDamage), RpcTarget.AllViaServer, damage);
            }
        }
    }

    private void TryApplyShieldClashKnockback(Collider shieldCollider)
    {
        if (!enableShieldClashKnockback)
        {
            return;
        }

        if (Time.time - lastShieldClashTime < shieldClashCooldown)
        {
            return;
        }

        Transform defenderRoot = shieldCollider.transform.root;
        if (defenderRoot == null || defenderRoot == ownerRoot)
        {
            return;
        }

        PlayerKnockback attackerKnockback = ownerRoot != null ? ownerRoot.GetComponent<PlayerKnockback>() : null;
        PlayerKnockback defenderKnockback = defenderRoot.GetComponent<PlayerKnockback>();

        if (attackerKnockback == null || defenderKnockback == null)
        {
            return;
        }

        lastShieldClashTime = Time.time;

        attackerKnockback.RequestApplyBackward();
        defenderKnockback.RequestApplyBackward();
    }
}