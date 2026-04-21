using System.Text;
using System.Collections.Generic;
using AndroidXR.LanguageModel;
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

        [Header("Language Model")]
        [SerializeField] private bool useLanguageModel = true;
        [SerializeField] private string dictionaryFileName = "dict10k.txt";
        [SerializeField] private string bigramFileName = "sorted_bigram.txt";
        [SerializeField] private float touchWeight = 1f;
        [SerializeField] private float unigramWeight = 0.18f;
        [SerializeField] private float bigramWeight = 0.32f;
        [SerializeField] private float suggestionSelectionYMin = 0.86f;

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
        private TouchSequenceWordDecoder wordDecoder;
        private readonly List<Vector2> currentWordTouches = new();
        private readonly StringBuilder currentRawWord = new();
        private string[] currentSuggestions = new string[0];
        private string previousWord = BigramLanguageModel.HeadToken;

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

            if (useLanguageModel)
            {
                StartCoroutine(LanguageModelLoader.Load(dictionaryFileName, bigramFileName, HandleLanguageModelLoaded));
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

            if (TryCommitSuggestion(touchPoint))
            {
                return;
            }

            var result = decoder.Decode(layout.Keys, touchPoint, commitDistanceThreshold);

            if (!result.IsCommitted || result.Key == null)
            {
                presenter.SetStatus($"Touch released with no commit. Closest key was {result.Key?.Label ?? "none"} at distance {result.DistanceFromCenter:F3}.");
                return;
            }

            CommitKey(result.Key, touchPoint, out var committedText, out var committedKind);
            SendKeyboardCommit(result, touchPoint, committedText, committedKind);
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

        private void SendKeyboardCommit(KeyDecodeResult result, Vector2 touchPoint, string text, string kind)
        {
            SendKeyboardCommit(result.Key.Id, result.Key.Label, touchPoint, text, kind, Mathf.Clamp01(Mathf.Exp(result.Score)));
        }

        private void SendKeyboardCommit(string keyId, string label, Vector2 touchPoint, string text, string kind, float confidence)
        {
            relayConnection?.Send(
                XrProtocolConstants.KeyboardCommit,
                XrProtocolConstants.TargetBrowser,
                new KeyboardCommitPayload
                {
                    keyId = keyId,
                    label = label,
                    text = text,
                    kind = kind,
                    confidence = confidence,
                    touch = new NormalizedPointPayload
                    {
                        x = Mathf.Clamp01(touchPoint.x),
                        y = Mathf.Clamp01(touchPoint.y),
                    },
                });
        }

        private void HandleLanguageModelLoaded(LoadedLanguageModel loadedLanguageModel)
        {
            if (loadedLanguageModel == null)
            {
                presenter.SetStatus("Language model unavailable. Falling back to raw key decoding.");
                return;
            }

            wordDecoder = new TouchSequenceWordDecoder(
                loadedLanguageModel.Vocabulary,
                loadedLanguageModel.BigramLanguageModel,
                touchWeight,
                unigramWeight,
                bigramWeight);
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

        private void CommitKey(KeyboardKeyDefinition key, Vector2 touchPoint, out string committedText, out string committedKind)
        {
            committedText = string.Empty;
            committedKind = ToProtocolKind(key.Kind);

            switch (key.Kind)
            {
                case KeyboardKeyKind.Character:
                    typedText.Append(key.Label);
                    currentRawWord.Append(key.Label.ToLowerInvariant());
                    currentWordTouches.Add(touchPoint);
                    committedText = key.Label;
                    SendSuggestions();
                    break;
                case KeyboardKeyKind.Space:
                    committedText = CommitSpaceWithLanguageModelCorrection();
                    committedKind = "replace_current_word";
                    SendSuggestions();
                    break;
                case KeyboardKeyKind.Backspace:
                    if (typedText.Length > 0)
                    {
                        typedText.Length -= 1;
                    }
                    if (currentRawWord.Length > 0)
                    {
                        currentRawWord.Length -= 1;
                    }
                    if (currentWordTouches.Count > 0)
                    {
                        currentWordTouches.RemoveAt(currentWordTouches.Count - 1);
                    }
                    committedKind = "backspace";
                    SendSuggestions();
                    break;
            }
        }

        private string CommitSpaceWithLanguageModelCorrection()
        {
            if (currentRawWord.Length == 0)
            {
                typedText.Append(' ');
                return " ";
            }

            var rawWord = currentRawWord.ToString();
            var decodedWord = rawWord;

            if (wordDecoder != null)
            {
                var result = wordDecoder.Decode(layout.Keys, currentWordTouches, previousWord, rawWord);
                if (result.IsDecoded)
                {
                    decodedWord = result.Word;
                }
            }

            ReplaceCurrentRawWord(decodedWord);
            typedText.Append(' ');
            previousWord = decodedWord;
            currentRawWord.Clear();
            currentWordTouches.Clear();
            return $"{decodedWord} ";
        }

        private bool TryCommitSuggestion(Vector2 touchPoint)
        {
            if (touchPoint.y < suggestionSelectionYMin ||
                currentRawWord.Length == 0 ||
                currentSuggestions.Length == 0)
            {
                return false;
            }

            var suggestionIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(touchPoint.x) * 3f), 0, 2);
            if (suggestionIndex >= currentSuggestions.Length || string.IsNullOrEmpty(currentSuggestions[suggestionIndex]))
            {
                return false;
            }

            var selectedWord = currentSuggestions[suggestionIndex];
            ReplaceCurrentRawWord(selectedWord);
            typedText.Append(' ');
            previousWord = selectedWord;
            currentRawWord.Clear();
            currentWordTouches.Clear();
            SendKeyboardCommit("SUGGESTION", selectedWord, touchPoint, $"{selectedWord} ", "replace_current_word", 1f);
            SendSuggestions();
            presenter.SetOutput(typedText.ToString());
            presenter.SetStatus($"Selected suggestion {selectedWord}.");
            return true;
        }

        private void ReplaceCurrentRawWord(string decodedWord)
        {
            if (currentRawWord.Length > 0 && typedText.Length >= currentRawWord.Length)
            {
                typedText.Length -= currentRawWord.Length;
            }

            typedText.Append(decodedWord);
        }

        private void SendSuggestions()
        {
            var suggestions = new string[0];
            if (wordDecoder != null && currentWordTouches.Count > 0)
            {
                var results = wordDecoder.DecodeTop(layout.Keys, currentWordTouches, previousWord, currentRawWord.ToString(), 3);
                suggestions = new string[results.Count];
                for (var i = 0; i < results.Count; i++)
                {
                    suggestions[i] = results[i].Word;
                }
            }

            currentSuggestions = suggestions;

            relayConnection?.Send(
                XrProtocolConstants.KeyboardSuggestions,
                XrProtocolConstants.TargetBrowser,
                new KeyboardSuggestionsPayload
                {
                    rawWord = currentRawWord.ToString(),
                    suggestions = suggestions,
                });
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
