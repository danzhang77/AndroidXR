using System;
using UnityEngine;

namespace AndroidXR.KeyboardDemo
{
    public sealed class VirtualPadInput
    {
        private readonly RectTransform padRect;
        private readonly Action<Vector2?> onPreviewChanged;
        private readonly Action<Vector2> onTouchReleased;
        private readonly Action onTouchCanceled;

        private bool isTouchActive;
        private Vector2? currentTouchPoint;

        public VirtualPadInput(
            RectTransform padRect,
            Action<Vector2?> onPreviewChanged,
            Action<Vector2> onTouchReleased,
            Action onTouchCanceled)
        {
            this.padRect = padRect;
            this.onPreviewChanged = onPreviewChanged;
            this.onTouchReleased = onTouchReleased;
            this.onTouchCanceled = onTouchCanceled;
        }

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (TryGetNormalizedPoint(Input.mousePosition, out var normalizedPoint))
                {
                    isTouchActive = true;
                    currentTouchPoint = normalizedPoint;
                    onPreviewChanged?.Invoke(normalizedPoint);
                }
            }

            if (isTouchActive && Input.GetMouseButton(0))
            {
                if (TryGetNormalizedPoint(Input.mousePosition, out var normalizedPoint))
                {
                    currentTouchPoint = normalizedPoint;
                    onPreviewChanged?.Invoke(normalizedPoint);
                }
                else
                {
                    currentTouchPoint = null;
                    onPreviewChanged?.Invoke(null);
                }
            }

            if (!isTouchActive || !Input.GetMouseButtonUp(0))
            {
                return;
            }

            isTouchActive = false;

            if (TryGetNormalizedPoint(Input.mousePosition, out var releasedPoint) && currentTouchPoint.HasValue)
            {
                currentTouchPoint = null;
                onPreviewChanged?.Invoke(null);
                onTouchReleased?.Invoke(releasedPoint);
            }
            else
            {
                currentTouchPoint = null;
                onPreviewChanged?.Invoke(null);
                onTouchCanceled?.Invoke();
            }
        }

        private bool TryGetNormalizedPoint(Vector2 screenPosition, out Vector2 normalizedPoint)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(padRect, screenPosition, null))
            {
                normalizedPoint = default;
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(padRect, screenPosition, null, out var localPoint))
            {
                normalizedPoint = default;
                return false;
            }

            var rect = padRect.rect;
            var normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            var normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
            normalizedPoint = new Vector2(normalizedX, normalizedY);

            return normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f;
        }
    }
}
