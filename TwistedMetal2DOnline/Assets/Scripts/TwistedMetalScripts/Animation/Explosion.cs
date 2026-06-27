using Photon.Pun;
using UnityEngine;

public class Explosion : MonoBehaviourPun
{
    [SerializeField] private float destroyTime = 0.8f;

    private void Start()
    {
        // Si fue instanciado a través de la red (PhotonNetwork.Instantiate)
        if (PhotonNetwork.InRoom && photonView != null && photonView.InstantiationId > 0)
        {
            if (photonView.IsMine)
            {
                StartCoroutine(DestroyAfterTimeNetworked());
            }
        }
        else
        {
            // Si fue instanciado localmente (mediante Instantiate común de Unity)
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
