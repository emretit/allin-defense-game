using TMPro;
using UnityEngine;

namespace AllinDefense.UI.Kit
{
    public sealed class UiKitText : MonoBehaviour
    {
        [SerializeField] private TMP_Text target;
        [SerializeField] private string turkish;
        [SerializeField] private string english;
        public void Configure(TMP_Text text, LocalizedCopy copy)
        {
            target = text; turkish = copy.Turkish; english = copy.English; Apply();
        }
        private void OnEnable() { GameLocalization.LanguageChanged += Apply; Apply(); }
        private void OnDisable() => GameLocalization.LanguageChanged -= Apply;
        private void Apply() { if (target != null) target.text = GameLocalization.Text(turkish, english); }
    }
}
