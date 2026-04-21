using System;
using System.Collections.Generic;

namespace AndroidXR.LanguageModel
{
    public sealed class Vocabulary
    {
        private readonly List<string> words;
        private readonly Dictionary<string, int> ranks;

        public IReadOnlyList<string> Words => words;

        private Vocabulary(List<string> words, Dictionary<string, int> ranks)
        {
            this.words = words;
            this.ranks = ranks;
        }

        public float GetRankLogPrior(string word)
        {
            return ranks.TryGetValue(word, out var rank)
                ? -(float)Math.Log(rank + 2)
                : -(float)Math.Log(words.Count + 2);
        }

        public static Vocabulary Parse(string text)
        {
            var words = new List<string>();
            var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                var word = lines[i].Trim().ToLowerInvariant();
                if (word.Length == 0 || ranks.ContainsKey(word))
                {
                    continue;
                }

                ranks[word] = words.Count;
                words.Add(word);
            }

            return new Vocabulary(words, ranks);
        }
    }
}

