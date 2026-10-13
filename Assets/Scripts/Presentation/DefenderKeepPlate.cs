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
        private TMP_Text amount, label;
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
            plate.sizeDelta = new Vector2(146, 48);
            var border = kit.Decoration(plate, "Keep Border", kit.Assets.roundedSmall, UiTokens.HairlineGold);
            backing = kit.Decoration(border.transform, "Keep Backing", kit.Assets.roundedSmall, UiTokens.GlassStrong);
            backing.rectTransform.offsetMin = Vector2.one;
            backing.rectTransform.offsetMax = -Vector2.one;
            label = kit.Text(plate, "Keep Label", "", 11, true);
            Place(label.rectTransform, new Vector2(10, 8), new Vector2(56, 24));
            amount = kit.Text(plate, "Keep Lives", "", 16, true);
            amount.alignment = TextAlignmentOptions.MidlineRight;
            Place(amount.rectTransform, new Vector2(76, 8), new Vector2(60, 28));
            var track = kit.Decoration(plate, "Keep Track", kit.Assets.pill, UiTokens.HairlineStrong);
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(.5f, 0);
            track.rectTransform.sizeDelta = new Vector2(126, 6);
            track.rectTransform.anchoredPosition = new Vector2(0, 10);
            fill = kit.Decoration(track.transform, "Keep Fill", kit.Assets.pill, UiTokens.Gold);
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
            label.text = GameLocalization.Text("KALE", "KEEP");
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
                screen.y = top + 32 * pixelsPerPoint.y;
            }
            Rect safe = Screen.safeArea;
            if (safe.width <= 0) safe = new Rect(0, 0, Screen.width, Screen.height);
            if (screen.y > safe.yMax - 90 * pixelsPerPoint.y && keepTop > safe.yMax - 122 * pixelsPerPoint.y)
                screen.x = keepLeft - 83 * pixelsPerPoint.x;
            screen.x = Mathf.Clamp(screen.x, safe.xMin + 83 * pixelsPerPoint.x, safe.xMax - 83 * pixelsPerPoint.x);
            screen.y = Mathf.Clamp(screen.y, safe.yMin + 40 * pixelsPerPoint.y, safe.yMax - 90 * pixelsPerPoint.y);
            return screen;
        }

        private void OnDestroy()
        {
            if (session != null) session.PresentationSignaled -= OnSignal;
        }
    }
}
