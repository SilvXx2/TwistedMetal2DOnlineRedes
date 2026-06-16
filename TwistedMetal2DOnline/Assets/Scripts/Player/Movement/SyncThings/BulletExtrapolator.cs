using UnityEngine;

public static class BulletExtrapolator
{
    public struct ExtrapolationResult
    {
        public Vector3 Position;
        public float RemainingLifetime;
        public bool ShouldSpawn;
    }

    public static ExtrapolationResult Calculate(Vector3 originalPosition, Vector3 direction, float speed, float originalLifetime, float lag)
    {
        ExtrapolationResult result;

        if (lag <= 0f)
        {
            result.Position = originalPosition;
            result.RemainingLifetime = originalLifetime;
            result.ShouldSpawn = true;
            return result;
        }

        if (lag >= originalLifetime)
        {
            result.Position = originalPosition;
            result.RemainingLifetime = 0f;
            result.ShouldSpawn = false;
            return result;
        }

        result.Position = originalPosition + direction * speed * lag;
        result.RemainingLifetime = originalLifetime - lag;
        result.ShouldSpawn = true;
        return result;
    }
}
