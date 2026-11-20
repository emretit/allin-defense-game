using AllinDefense.Core;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Presentation
{
    public sealed class DefenderKeepPlate : MonoBehaviour
    {
        private GameSessionController session;
        private Camera worldCamera;
        private RectTransform root, plate;
        private Image backing, fill;
        private const float PlateWidth = 124, PlateHeight = 20, PlateHalfWidth = PlateWidth / 2 + 6;
        private TMP_Text amount;
        private UiMotion motion;
        private int shownLives = -1, shownMaximum = -1;
        private bool critical;
        private float flashRemaining;
        private Renderer[] keepRenderers;
        public TMP_Text Amount => amount;
        public Image Fill => fill;
        public RectTransform Plate => plate;

        public void Initialize(GameSessionController owner, UiKit kit)
        {
            if (session != null) return;
            session = owner;
            worldCamera = Camera.main;
            root = (RectTransform)transform;
            var anchor = UiKit.Node(transform, "Keep Anchor");
            plate = UiKit.Node(anchor, "Keep Plate");
            // Compact health bar above the keep: slim bar with a small "20/20" beside it, no "KEEP" label.
            plate.sizeDelta = new Vector2(PlateWidth, PlateHeight);
            backing = kit.Decoration(plate, "Keep Backing", kit.Assets.pill, UiTokens.GlassStrong);
            var track = kit.Decoration(plate, "Keep Track", kit.Assets.pill, UiTokens.HairlineStrong);
            Place(track.rectTransform, new Vector2(7, 0), new Vector2(68, 8));
            fill = kit.Decoration(track.transform, "Keep Fill", kit.Assets.pill, UiTokens.Gold);
            amount = kit.Text(plate, "Keep Lives", "", 12, true);
            Place(amount.rectTransform, new Vector2(81, 0), new Vector2(38, 18));
            motion = plate.gameObject.AddComponent<UiMotion>();
            foreach (var item in session.GetComponentsInChildren<Transform>())
                if (item.name == "Defender Keep Licensed")
                {
                    keepRenderers = item.GetComponentsInChildren<Renderer>();
                    break;
                }
            session.PresentationSignaled += OnSignal;
            Refresh();
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public void Refresh()
        {
            if (session == null) return;
            if (session.Lives == shownLives && session.StartingLivesForLevel == shownMaximum) return;
            shownLives = session.Lives;
            shownMaximum = session.StartingLivesForLevel;
            amount.SetText("{0}/{1}", shownLives, shownMaximum);
            UiKit.SetHorizontalFill(fill, shownLives / (float)Mathf.Max(1, shownMaximum));
            bool nextCritical = shownLives <= 5;
            fill.color = amount.color = nextCritical ? UiTokens.Danger : UiTokens.Gold;
            if (nextCritical && !critical) motion.Punch(plate);
            critical = nextCritical;
        }

        private void OnSignal(PresentationSignal signal)
        {
            if (signal.cue == PresentationCue.UiError && signal.value > 0) flashRemaining = .3f;
        }

        private void LateUpdate()
        {
            if (session == null || worldCamera == null) return;
            Refresh();
            flashRemaining = Mathf.Max(0, flashRemaining - Time.unscaledDeltaTime);
            backing.color = Color.Lerp(UiTokens.GlassStrong, UiTokens.Danger, flashRemaining / .3f * .65f);
            Vector2 screen = ScreenAnchor();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out Vector2 local);
            var anchor = (RectTransform)plate.parent;
            anchor.anchoredPosition = local;
            Vector2 points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            anchor.localScale = new Vector3(1 / points.x, 1 / points.y, 1);
        }

        public Vector2 ScreenAnchor()
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(session.Path[session.Path.Count - 1] + Vector3.up * 4.3f);
            Vector2 pixelsPerPoint = new(Screen.width / UiMetrics.LogicalScreenSize.x, Screen.height / UiMetrics.LogicalScreenSize.y);
            float keepLeft = screen.x, keepTop = float.MinValue;
            if (keepRenderers != null && keepRenderers.Length > 0)
            {
                Bounds bounds = keepRenderers[0].bounds;
                for (int i = 1; i < keepRenderers.Length; i++) bounds.Encapsulate(keepRenderers[i].bounds);
                screen = worldCamera.WorldToScreenPoint(bounds.center);
                float top = float.MinValue;
                keepLeft = float.MaxValue;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    Vector3 projected = worldCamera.WorldToScreenPoint(point);
                    top = Mathf.Max(top, projected.y);
                    keepLeft = Mathf.Min(keepLeft, projected.x);
                }
                keepTop = top;
                screen.y = top + 16 * pixelsPerPoint.y;
            }
            Rect safe = Screen.safeArea;
            if (safe.width <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            if (screen.y > safe.yMax - 90 * pixelsPerPoint.y && keepTop > safe.yMax - 122 * pixelsPerPoint.y)
                screen.x = keepLeft - PlateHalfWidth * pixelsPerPoint.x;
            screen.x = Mathf.Clamp(screen.x, safe.xMin + PlateHalfWidth * pixelsPerPoint.x, safe.xMax - PlateHalfWidth * pixelsPerPoint.x);
            screen.y = Mathf.Clamp(screen.y, safe.yMin + 40 * pixelsPerPoint.y, safe.yMax - 90 * pixelsPerPoint.y);
            return screen;
        }

        private void OnDestroy()
        {
            if (session != null) session.PresentationSignaled -= OnSignal;
        }
    }
}
