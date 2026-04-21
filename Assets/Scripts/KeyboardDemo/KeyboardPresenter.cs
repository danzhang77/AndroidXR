using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AndroidXR.KeyboardDemo
{
    public sealed class KeyboardPresenter
    {
        private readonly RectTransform padRect;
        private readonly RectTransform padInputRect;
        private readonly Image padTouchMarker;

        public RectTransform PadRect => padRect;
        public RectTransform PadInputRect => padInputRect;

        public KeyboardPresenter(
            Transform root,
            IReadOnlyList<KeyboardKeyDefinition> keys,
            Font font,
            Color keyBaseColor,
            Color keyFlashColor,
            float flashDuration)
        {
            var canvas = CreateCanvas(root.gameObject);
            CreateBackdrop(canvas.transform);

            padRect = CreatePanel(canvas.transform, "Trackpad", new Vector2(1120f, 620f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.06f, 0.09f, 0.12f, 0.92f));
            CreatePanelHeader(padRect, "Trackpad Input", font);
            padInputRect = CreateContentArea(padRect, 44f, 14f);
            CreatePadFrame(padInputRect);
            padTouchMarker = CreatePadTouchMarker(padInputRect);
        }

        public void SetOutput(string value)
        {
            // Text output is rendered by the browser glasses view.
        }

        public void SetStatus(string value)
        {
            // Status is intentionally hidden on the phone input surface.
        }

        public void SetTouchPreview(Vector2? normalizedPoint)
        {
            padTouchMarker.enabled = normalizedPoint.HasValue;

            if (!normalizedPoint.HasValue)
            {
                return;
            }

            var rect = padInputRect.rect;
            var anchoredPosition = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPoint.Value.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPoint.Value.y));

            padTouchMarker.rectTransform.anchoredPosition = anchoredPosition;
        }

        public void FlashKey(string keyId)
        {
            // Key highlighting is rendered by the browser glasses view.
        }

        public void Tick(float deltaTime)
        {
            // Browser view owns transient visual effects.
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
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

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
