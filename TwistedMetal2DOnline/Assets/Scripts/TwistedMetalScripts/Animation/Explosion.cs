using Photon.Pun;
using UnityEngine;

public class Explosion : MonoBehaviourPun
{
    [SerializeField] private float destroyTime = 0.8f;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (PhotonNetwork.InRoom && photonView != null && photonView.InstantiationId > 0)
        {
            if (photonView.IsMine)
            {
                if (animator != null)
                {
                    animator.SetBool("Play", true);
                }
                StartCoroutine(DestroyAfterTimeNetworked());
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("Play", true);
            }
            Destroy(gameObject, destroyTime);
        }
    }

    private System.Collections.IEnumerator DestroyAfterTimeNetworked()
    {
        yield return new WaitForSeconds(destroyTime);
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
