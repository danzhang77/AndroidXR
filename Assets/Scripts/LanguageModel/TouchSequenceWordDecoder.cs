using System.Collections.Generic;
using AndroidXR.KeyboardDemo;
using UnityEngine;

namespace AndroidXR.LanguageModel
{
    public sealed class TouchSequenceWordDecoder
    {
        private readonly Vocabulary vocabulary;
        private readonly BigramLanguageModel bigramLanguageModel;
        private readonly float touchWeight;
        private readonly float unigramWeight;
        private readonly float bigramWeight;

        public TouchSequenceWordDecoder(
            Vocabulary vocabulary,
            BigramLanguageModel bigramLanguageModel,
            float touchWeight,
            float unigramWeight,
            float bigramWeight)
        {
            this.vocabulary = vocabulary;
            this.bigramLanguageModel = bigramLanguageModel;
            this.touchWeight = touchWeight;
            this.unigramWeight = unigramWeight;
            this.bigramWeight = bigramWeight;
        }

        public WordDecodeResult Decode(
            IReadOnlyList<KeyboardKeyDefinition> keys,
            IReadOnlyList<Vector2> touchSequence,
            string previousWord,
            string fallbackWord)
        {
            var results = DecodeTop(keys, touchSequence, previousWord, fallbackWord, 1);
            return results.Count > 0
                ? results[0]
                : new WordDecodeResult(fallbackWord, 0f, false);
        }

        public IReadOnlyList<WordDecodeResult> DecodeTop(
            IReadOnlyList<KeyboardKeyDefinition> keys,
            IReadOnlyList<Vector2> touchSequence,
            string previousWord,
            string fallbackWord,
            int maxResults)
        {
            var results = new List<WordDecodeResult>();
            if (touchSequence.Count == 0 || maxResults <= 0)
            {
                return results;
            }

            var keyByCharacter = BuildCharacterKeyMap(keys);

            for (var i = 0; i < vocabulary.Words.Count; i++)
            {
                var word = vocabulary.Words[i];
                if (word.Length != touchSequence.Count)
                {
                    continue;
                }

                var touchScore = ScoreTouchSequence(word, touchSequence, keyByCharacter);
                if (float.IsNegativeInfinity(touchScore))
                {
                    continue;
                }

                var score =
                    (touchWeight * touchScore) +
                    (unigramWeight * vocabulary.GetRankLogPrior(word)) +
                    (bigramWeight * bigramLanguageModel.GetLogProbability(previousWord, word));

                AddRankedResult(results, new WordDecodeResult(word, score, true), maxResults);
            }

            if (results.Count == 0 && !string.IsNullOrEmpty(fallbackWord))
            {
                results.Add(new WordDecodeResult(fallbackWord, 0f, false));
            }

            return results;
        }

        private static void AddRankedResult(List<WordDecodeResult> results, WordDecodeResult candidate, int maxResults)
        {
            var insertIndex = results.Count;
            for (var i = 0; i < results.Count; i++)
            {
                if (candidate.Score > results[i].Score)
                {
                    insertIndex = i;
                    break;
                }
            }

            if (insertIndex >= maxResults)
            {
                return;
            }

            results.Insert(insertIndex, candidate);
            if (results.Count > maxResults)
            {
                results.RemoveAt(results.Count - 1);
            }
        }

        private static Dictionary<char, KeyboardKeyDefinition> BuildCharacterKeyMap(IReadOnlyList<KeyboardKeyDefinition> keys)
        {
            var map = new Dictionary<char, KeyboardKeyDefinition>();
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                if (key.Kind != KeyboardKeyKind.Character || string.IsNullOrEmpty(key.Label))
                {
                    continue;
                }

                map[char.ToLowerInvariant(key.Label[0])] = key;
            }

            return map;
        }

        private static float ScoreTouchSequence(
            string word,
            IReadOnlyList<Vector2> touchSequence,
            IReadOnlyDictionary<char, KeyboardKeyDefinition> keyByCharacter)
        {
            var score = 0f;

            for (var i = 0; i < word.Length; i++)
            {
                var character = char.ToLowerInvariant(word[i]);
                if (!keyByCharacter.TryGetValue(character, out var key))
                {
                    return float.NegativeInfinity;
                }

                var touchPoint = touchSequence[i];
                score += GaussianTouchModel.Score(touchPoint, key);
            }

            return score;
        }
    }
}
