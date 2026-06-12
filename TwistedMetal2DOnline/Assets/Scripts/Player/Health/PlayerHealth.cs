using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerDamageInvulnerability))]
[RequireComponent(typeof(PlayerDamageBlink))]
[RequireComponent(typeof(PlayerKnockback))]
public class PlayerHealth : MonoBehaviourPun
{
    public static PlayerHealth LocalPlayerInstance { get; private set; }

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
        if (photonView != null && photonView.IsMine)
        {
            LocalPlayerInstance = this;
        }

        // LiveOps: sobreescribir maxHealth con el valor remoto si está disponible.
        // Si LiveOpsManager no cargó todavía, se usa el valor del inspector como fallback y se subscribe.
        if (LiveOpsManager.Instance != null && LiveOpsManager.Instance.IsReady)
        {
            ApplyLiveOpsConfig();
        }
        else if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady += ApplyLiveOpsConfig;
        }

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

    private void ApplyLiveOpsConfig()
    {
        if (LiveOpsManager.Instance != null)
        {
            int oldMax = maxHealth;
            maxHealth = Mathf.Max(1, LiveOpsManager.Instance.Config.PlayerMaxHealth);
            if (currentHealth == oldMax)
            {
                currentHealth = maxHealth;
            }
            Debug.Log($"[PlayerHealth] LiveOps aplicado — maxHealth:{maxHealth}");
        }
    }

    private void OnDestroy()
    {
        if (LocalPlayerInstance == this)
        {
            LocalPlayerInstance = null;
        }
        if (LiveOpsManager.Instance != null)
        {
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyLiveOpsConfig;
        }
    }

    [PunRPC]
    public void TakeDamage(int amount, int attackerActorNumber)
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
            if (PhotonNetwork.InRoom)
            {
                if (photonView != null && photonView.IsMine)
                {
                    int deaths = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Deaths") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Deaths"] : 0;
                    ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
                    hash["Deaths"] = deaths + 1;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
                }

                if (PhotonNetwork.LocalPlayer.ActorNumber == attackerActorNumber)
                {
                    int kills = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Kills") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Kills"] : 0;
                    int score = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Score") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Score"] : 0;
                    ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
                    hash["Kills"] = kills + 1;
                    hash["Score"] = score + 100;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

                    int tauntId = UnityEngine.Random.Range(1, 6);
                    string victimName = (photonView != null && photonView.Owner != null) ? photonView.Owner.NickName : "El rival";
                    string attackerName = PhotonNetwork.LocalPlayer.NickName;

                    if (photonView != null && photonView.Owner != null && string.IsNullOrEmpty(victimName))
                        victimName = $"Jugador {photonView.Owner.ActorNumber}";
                    if (string.IsNullOrEmpty(attackerName))
                        attackerName = $"Jugador {PhotonNetwork.LocalPlayer.ActorNumber}";

                    TauntList.Instance.ObtenerTauntPorId(tauntId, (tauntText) =>
                    {
                        if (LocalPlayerInstance != null && LocalPlayerInstance.photonView != null)
                        {
                            LocalPlayerInstance.photonView.RPC("RPC_ShowTaunt", RpcTarget.All, attackerName, victimName, tauntText);
                        }
                        else
                        {
                            if (TauntNotifier.Instance != null)
                            {
                                TauntNotifier.Instance.ShowTaunt(attackerName, victimName, tauntText);
                            }
                        }
                    });
                }
            }
            else
            {
                // Single-player / Offline fallback
                if (photonView == null || photonView.IsMine)
                {
                    int tauntId = UnityEngine.Random.Range(1, 6);
                    TauntList.Instance.ObtenerTauntPorId(tauntId, (tauntText) =>
                    {
                        if (TauntNotifier.Instance != null)
                        {
                            TauntNotifier.Instance.ShowTaunt("El rival", "Tú", tauntText);
                        }
                    });
                }
                else
                {
                    int tauntId = UnityEngine.Random.Range(1, 6);
                    TauntList.Instance.ObtenerTauntPorId(tauntId, (tauntText) =>
                    {
                        if (TauntNotifier.Instance != null)
                        {
                            TauntNotifier.Instance.ShowTaunt("Tú", "El rival", tauntText);
                        }
                    });
                }
            }

            Die();
        }
    }

    [PunRPC]
    public void RPC_ShowTaunt(string attackerName, string victimName, string tauntText)
    {
        Debug.Log($"[Taunt RPC] {attackerName} -> {victimName}: {tauntText}");
        if (TauntNotifier.Instance != null)
        {
            TauntNotifier.Instance.ShowTaunt(attackerName, victimName, tauntText);
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
