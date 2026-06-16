using UnityEngine;

public static class BulletExtrapolator
{
    public struct ExtrapolationResult
    {
        public Vector3 Position;
        public float RemainingLifetime;
        public bool ShouldSpawn;
    }

    /// <summary>
    /// Calculates the extrapolated position and lifetime of a bullet based on networking lag.
    /// </summary>
    /// <param name="originalPosition">The position where the bullet was fired on the sender's client.</param>
    /// <param name="direction">The direction vector of the bullet.</param>
    /// <param name="speed">The speed of the bullet (units per second).</param>
    /// <param name="originalLifetime">The original lifetime of the bullet.</param>
    /// <param name="lag">The calculated network lag in seconds.</param>
    /// <returns>An ExtrapolationResult structure containing the calculated position, remaining lifetime, and if the bullet should spawn.</returns>
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
