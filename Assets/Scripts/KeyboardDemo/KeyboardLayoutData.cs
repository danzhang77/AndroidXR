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

            const float horizontalPadding = 0.035f;
            const float verticalPadding = 0.06f;
            const float horizontalGap = 0.012f;
            const float verticalGap = 0.024f;
            const int rowCount = 4;

            var rowHeight = (1f - (2f * verticalPadding) - ((rowCount - 1) * verticalGap)) / rowCount;
            var letterWidth = (1f - (2f * horizontalPadding) - (9f * horizontalGap)) / 10f;
            var secondRowOffset = (letterWidth + horizontalGap) * 0.5f;
            var thirdRowOffset = letterWidth + horizontalGap;
            var backWidth = (2f * letterWidth) + horizontalGap;
            var spaceWidth = (4f * letterWidth) + (3f * horizontalGap);
            var sideWidth = (3f * letterWidth) + (2f * horizontalGap);

            AddRow(keys, random, 0, new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" }, KeyboardKeyKind.Character, horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, verticalGap, sigmaMin, sigmaMax, 0f);
            AddRow(keys, random, 1, new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" }, KeyboardKeyKind.Character, horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, verticalGap, sigmaMin, sigmaMax, secondRowOffset);
            AddRow(keys, random, 2, new[] { "Z", "X", "C", "V", "B", "N", "M" }, KeyboardKeyKind.Character, horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, verticalGap, sigmaMin, sigmaMax, thirdRowOffset);

            var rowY = 1f - verticalPadding - rowHeight - (2f * (rowHeight + verticalGap));
            var backX = horizontalPadding + thirdRowOffset + (7f * (letterWidth + horizontalGap));
            AddKey(keys, random, "BACK", "BACK", KeyboardKeyKind.Backspace, new Rect(backX, rowY, backWidth, rowHeight), sigmaMin, sigmaMax);

            rowY = 1f - verticalPadding - rowHeight - (3f * (rowHeight + verticalGap));
            AddKey(keys, random, "SPACE", "SPACE", KeyboardKeyKind.Space, new Rect(horizontalPadding + sideWidth + horizontalGap, rowY, spaceWidth, rowHeight), sigmaMin, sigmaMax);

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
            float verticalGap,
            float sigmaMin,
            float sigmaMax,
            float horizontalOffset)
        {
            var rowY = 1f - verticalPadding - keyHeight - (rowIndex * (keyHeight + verticalGap));

            for (var i = 0; i < labels.Count; i++)
            {
                var x = horizontalPadding + horizontalOffset + (i * (keyWidth + horizontalGap));
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
