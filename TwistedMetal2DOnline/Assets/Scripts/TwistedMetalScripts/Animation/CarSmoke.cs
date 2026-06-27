using UnityEngine;
using Photon.Pun;

public class CarSmoke : MonoBehaviourPun
{
    [SerializeField] private GameObject smoke;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float minSpeedToShow = 0.1f;

    private Animator smokeAnimator;
    private bool smokeVisible;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (smoke != null)
        {
            smokeAnimator = smoke.GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (photonView != null && !photonView.IsMine)
            return;

        bool shouldShowSmoke = rb != null && rb.velocity.magnitude > minSpeedToShow;

        if (shouldShowSmoke == smokeVisible)
            return;

        smokeVisible = shouldShowSmoke;

        if (smokeAnimator != null)
        {
            smokeAnimator.SetBool("IsSmoking", smokeVisible);
        }
    }
}
