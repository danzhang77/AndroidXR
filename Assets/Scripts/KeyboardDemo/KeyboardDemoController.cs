using System.Text;
using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public sealed class KeyboardDemoController : MonoBehaviour
    {
        [Header("Gaussian Placeholder Parameters")]
        [SerializeField] private int placeholderRandomSeed = 11;
        [SerializeField] private float placeholderSigmaMin = 0.075f;
        [SerializeField] private float placeholderSigmaMax = 0.095f;

        [Header("Commit Tuning")]
        [SerializeField] private bool thresholdUsesLetterKeyHeight = true;
        [SerializeField] private float commitDistanceThreshold = 0.2f;

        [Header("Visual Tuning")]
        [SerializeField] private float keyFlashDuration = 0.16f;
        [SerializeField] private Color keyBaseColor = new(0.13f, 0.2f, 0.27f, 0.95f);
        [SerializeField] private Color keyFlashColor = new(0.22f, 0.73f, 0.88f, 1f);

        private readonly StringBuilder typedText = new();
        private GaussianKeyDecoder decoder;
        private KeyboardLayoutData layout;
        private KeyboardPresenter presenter;
        private VirtualPadInput padInput;

        private void Awake()
        {
            decoder = new GaussianKeyDecoder();
            layout = KeyboardLayoutData.CreateDefaultLayout(
                placeholderRandomSeed,
                placeholderSigmaMin,
                placeholderSigmaMax);

            if (thresholdUsesLetterKeyHeight)
            {
                commitDistanceThreshold = layout.LetterKeyHeight;
            }

            presenter = new KeyboardPresenter(
                transform,
                layout.Keys,
                ResolveFont(),
                keyBaseColor,
                keyFlashColor,
                keyFlashDuration);

            presenter.SetOutput(string.Empty);
            presenter.SetStatus(BuildPlaceholderMessage());

            padInput = new VirtualPadInput(
                presenter.PadInputRect,
                presenter.SetTouchPreview,
                HandleTouchReleased,
                HandleTouchCanceled);
        }

        private void Update()
        {
            padInput.Update();
            presenter.Tick(Time.deltaTime);
        }

        private void HandleTouchReleased(Vector2 touchPoint)
        {
            var result = decoder.Decode(layout.Keys, touchPoint, commitDistanceThreshold);

            if (!result.IsCommitted || result.Key == null)
            {
                presenter.SetStatus($"Touch released with no commit. Closest key was {result.Key?.Label ?? "none"} at distance {result.DistanceFromCenter:F3}.");
                return;
            }

            CommitKey(result.Key);
            presenter.FlashKey(result.Key.Id);
            presenter.SetOutput(typedText.ToString());
            presenter.SetStatus($"Committed {result.Key.Label} with placeholder sigma ({result.Key.GaussianSigma.x:F3}, {result.Key.GaussianSigma.y:F3}).");
        }

        private void HandleTouchCanceled()
        {
            presenter.SetStatus("Touch canceled because release occurred outside the pad.");
        }

        private void CommitKey(KeyboardKeyDefinition key)
        {
            switch (key.Kind)
            {
                case KeyboardKeyKind.Character:
                    typedText.Append(key.Label);
                    break;
                case KeyboardKeyKind.Space:
                    typedText.Append(' ');
                    break;
                case KeyboardKeyKind.Backspace:
                    if (typedText.Length > 0)
                    {
                        typedText.Length -= 1;
                    }
                    break;
            }
        }

        private string BuildPlaceholderMessage()
        {
            return $"Placeholder Gaussian means come from each key center. Sigma uses seeded random values in [{placeholderSigmaMin:F3}, {placeholderSigmaMax:F3}] with seed {placeholderRandomSeed}.";
        }

        private static Font ResolveFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
            {
                return font;
            }

            return Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Verdana" }, 16);
        }
    }
}
