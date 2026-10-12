using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiStarRow : MonoBehaviour
    {
        [SerializeField] private Image[] stars;
        [SerializeField] private UiDesignSystemAssets assets;
        [SerializeField] private int earned;
        private UiMotion.Handle animation;
        public int Earned => earned;
        public int VisibleStars { get; private set; }
        public void Configure(Image[] images, UiDesignSystemAssets set) { stars = images; assets = set; }
        public void SetStars(int count, bool animate = true)
        {
            earned = Mathf.Clamp(count, 0, 3); animation?.Cancel();
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            if (!animate || !isActiveAndEnabled) Render(earned);
            else animation = motion.Animate(t => Render(Mathf.FloorToInt(t * earned)), UiTokens.SlideDuration * 3);
        }
        private void Render(int count)
        {
            VisibleStars = count;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].sprite = i < count ? assets.iconStarFilled : assets.iconStarEmpty;
                stars[i].color = i < count ? UiTokens.Gold : UiTokens.TextSecondary;
            }
        }
        private void OnDisable() { animation?.Cancel(); if (stars != null) Render(earned); }
    }
}
