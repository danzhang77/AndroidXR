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
                var score = GaussianTouchModel.Score(touchPoint, candidate);
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
