using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.UI.Kit
{
    public sealed class UiProgressSegments : MonoBehaviour
    {
        [SerializeField] private Image[] fills;
        public int Count => fills.Length;
        public float Progress { get; private set; }
        public void Configure(Image[] segments) { fills = segments; SetProgress(0); }
        public void SetProgress(float completedSegments)
        {
            Progress = Mathf.Clamp(completedSegments, 0, Count);
            for (int i = 0; i < Count; i++) fills[i].fillAmount = Mathf.Clamp01(Progress - i);
        }
    }
}
