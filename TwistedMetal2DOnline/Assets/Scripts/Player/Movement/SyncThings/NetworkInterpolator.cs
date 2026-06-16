using Photon.Pun;
using UnityEngine;

internal sealed class NetworkInterpolator
{
    private readonly Transform targetTransform;
    private readonly float teleportThreshold;

    private Vector3 positionFrom;
    private Vector3 positionTo;
    private Quaternion rotationFrom;
    private Quaternion rotationTo;

    private float snapshotInterval;
    private float smoothedInterval;
    private float interpolationTime;
    private bool hasFirstSnapshot;
    private bool hasSecondSnapshot;

    private const float IntervalSmoothFactor = 0.3f;
    private const float DefaultInterval = 0.1f;

    public NetworkInterpolator(Transform targetTransform, float teleportThreshold = 20f)
    {
        this.targetTransform = targetTransform;
        this.teleportThreshold = teleportThreshold;
        smoothedInterval = DefaultInterval;
    }

    public void ApplyRemoteStep(float deltaTime)
    {
        if (!hasSecondSnapshot)
        {
            if (hasFirstSnapshot)
            {
                targetTransform.position = positionTo;
                targetTransform.rotation = rotationTo;
            }
            return;
        }

        interpolationTime += deltaTime;

        float t = (smoothedInterval > 0.001f)
            ? Mathf.Clamp01(interpolationTime / smoothedInterval)
            : 1f;

        targetTransform.position = Vector3.Lerp(positionFrom, positionTo, t);
        targetTransform.rotation = Quaternion.Slerp(rotationFrom, rotationTo, t);
    }

    public void Serialize(PhotonStream stream)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(targetTransform.position);
            stream.SendNext(targetTransform.rotation);
            return;
        }

        Vector3 newPosition = (Vector3)stream.ReceiveNext();
        Quaternion newRotation = (Quaternion)stream.ReceiveNext();

        if (!hasFirstSnapshot)
        {
            targetTransform.position = newPosition;
            targetTransform.rotation = newRotation;
            positionTo = newPosition;
            rotationTo = newRotation;
            hasFirstSnapshot = true;
            return;
        }

        float elapsed = interpolationTime;
        if (elapsed > 0.001f)
        {
            snapshotInterval = elapsed;
            smoothedInterval = Mathf.Lerp(smoothedInterval, snapshotInterval, IntervalSmoothFactor);
        }

        positionFrom = targetTransform.position;
        rotationFrom = targetTransform.rotation;
        positionTo = newPosition;
        rotationTo = newRotation;

        float distance = Vector3.Distance(positionFrom, positionTo);
        if (distance > teleportThreshold)
        {
            targetTransform.position = newPosition;
            targetTransform.rotation = newRotation;
            positionFrom = newPosition;
            rotationFrom = newRotation;
        }

        interpolationTime = 0f;
        hasSecondSnapshot = true;
    }
}
