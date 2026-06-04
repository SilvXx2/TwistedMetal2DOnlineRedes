using Photon.Pun;
using UnityEngine;

internal sealed class RemoteTransformSynchronizer
{
    private readonly Transform targetTransform;
    private readonly float lerpSpeed;

    private Vector3 remotePosition;
    private Quaternion remoteRotation;
    private bool hasRemoteState;

    public RemoteTransformSynchronizer(Transform targetTransform, float lerpSpeed)
    {
        this.targetTransform = targetTransform;
        this.lerpSpeed = lerpSpeed;
    }

    public void ApplyRemoteStep(float deltaTime)
    {
        if (!hasRemoteState)
        {
            return;
        }

        targetTransform.position = Vector3.Lerp(targetTransform.position, remotePosition, lerpSpeed * deltaTime);
        targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, remoteRotation, lerpSpeed * deltaTime);
    }

    public void Serialize(PhotonStream stream)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(targetTransform.position);
            stream.SendNext(targetTransform.rotation);
            return;
        }

        remotePosition = (Vector3)stream.ReceiveNext();
        remoteRotation = (Quaternion)stream.ReceiveNext();

        if (!hasRemoteState)
        {
            targetTransform.position = remotePosition;
            targetTransform.rotation = remoteRotation;
            hasRemoteState = true;
        }
    }
}
