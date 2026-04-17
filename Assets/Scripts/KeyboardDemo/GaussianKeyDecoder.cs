using System.Collections.Generic;
using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public readonly struct KeyDecodeResult
    {
        public KeyboardKeyDefinition Key { get; }
        public float Score { get; }
        public float DistanceFromCenter { get; }
        public bool IsCommitted { get; }

        public KeyDecodeResult(KeyboardKeyDefinition key, float score, float distanceFromCenter, bool isCommitted)
        {
            Key = key;
            Score = score;
            DistanceFromCenter = distanceFromCenter;
            IsCommitted = isCommitted;
        }
    }

    public sealed class GaussianKeyDecoder
    {
        public KeyDecodeResult Decode(IReadOnlyList<KeyboardKeyDefinition> keys, Vector2 touchPoint, float commitDistanceThreshold)
        {
            KeyboardKeyDefinition bestKey = null;
            var bestScore = float.NegativeInfinity;
            var bestDistance = float.PositiveInfinity;

            for (var i = 0; i < keys.Count; i++)
            {
                var candidate = keys[i];
                var dx = touchPoint.x - candidate.GaussianMean.x;
                var dy = touchPoint.y - candidate.GaussianMean.y;

                var exponent = -0.5f * (
                    ((dx * dx) / Mathf.Max(candidate.GaussianSigma.x * candidate.GaussianSigma.x, 0.0001f)) +
                    ((dy * dy) / Mathf.Max(candidate.GaussianSigma.y * candidate.GaussianSigma.y, 0.0001f)));

                var score = exponent;
                var distance = Vector2.Distance(touchPoint, candidate.Center);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestKey = candidate;
                    bestDistance = distance;
                }
            }

            var committed = bestKey != null && bestDistance <= commitDistanceThreshold;
            return new KeyDecodeResult(bestKey, bestScore, bestDistance, committed);
        }
    }
}
