using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    [RequireComponent(typeof(RawImage), typeof(RectTransform))]
    public sealed class FrontendCoverImage : MonoBehaviour
    {
        private RawImage image;
        private RectTransform rectTransform;
        public bool Drift { get; set; }
        private Texture lastTexture;
        private Vector2 lastSize;

        private void Awake()
        {
            image = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();
            ApplyCoverCrop();
        }

        private void LateUpdate()
        {
            if (!Drift && image.texture == lastTexture && rectTransform.rect.size == lastSize) return;
            ApplyCoverCrop();
        }

        private void ApplyCoverCrop()
        {
            lastTexture = image.texture;
            lastSize = rectTransform.rect.size;
            if (lastTexture == null || lastSize.x <= 0f || lastSize.y <= 0f) return;

            float textureAspect = lastTexture.width / (float)lastTexture.height;
            float viewAspect = lastSize.x / lastSize.y;
            if (viewAspect > textureAspect)
            {
                float visibleHeight = textureAspect / viewAspect;
                image.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }
            else
            {
                float visibleWidth = viewAspect / textureAspect;
                image.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }
            if (Drift && !FrontendPreferences.ReducedMotion)
            {
                Rect uv = image.uvRect;
                float insetX = uv.width * .008f, insetY = uv.height * .008f;
                uv.x += insetX * (1 + Mathf.Sin(Time.unscaledTime * .14f));
                uv.y += insetY * (1 + Mathf.Cos(Time.unscaledTime * .11f));
                uv.width -= insetX * 2; uv.height -= insetY * 2;
                image.uvRect = uv;
            }
        }
    }
}
