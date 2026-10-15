using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    [ExecuteAlways]
    public sealed class UiPointSize : LayoutElement
    {
        [SerializeField] private float points = UiMetrics.MinTouchPt;
        public float Points => points;
        public bool IgnoreOwnScale { get; set; }
        public void Configure(float value) { points = Mathf.Max(UiMetrics.MinTouchPt, value); Refresh(); }
        protected override void OnEnable() { base.OnEnable(); Canvas.preWillRenderCanvases += Refresh; Refresh(); }
        protected override void OnDisable() { Canvas.preWillRenderCanvases -= Refresh; base.OnDisable(); }
        public void Refresh()
        {
            Vector2 size = UiMetrics.CanvasUnitsForPoints(points, UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>()));
            Vector2 scale = Vector2.one;
            for (Transform item = IgnoreOwnScale ? transform.parent : transform; item != null && item.GetComponent<Canvas>() == null; item = item.parent)
                scale = Vector2.Scale(scale, new Vector2(Mathf.Abs(item.localScale.x), Mathf.Abs(item.localScale.y)));
            minWidth = size.x / Mathf.Max(.001f, scale.x);
            minHeight = size.y / Mathf.Max(.001f, scale.y);
        }
    }
}
