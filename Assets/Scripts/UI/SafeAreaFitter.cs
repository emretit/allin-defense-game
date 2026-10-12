using UnityEngine;

namespace AllinDefense.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private Vector2 lastAnchorMin;
        private Vector2 lastAnchorMax;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (lastSafeArea == Screen.safeArea &&
                lastScreenSize.x == Screen.width &&
                lastScreenSize.y == Screen.height &&
                rectTransform.anchorMin == lastAnchorMin &&
                rectTransform.anchorMax == lastAnchorMax &&
                rectTransform.offsetMin == Vector2.zero &&
                rectTransform.offsetMax == Vector2.zero)
                return;

            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            lastAnchorMin = anchorMin;
            lastAnchorMax = anchorMax;
        }
    }
}
