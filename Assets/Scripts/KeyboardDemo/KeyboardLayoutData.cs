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
        public float GaussianRho;

        public Vector2 Center => new(
            NormalizedRect.x + (NormalizedRect.width * 0.5f),
            NormalizedRect.y + (NormalizedRect.height * 0.5f));
    }

    public sealed class KeyboardLayoutData
    {
        // Pixel-space touch parameters are normalized against the source key size,
        // then scaled into each key's normalized rectangle.
        // Pixel Y is treated as screen-style downward, while normalized keyboard Y is upward.
        private const float SourceKeyWidthPixels = 108f;
        private const float SourceKeyHeightPixels = 135f;

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
                GaussianMean = ResolveGaussianMean(label, kind, rect, center),
                GaussianSigma = ResolveGaussianSigma(label, kind, rect, sigmaX, sigmaY),
                GaussianRho = ResolveGaussianRho(label, kind),
            });
        }

        private static Vector2 ResolveGaussianMean(
            string label,
            KeyboardKeyKind kind,
            Rect rect,
            Vector2 center)
        {
            if (!TryGetLetterParameters(label, kind, out var parameters))
            {
                return center;
            }

            return new Vector2(
                center.x + ((parameters.MeanOffsetPixels.x / SourceKeyWidthPixels) * rect.width),
                center.y - ((parameters.MeanOffsetPixels.y / SourceKeyHeightPixels) * rect.height));
        }

        private static Vector2 ResolveGaussianSigma(
            string label,
            KeyboardKeyKind kind,
            Rect rect,
            float fallbackSigmaX,
            float fallbackSigmaY)
        {
            if (!TryGetLetterParameters(label, kind, out var parameters))
            {
                return new Vector2(fallbackSigmaX, fallbackSigmaY);
            }

            return new Vector2(
                (Mathf.Abs(parameters.SigmaPixels.x) / SourceKeyWidthPixels) * rect.width,
                (Mathf.Abs(parameters.SigmaPixels.y) / SourceKeyHeightPixels) * rect.height);
        }

        private static float ResolveGaussianRho(string label, KeyboardKeyKind kind)
        {
            return TryGetLetterParameters(label, kind, out var parameters) ? parameters.Rho : 0f;
        }

        private static bool TryGetLetterParameters(string label, KeyboardKeyKind kind, out LetterTouchParameters parameters)
        {
            parameters = default;
            if (kind != KeyboardKeyKind.Character || string.IsNullOrEmpty(label))
            {
                return false;
            }

            return LetterTouchParametersByCharacter.TryGetValue(char.ToLowerInvariant(label[0]), out parameters);
        }

        private readonly struct LetterTouchParameters
        {
            public Vector2 MeanOffsetPixels { get; }
            public Vector2 SigmaPixels { get; }
            public float Rho { get; }

            public LetterTouchParameters(float meanX, float meanY, float sigmaX, float sigmaY, float rho)
            {
                MeanOffsetPixels = new Vector2(meanX, meanY);
                SigmaPixels = new Vector2(sigmaX, sigmaY);
                Rho = rho;
            }
        }

        private static readonly Dictionary<char, LetterTouchParameters> LetterTouchParametersByCharacter = new Dictionary<char, LetterTouchParameters>
        {
            ['a'] = new LetterTouchParameters(-31.93f, -103.32f, 40.41f, 65.54f, 0.05f),
            ['b'] = new LetterTouchParameters(-101.43f, -31.05f, 69.12f, 55.39f, -0.24f),
            ['c'] = new LetterTouchParameters(-38.52f, -36.09f, 60.31f, 53.46f, -0.11f),
            ['d'] = new LetterTouchParameters(-19.49f, -104.38f, 63.11f, 58.96f, -0.05f),
            ['e'] = new LetterTouchParameters(-25.00f, -144.36f, 51.90f, 74.77f, 0.07f),
            ['f'] = new LetterTouchParameters(-23.13f, -88.86f, 55.73f, 62.77f, 0.07f),
            ['g'] = new LetterTouchParameters(-52.89f, -97.07f, 65.56f, 66.30f, -0.24f),
            ['h'] = new LetterTouchParameters(-58.59f, -92.90f, 56.80f, 69.04f, -0.13f),
            ['i'] = new LetterTouchParameters(-64.91f, -134.94f, 52.58f, 88.97f, 0.19f),
            ['j'] = new LetterTouchParameters(-85.67f, -77.53f, 69.80f, 61.28f, -0.07f),
            ['k'] = new LetterTouchParameters(-109.52f, -90.45f, 66.60f, 70.39f, 0.03f),
            ['l'] = new LetterTouchParameters(-75.96f, -84.89f, 59.82f, 67.52f, 0.08f),
            ['m'] = new LetterTouchParameters(-82.43f, -33.14f, 44.72f, 49.81f, 0.03f),
            ['n'] = new LetterTouchParameters(-92.07f, -39.21f, 52.66f, 55.30f, -0.03f),
            ['o'] = new LetterTouchParameters(-80.50f, -127.38f, 53.52f, 89.54f, 0.13f),
            ['p'] = new LetterTouchParameters(-64.80f, -117.89f, 45.15f, 82.50f, 0.11f),
            ['q'] = new LetterTouchParameters(40.53f, -142.73f, 67.49f, 65.53f, -0.10f),
            ['r'] = new LetterTouchParameters(-14.54f, -138.39f, 64.43f, 78.09f, -0.01f),
            ['s'] = new LetterTouchParameters(-12.12f, -98.14f, 51.24f, 64.83f, 0.00f),
            ['t'] = new LetterTouchParameters(-33.73f, -142.53f, 60.84f, 77.36f, -0.08f),
            ['u'] = new LetterTouchParameters(-66.68f, -138.59f, 54.84f, 95.92f, -0.10f),
            ['v'] = new LetterTouchParameters(-42.60f, -40.38f, 61.81f, 53.03f, 0.03f),
            ['w'] = new LetterTouchParameters(-4.36f, -131.41f, 45.11f, 70.28f, 0.07f),
            ['x'] = new LetterTouchParameters(-35.97f, -83.76f, 38.26f, 39.11f, -0.12f),
            ['y'] = new LetterTouchParameters(-48.40f, -139.17f, 71.18f, 84.06f, -0.11f),
            ['z'] = new LetterTouchParameters(-4.42f, -47.98f, 79.42f, 48.01f, 0.30f),
        };
    }
}
