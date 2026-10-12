using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    [ExecuteAlways, RequireComponent(typeof(Image))]
    public sealed class UiPillShape : MonoBehaviour
    {
        private void OnEnable() => Refresh();
        private void OnRectTransformDimensionsChange() => Refresh();
        private void Refresh()
        {
            var image = GetComponent<Image>();
            if (image.sprite == null) return;
            float size = Mathf.Min(image.rectTransform.rect.width, image.rectTransform.rect.height);
            if (size <= 0) return;
            var canvas = image.canvas;
            float reference = canvas != null ? canvas.referencePixelsPerUnit : 100;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = image.sprite.rect.height * reference / (image.sprite.pixelsPerUnit * size);
        }
    }
}
