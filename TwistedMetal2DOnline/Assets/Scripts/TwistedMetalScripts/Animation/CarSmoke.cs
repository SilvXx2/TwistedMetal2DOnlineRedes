using UnityEngine;
using Photon.Pun;

public class CarSmoke : MonoBehaviourPun
{
    [SerializeField] private GameObject smoke;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float minSpeedToShow = 0.1f;

    private bool smokeVisible;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!photonView.IsMine)
            return;

        bool shouldShowSmoke = rb != null && rb.velocity.magnitude > minSpeedToShow;

        if (shouldShowSmoke == smokeVisible)
            return;

        smokeVisible = shouldShowSmoke;

        photonView.RPC(
            nameof(RPC_SetSmokeVisible),
            RpcTarget.All,
            smokeVisible
        );
    }

    [PunRPC]
    private void RPC_SetSmokeVisible(bool visible)
    {
        if (smoke != null)
            smoke.SetActive(visible);
    }
}
