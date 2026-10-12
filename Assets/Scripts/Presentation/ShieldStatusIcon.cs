using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    // A shape, rather than a font glyph: readable when emoji fonts are unavailable on a device.
    public sealed class ShieldStatusIcon : MaskableGraphic
    {
        public static void Attach(Transform healthBar)
        {
            var go = new GameObject("Physical armour 50 percent", typeof(RectTransform), typeof(ShieldStatusIcon));
            go.transform.SetParent(healthBar, false);
            var icon = go.GetComponent<ShieldStatusIcon>();
            icon.color = new Color(.55f,.8f,1); icon.raycastTarget = false;
            icon.rectTransform.anchoredPosition = new Vector2(-68,4);
            icon.rectTransform.sizeDelta = new Vector2(24,30);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = GetPixelAdjustedRect();
            Vector2[] points = { new(r.xMin,r.yMax), new(r.xMax,r.yMax), new(r.xMax,r.center.y), new(r.center.x,r.yMin), new(r.xMin,r.center.y) };
            foreach (var point in points) vh.AddVert(point,color,Vector2.zero);
            vh.AddTriangle(0,1,2); vh.AddTriangle(0,2,3); vh.AddTriangle(0,3,4);
        }
    }
}
