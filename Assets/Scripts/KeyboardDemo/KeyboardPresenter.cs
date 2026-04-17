using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AndroidXR.KeyboardDemo
{
    public sealed class KeyboardPresenter
    {
        private readonly Dictionary<string, Image> keyBackgrounds = new();
        private readonly RectTransform padRect;
        private readonly Image padTouchMarker;
        private readonly Text outputText;
        private readonly Text statusText;
        private readonly Color keyBaseColor;
        private readonly Color keyFlashColor;
        private readonly float flashDuration;

        private string flashingKeyId;
        private float flashTimeRemaining;

        public RectTransform PadRect => padRect;

        public KeyboardPresenter(
            Transform root,
            IReadOnlyList<KeyboardKeyDefinition> keys,
            Font font,
            Color keyBaseColor,
            Color keyFlashColor,
            float flashDuration)
        {
            this.keyBaseColor = keyBaseColor;
            this.keyFlashColor = keyFlashColor;
            this.flashDuration = flashDuration;

            var canvas = CreateCanvas(root.gameObject);
            CreateBackdrop(canvas.transform);
            outputText = CreateOutputText(canvas.transform, font);
            statusText = CreateStatusText(canvas.transform, font);

            var keyboardPanel = CreatePanel(canvas.transform, "Floating Keyboard", new Vector2(900f, 360f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0f, 0f), new Color(0.07f, 0.11f, 0.17f, 0.78f));
            CreatePanelHeader(keyboardPanel, "Floating Keyboard View", font);
            BuildKeys(CreateContentArea(keyboardPanel, 44f, 8f), keys, font);

            padRect = CreatePanel(canvas.transform, "Simulation Pad", new Vector2(760f, 210f), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), Vector2.zero, new Color(0.06f, 0.09f, 0.12f, 0.92f));
            CreatePadFrame(padRect);
            CreatePanelHeader(padRect, "Simulation Pad", font);
            padTouchMarker = CreatePadTouchMarker(padRect);
        }

        public void SetOutput(string value)
        {
            outputText.text = value.Length == 0 ? "Type using the pad below" : value;
        }

        public void SetStatus(string value)
        {
            statusText.text = value;
        }

        public void SetTouchPreview(Vector2? normalizedPoint)
        {
            padTouchMarker.enabled = normalizedPoint.HasValue;

            if (!normalizedPoint.HasValue)
            {
                return;
            }

            var rect = padRect.rect;
            var anchoredPosition = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPoint.Value.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPoint.Value.y));

            padTouchMarker.rectTransform.anchoredPosition = anchoredPosition;
        }

        public void FlashKey(string keyId)
        {
            if (!string.IsNullOrEmpty(flashingKeyId) && keyBackgrounds.TryGetValue(flashingKeyId, out var previousBackground))
            {
                previousBackground.color = keyBaseColor;
            }

            flashingKeyId = keyId;
            flashTimeRemaining = flashDuration;

            if (keyBackgrounds.TryGetValue(keyId, out var background))
            {
                background.color = keyFlashColor;
            }
        }

        public void Tick(float deltaTime)
        {
            if (string.IsNullOrEmpty(flashingKeyId))
            {
                return;
            }

            flashTimeRemaining -= deltaTime;
            if (!keyBackgrounds.TryGetValue(flashingKeyId, out var background))
            {
                flashingKeyId = null;
                flashTimeRemaining = 0f;
                return;
            }

            var t = Mathf.Clamp01(flashTimeRemaining / flashDuration);
            background.color = Color.Lerp(keyBaseColor, keyFlashColor, t);

            if (flashTimeRemaining > 0f)
            {
                return;
            }

            background.color = keyBaseColor;
            flashingKeyId = null;
        }

        private void BuildKeys(RectTransform keyboardPanel, IReadOnlyList<KeyboardKeyDefinition> keys, Font font)
        {
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var keyObject = new GameObject($"{key.Id} Key", typeof(RectTransform), typeof(Image));
                keyObject.transform.SetParent(keyboardPanel, false);

                var rectTransform = keyObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(key.NormalizedRect.xMin, key.NormalizedRect.yMin);
                rectTransform.anchorMax = new Vector2(key.NormalizedRect.xMax, key.NormalizedRect.yMax);
                rectTransform.offsetMin = new Vector2(6f, 6f);
                rectTransform.offsetMax = new Vector2(-6f, -6f);

                var image = keyObject.GetComponent<Image>();
                image.color = keyBaseColor;

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(keyObject.transform, false);

                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var text = labelObject.GetComponent<Text>();
                text.font = font;
                text.text = key.Label;
                text.fontSize = key.Kind == KeyboardKeyKind.Character ? 28 : 24;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;

                keyBackgrounds[key.Id] = image;
            }
        }

        private static Canvas CreateCanvas(GameObject owner)
        {
            var canvas = owner.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = owner.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            owner.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateBackdrop(Transform parent)
        {
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(parent, false);

            var rect = backdrop.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = backdrop.GetComponent<Image>();
            image.color = new Color(0.03f, 0.05f, 0.08f, 1f);
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Color backgroundColor)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = panel.GetComponent<Image>();
            image.color = backgroundColor;

            return rect;
        }

        private static Text CreateOutputText(Transform parent, Font font)
        {
            var outputRoot = CreatePanel(parent, "Output", new Vector2(900f, 90f), new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.93f), Vector2.zero, new Color(0.02f, 0.07f, 0.1f, 0.88f));
            var textObject = new GameObject("Output Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(outputRoot, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(24f, 14f);
            rect.offsetMax = new Vector2(-24f, -14f);

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = "Type using the pad below";
            return text;
        }

        private static Text CreateStatusText(Transform parent, Font font)
        {
            var statusRoot = CreatePanel(parent, "Status", new Vector2(900f, 44f), new Vector2(0.5f, 0.87f), new Vector2(0.5f, 0.87f), Vector2.zero, new Color(0f, 0f, 0f, 0f));
            var textObject = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(statusRoot, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.7f, 0.84f, 0.93f, 1f);
            text.text = "Click inside the pad, then release to commit a key";
            return text;
        }

        private static void CreatePanelHeader(RectTransform panel, string title, Font font)
        {
            var titleObject = new GameObject("Header", typeof(RectTransform), typeof(Text));
            titleObject.transform.SetParent(panel, false);

            var rect = titleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 36f);
            rect.anchoredPosition = new Vector2(0f, -6f);

            var text = titleObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 18;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.62f, 0.8f, 0.9f, 1f);
            text.text = $"  {title}";
        }

        private static void CreatePadFrame(RectTransform padRect)
        {
            var frame = new GameObject("Pad Frame", typeof(RectTransform), typeof(Image), typeof(Outline));
            frame.transform.SetParent(padRect, false);

            var rect = frame.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10f, 10f);
            rect.offsetMax = new Vector2(-10f, -10f);

            var image = frame.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            var outline = frame.GetComponent<Outline>();
            outline.effectColor = new Color(0.31f, 0.72f, 0.85f, 0.85f);
            outline.effectDistance = new Vector2(2f, 2f);
        }

        private static RectTransform CreateContentArea(RectTransform parent, float topInset, float sideInset)
        {
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(parent, false);

            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(sideInset, sideInset);
            rect.offsetMax = new Vector2(-sideInset, -topInset);
            return rect;
        }

        private static Image CreatePadTouchMarker(RectTransform padRect)
        {
            var marker = new GameObject("Pad Touch Marker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(padRect, false);

            var rect = marker.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(26f, 26f);

            var image = marker.GetComponent<Image>();
            image.color = new Color(0.98f, 0.87f, 0.3f, 0.95f);
            image.enabled = false;
            return image;
        }
    }
}
