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
            else
            {
                Render(0);
                animation = motion.Animate(t =>
                {
                    float sequence = t * earned;
                    Render(Mathf.Min(earned, Mathf.CeilToInt(sequence)));
                    for (int i = 0; i < earned; i++)
                    {
                        float phase = Mathf.Clamp01(sequence - i);
                        float scale = phase < .7f ? Mathf.Lerp(0, 1.2f, phase / .7f) : Mathf.Lerp(1.2f, 1, (phase - .7f) / .3f);
                        stars[i].transform.localScale = Vector3.one * scale;
                    }
                }, UiTokens.SlideDuration * 3);
            }
        }
        private void Render(int count)
        {
            VisibleStars = count;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].transform.localScale = Vector3.one;
                stars[i].sprite = i < count ? assets.iconStarFilled : assets.iconStarEmpty;
                stars[i].color = i < count ? UiTokens.Gold : UiTokens.TextSecondary;
            }
        }
        private void OnDisable() { animation?.Cancel(); if (stars != null) Render(earned); }
    }
}
