using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.UI
{
    [DisallowMultipleComponent]
    public sealed class MobileTouchTarget : MonoBehaviour
    {
        // At 0.39842 pt per canvas unit, 112 units exceed the 44 pt mobile target.
        public const float MinimumCanvasSize = 112f;
        private RectTransform visual;
        private RectTransform hitArea;

        public static void Attach(Button button)
        {
            if (button == null) return;
            var target = button.GetComponent<MobileTouchTarget>();
            if (target == null) target = button.gameObject.AddComponent<MobileTouchTarget>();
            target.UpdateBounds();
        }

        private void OnEnable() => UpdateBounds();
        private void LateUpdate() => UpdateBounds();

        private void UpdateBounds()
        {
            if (visual == null) visual = (RectTransform)transform;
            if (hitArea == null)
            {
                hitArea = transform.Find("Touch Target") as RectTransform;
                if (hitArea == null)
                {
                    var root = new GameObject("Touch Target", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    root.transform.SetParent(transform, false);
                    root.transform.SetAsFirstSibling();
                    hitArea = (RectTransform)root.transform;
                    var image = root.GetComponent<Image>();
                    image.color = Color.clear;
                    image.raycastTarget = true;
                }
                hitArea.anchorMin = hitArea.anchorMax = hitArea.pivot = new Vector2(.5f, .5f);
                hitArea.anchoredPosition = Vector2.zero;
            }
            Vector2 size = visual.rect.size;
            hitArea.sizeDelta = new Vector2(Mathf.Max(MinimumCanvasSize, size.x), Mathf.Max(MinimumCanvasSize, size.y));
        }
    }
}
