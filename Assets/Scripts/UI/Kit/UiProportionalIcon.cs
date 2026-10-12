using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiProportionalIcon : LayoutElement
    {
        [SerializeField] private UiPointSize target;
        public void Configure(UiPointSize touchTarget) { target = touchTarget; layoutPriority = 2; Refresh(); }
        public override void CalculateLayoutInputHorizontal() => Refresh();
        public override void CalculateLayoutInputVertical() => Refresh();
        private void Refresh()
        {
            if (target == null) return;
            target.Refresh();
            preferredWidth = target.minWidth * .56f;
            preferredHeight = target.minHeight * .56f;
            minWidth = preferredWidth; minHeight = preferredHeight;
        }
    }
}
