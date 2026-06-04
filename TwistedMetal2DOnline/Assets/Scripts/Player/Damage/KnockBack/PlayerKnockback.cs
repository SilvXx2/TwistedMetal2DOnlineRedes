using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerKnockback : MonoBehaviourPun
{
    [SerializeField, Min(0f)] private float knockbackDistance = 0.75f;

    public void RequestApplyBackward()
    {
        if (knockbackDistance <= 0f)
        {
            return;
        }

        if (photonView != null)
        {
            if (photonView.IsMine)
            {
                ApplyBackwardInternal();
                return;
            }

            if (photonView.Owner != null)
            {
                photonView.RPC(nameof(RpcApplyBackward), photonView.Owner);
                return;
            }
        }

        ApplyBackwardInternal();
    }

    [PunRPC]
    private void RpcApplyBackward()
    {
        ApplyBackwardInternal();
    }

    private void ApplyBackwardInternal()
    {
        if (knockbackDistance <= 0f)
        {
            return;
        }

        Vector3 backward = -transform.forward;
        backward.y = 0f;

        if (backward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        backward.Normalize();
        transform.position += backward * knockbackDistance;
    }
}