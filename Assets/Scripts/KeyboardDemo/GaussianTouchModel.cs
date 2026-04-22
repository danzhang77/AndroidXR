using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public static class GaussianTouchModel
    {
        public static float Score(Vector2 touchPoint, KeyboardKeyDefinition key)
        {
            var sigmaX = Mathf.Max(Mathf.Abs(key.GaussianSigma.x), 0.0001f);
            var sigmaY = Mathf.Max(Mathf.Abs(key.GaussianSigma.y), 0.0001f);
            var rho = Mathf.Clamp(key.GaussianRho, -0.99f, 0.99f);
            var dx = touchPoint.x - key.GaussianMean.x;
            var dy = touchPoint.y - key.GaussianMean.y;
            var oneMinusRhoSquared = Mathf.Max(1f - (rho * rho), 0.0001f);

            var z =
                ((dx * dx) / (sigmaX * sigmaX)) -
                ((2f * rho * dx * dy) / (sigmaX * sigmaY)) +
                ((dy * dy) / (sigmaY * sigmaY));

            return -z / (2f * oneMinusRhoSquared);
        }
    }
}

