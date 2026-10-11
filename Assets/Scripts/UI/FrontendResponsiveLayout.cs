using System;
using UnityEngine;

namespace DefenderChronicles.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FrontendResponsiveLayout : MonoBehaviour
    {
        private RectTransform rect;
        private Vector2 lastSize = new(-1, -1);
        private Action<Vector2> apply;

        public void Configure(Action<Vector2> layout)
        {
            rect = (RectTransform)transform;
            apply = layout;
            Refresh();
        }

        public static bool UseColumns(Vector2 size) => size.y > 0 && size.x / size.y >= 0.70f;

        public void Refresh()
        {
            if (rect == null || apply == null || rect.rect.width <= 0 || rect.rect.height <= 0) return;
            lastSize = rect.rect.size;
            apply(lastSize);
        }

        private void LateUpdate()
        {
            if (rect != null && rect.rect.size != lastSize) Refresh();
        }
        private void OnEnable() => Refresh();
    }
}
