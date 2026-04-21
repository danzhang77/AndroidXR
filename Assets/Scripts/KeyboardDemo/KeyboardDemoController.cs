using System.Text;
using AndroidXR.Protocol;
using AndroidXR.Trackpad;
using AndroidXR.Transport;
using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public sealed class KeyboardDemoController : MonoBehaviour
    {
        [Header("Relay")]
        [SerializeField] private bool connectToRelay = true;
        [SerializeField] private string relayUrl = "ws://localhost:8787";

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
        private RelayConnection relayConnection;

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

            padInput = new VirtualPadInput(
                presenter.PadInputRect,
                HandleTouchStarted,
                HandleTouchMoved,
                presenter.SetTouchPreview,
                HandleTouchReleased,
                HandleTouchCanceled);

            if (connectToRelay)
            {
                relayConnection = new RelayConnection(new UnityWebSocketTransport());
                _ = relayConnection.Connect(relayUrl);
            }
        }

        private void Update()
        {
            relayConnection?.Tick();
            padInput.Update();
            presenter.Tick(Time.deltaTime);
        }

        private async void OnDestroy()
        {
            if (relayConnection != null)
            {
                await relayConnection.Close();
            }
        }

        private void HandleTouchStarted(Vector2 touchPoint)
        {
            SendTouch(TrackpadPhase.Down, touchPoint);
        }

        private void HandleTouchMoved(Vector2 touchPoint)
        {
            SendTouch(TrackpadPhase.Move, touchPoint);
        }

        private void HandleTouchReleased(Vector2 touchPoint)
        {
            SendTouch(TrackpadPhase.Up, touchPoint);

            var result = decoder.Decode(layout.Keys, touchPoint, commitDistanceThreshold);

            if (!result.IsCommitted || result.Key == null)
            {
                presenter.SetStatus($"Touch released with no commit. Closest key was {result.Key?.Label ?? "none"} at distance {result.DistanceFromCenter:F3}.");
                return;
            }

            CommitKey(result.Key);
            SendKeyboardCommit(result, touchPoint);
            presenter.FlashKey(result.Key.Id);
            presenter.SetOutput(typedText.ToString());
            presenter.SetStatus($"Committed {result.Key.Label} with placeholder sigma ({result.Key.GaussianSigma.x:F3}, {result.Key.GaussianSigma.y:F3}).");
        }

        private void HandleTouchCanceled()
        {
            relayConnection?.Send(
                XrProtocolConstants.TrackpadTouch,
                XrProtocolConstants.TargetBrowser,
                new TrackpadTouchPayload
                {
                    pointerId = 0,
                    phase = TrackpadPhase.Cancel,
                    x = 0f,
                    y = 0f,
                    pressure = 0f,
                });

            presenter.SetStatus("Touch canceled because release occurred outside the pad.");
        }

        private void SendTouch(string phase, Vector2 touchPoint)
        {
            relayConnection?.Send(
                XrProtocolConstants.TrackpadTouch,
                XrProtocolConstants.TargetBrowser,
                new TrackpadTouchPayload
                {
                    pointerId = 0,
                    phase = phase,
                    x = Mathf.Clamp01(touchPoint.x),
                    y = Mathf.Clamp01(touchPoint.y),
                    pressure = phase == TrackpadPhase.Up || phase == TrackpadPhase.Cancel ? 0f : 1f,
                });
        }

        private void SendKeyboardCommit(KeyDecodeResult result, Vector2 touchPoint)
        {
            var text = result.Key.Kind switch
            {
                KeyboardKeyKind.Character => result.Key.Label,
                KeyboardKeyKind.Space => " ",
                KeyboardKeyKind.Backspace => string.Empty,
                _ => string.Empty,
            };

            relayConnection?.Send(
                XrProtocolConstants.KeyboardCommit,
                XrProtocolConstants.TargetBrowser,
                new KeyboardCommitPayload
                {
                    keyId = result.Key.Id,
                    label = result.Key.Label,
                    text = text,
                    kind = ToProtocolKind(result.Key.Kind),
                    confidence = Mathf.Clamp01(Mathf.Exp(result.Score)),
                    touch = new NormalizedPointPayload
                    {
                        x = Mathf.Clamp01(touchPoint.x),
                        y = Mathf.Clamp01(touchPoint.y),
                    },
                });
        }

        private static string ToProtocolKind(KeyboardKeyKind kind)
        {
            return kind switch
            {
                KeyboardKeyKind.Character => "character",
                KeyboardKeyKind.Space => "space",
                KeyboardKeyKind.Backspace => "backspace",
                _ => "unknown",
            };
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
