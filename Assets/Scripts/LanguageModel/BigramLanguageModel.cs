using System;
using System.Collections.Generic;
using System.Globalization;

namespace AndroidXR.LanguageModel
{
    public sealed class BigramLanguageModel
    {
        public const string HeadToken = "_HEAD_";

        private const float MissingProbability = 0.00000001f;

        private readonly Dictionary<string, Dictionary<string, float>> logProbabilities;

        private BigramLanguageModel(Dictionary<string, Dictionary<string, float>> logProbabilities)
        {
            this.logProbabilities = logProbabilities;
        }

        public float GetLogProbability(string previousWord, string currentWord)
        {
            var previous = string.IsNullOrWhiteSpace(previousWord)
                ? HeadToken
                : previousWord.ToLowerInvariant();
            var current = currentWord.ToLowerInvariant();

            if (logProbabilities.TryGetValue(previous, out var nextWords) &&
                nextWords.TryGetValue(current, out var logProbability))
            {
                return logProbability;
            }

            return (float)Math.Log(MissingProbability);
        }

        public static BigramLanguageModel Parse(string text)
        {
            var probabilities = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split('\t');
                if (parts.Length < 3 ||
                    !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var probability) ||
                    probability <= 0f)
                {
                    continue;
                }

                var previous = parts[0].Trim().ToLowerInvariant();
                var current = parts[1].Trim().ToLowerInvariant();
                if (previous.Length == 0 || current.Length == 0)
                {
                    continue;
                }

                if (!probabilities.TryGetValue(previous, out var nextWords))
                {
                    nextWords = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                    probabilities[previous] = nextWords;
                }

                nextWords[current] = (float)Math.Log(probability);
            }

            return new BigramLanguageModel(probabilities);
        }
    }
}

