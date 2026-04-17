using System;
using System.Collections.Generic;
using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public enum KeyboardKeyKind
    {
        Character,
        Space,
        Backspace,
    }

    [Serializable]
    public sealed class KeyboardKeyDefinition
    {
        public string Id;
        public string Label;
        public KeyboardKeyKind Kind;
        public Rect NormalizedRect;
        public Vector2 GaussianMean;
        public Vector2 GaussianSigma;

        public Vector2 Center => new(
            NormalizedRect.x + (NormalizedRect.width * 0.5f),
            NormalizedRect.y + (NormalizedRect.height * 0.5f));
    }

    public sealed class KeyboardLayoutData
    {
        public IReadOnlyList<KeyboardKeyDefinition> Keys => keys;
        public float LetterKeyHeight { get; }

        private readonly List<KeyboardKeyDefinition> keys;

        private KeyboardLayoutData(List<KeyboardKeyDefinition> keys, float letterKeyHeight)
        {
            this.keys = keys;
            LetterKeyHeight = letterKeyHeight;
        }

        public static KeyboardLayoutData CreateDefaultLayout(
            int randomSeed,
            float sigmaMin,
            float sigmaMax)
        {
            var keys = new List<KeyboardKeyDefinition>();
            var random = new System.Random(randomSeed);

            const float horizontalPadding = 0.05f;
            const float verticalPadding = 0.07f;
            const float horizontalGap = 0.02f;
            const float verticalGap = 0.03f;
            const int rowCount = 4;

            var rowHeight = (1f - (2f * verticalPadding) - ((rowCount - 1) * verticalGap)) / rowCount;
            var letterWidth = (1f - (2f * horizontalPadding) - (4f * horizontalGap)) / 5f;
            var specialWidth = (1f - (2f * horizontalPadding) - horizontalGap) * 0.5f;

            AddRow(keys, random, 0, new[] { "Q", "W", "E", "R", "T" }, KeyboardKeyKind.Character, horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, sigmaMin, sigmaMax);
            AddRow(keys, random, 1, new[] { "A", "S", "D", "F", "G" }, KeyboardKeyKind.Character, horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, sigmaMin, sigmaMax);
            AddRow(keys, random, 2, new[] { "Z", "X", "C", "V", "B" }, KeyboardKeyKind.Character, horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, sigmaMin, sigmaMax);

            var rowY = 1f - verticalPadding - rowHeight - (3f * (rowHeight + verticalGap));
            AddKey(keys, random, "SPACE", "SPACE", KeyboardKeyKind.Space, new Rect(horizontalPadding, rowY, specialWidth, rowHeight), sigmaMin, sigmaMax);
            AddKey(keys, random, "BACK", "BACK", KeyboardKeyKind.Backspace, new Rect(horizontalPadding + specialWidth + horizontalGap, rowY, specialWidth, rowHeight), sigmaMin, sigmaMax);

            return new KeyboardLayoutData(keys, rowHeight);
        }

        private static void AddRow(
            List<KeyboardKeyDefinition> keys,
            System.Random random,
            int rowIndex,
            IReadOnlyList<string> labels,
            KeyboardKeyKind kind,
            float horizontalPadding,
            float verticalPadding,
            float keyWidth,
            float keyHeight,
            float horizontalGap,
            float sigmaMin,
            float sigmaMax)
        {
            var rowY = 1f - verticalPadding - keyHeight - (rowIndex * (keyHeight + 0.03f));

            for (var i = 0; i < labels.Count; i++)
            {
                var x = horizontalPadding + (i * (keyWidth + horizontalGap));
                AddKey(keys, random, labels[i], labels[i], kind, new Rect(x, rowY, keyWidth, keyHeight), sigmaMin, sigmaMax);
            }
        }

        private static void AddKey(
            ICollection<KeyboardKeyDefinition> keys,
            System.Random random,
            string id,
            string label,
            KeyboardKeyKind kind,
            Rect rect,
            float sigmaMin,
            float sigmaMax)
        {
            var sigmaX = Mathf.Lerp(sigmaMin, sigmaMax, (float)random.NextDouble());
            var sigmaY = Mathf.Lerp(sigmaMin, sigmaMax, (float)random.NextDouble());
            var center = new Vector2(rect.x + (rect.width * 0.5f), rect.y + (rect.height * 0.5f));

            keys.Add(new KeyboardKeyDefinition
            {
                Id = id,
                Label = label,
                Kind = kind,
                NormalizedRect = rect,
                GaussianMean = center,
                GaussianSigma = new Vector2(sigmaX, sigmaY),
            });
        }
    }
}
