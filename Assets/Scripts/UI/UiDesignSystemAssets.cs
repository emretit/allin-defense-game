using TMPro;
using UnityEngine;

namespace DefenderChronicles.UI
{
    /// <summary>Builder-owned font roles and atlas-backed UI primitives for UGUI components.</summary>
    public sealed class UiDesignSystemAssets : ScriptableObject
    {
        public TMP_FontAsset regularFont;
        public TMP_FontAsset semiBoldFont;
        public TMP_FontAsset boldFont;
        public Sprite roundedSmall;
        public Sprite roundedMedium;
        public Sprite roundedCard;
        public Sprite pill;
        public Sprite softShadow;
        public Sprite verticalGradient;
        public Sprite goldGradient;
        public Sprite filledCircle;
        public Sprite cooldownRing;
    }
}
