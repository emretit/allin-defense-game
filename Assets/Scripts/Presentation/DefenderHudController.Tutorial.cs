using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace AllinDefense.Presentation
{
    public sealed partial class DefenderHudController
    {
        private RectTransform tutorialBubble, tutorialArrow;
        private TMP_Text tutorialCopy;
        private Button tutorialHelp, tutorialSkip;
        private Image tutorialAbilityRing;
        private Transform tutorialTarget;
        private Camera tutorialCamera;
        private int tutorialDisplayStep = -1;
        private bool tutorialDismissed;
        private float tutorialCompletedUntil;
        public Transform TutorialTarget => tutorialTarget;
        public RectTransform TutorialArrow => tutorialArrow;

        private void InitializeTutorial()
        {
            tutorialBubble = UiKit.Node(transform, "TutorialGuide");
            tutorialCopy = frameKit.Text(tutorialBubble, "TutorialCopy", "", 12, true);
            PlaceCardText(tutorialCopy, 0, 0, 205, 24, 12);
            tutorialCopy.alignment = TextAlignmentOptions.Center;
            tutorialCopy.color = UiTokens.Gold;
            var balloon = frameKit.Decoration(tutorialBubble, "TutorialBalloon", frameKit.Assets.roundedSmall, UiTokens.KitSurface);
            PlaceTutorialRect(balloon.rectTransform, new Vector2(-6, 4), new Vector2(217, 29));
            balloon.transform.SetAsFirstSibling();
            var arrow = frameKit.Icon(tutorialBubble, "TutorialArrow", frameKit.Assets.iconBack, 24);
            tutorialArrow = arrow.rectTransform;
            PlaceTutorialRect(tutorialArrow, new Vector2(105, -42), new Vector2(30, 30));
            tutorialArrow.pivot = new Vector2(.5f, .5f);
            tutorialArrow.localEulerAngles = new Vector3(0, 0, 90); arrow.color = UiTokens.Gold;
            tutorialSkip = frameKit.Button(tutorialBubble, "TutorialSkip", FrontendCopy.TutorialSkip, UiButtonVariant.Glass);
            PlaceTutorialRect((RectTransform)tutorialSkip.transform, new Vector2(212, 0), new Vector2(88, 44));
            tutorialSkip.GetComponentInChildren<TMP_Text>().fontSize = 11;
            tutorialSkip.onClick.AddListener(() => { tutorialDismissed = true; tutorialBubble.gameObject.SetActive(false); });
            tutorialHelp = frameKit.Button(transform, "TutorialHelp", "?", UiButtonVariant.Glass);
            tutorialHelp.onClick.AddListener(() => { if (battleRadial.IsOpen) battleRadial.Close(); tutorialDismissed = false; tutorialCompletedUntil = Time.unscaledTime + 2; });
            tutorialAbilityRing = frameKit.Decoration(abilityButton.transform, "TutorialAbilityRing", frameKit.Assets.cooldownRing, UiTokens.Gold);
            tutorialAbilityRing.rectTransform.offsetMin = new Vector2(-4, -4);
            tutorialAbilityRing.rectTransform.offsetMax = new Vector2(4, 4);
            tutorialAbilityRing.gameObject.SetActive(false);
            tutorialBubble.gameObject.SetActive(false);
            tutorialHelp.gameObject.SetActive(false);
        }

        private static void PlaceTutorialRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        public void SetTutorialGuide(int step, Transform worldTarget, Camera camera)
        {
            if (tutorialBubble == null) return;
            tutorialTarget = step == 2 ? nextWaveButton.transform : step == 3 ? abilityButton.transform : worldTarget;
            tutorialCamera = camera;
            if (step != tutorialDisplayStep && step >= 6) tutorialCompletedUntil = Time.unscaledTime + 2;
            tutorialDisplayStep = step;
            tutorialArrow.gameObject.SetActive(step < 6);
            tutorialSkip.gameObject.SetActive(step < 6);
            string text = step switch
            {
                0 => FrontendCopy.TutorialBuild.Value, 1 => FrontendCopy.TutorialSecond.Value,
                2 => FrontendCopy.TutorialWave.Value, 3 => FrontendCopy.TutorialAbility.Value,
                4 => FrontendCopy.TutorialThird.Value, 5 => FrontendCopy.TutorialUpgrade.Value,
                _ => FrontendCopy.TutorialDone.Value
            };
            tutorialCopy.text = step >= 6 ? text : $"{step + 1}/6 · {text}";
            tutorialHelp.gameObject.SetActive(true);
            UpdateTutorialPresentation();
        }

        private void UpdateTutorialPresentation()
        {
            if (tutorialBubble == null || tutorialDisplayStep < 0) return;
            var points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            var scale = new Vector3(1 / points.x, 1 / points.y, 1);
            var helpRect = (RectTransform)tutorialHelp.transform;
            helpRect.anchorMin = helpRect.anchorMax = new Vector2(Screen.safeArea.xMin / Screen.width, Screen.safeArea.yMax / Screen.height);
            helpRect.pivot = new Vector2(0, 1); helpRect.localScale = scale;
            helpRect.anchoredPosition = new Vector2(4 / points.x, -60 / points.y); helpRect.sizeDelta = Vector2.one * 44;
            var session = AllinDefense.Core.GameSessionController.Instance;
            bool activeBattle = session == null || session.Phase is not (AllinDefense.Core.GamePhase.Paused or AllinDefense.Core.GamePhase.Victory or AllinDefense.Core.GamePhase.Defeat);
            bool visible = activeBattle && !tutorialDismissed && !battleRadial.IsOpen && (tutorialDisplayStep < 6 || Time.unscaledTime < tutorialCompletedUntil);
            tutorialHelp.gameObject.SetActive(activeBattle);
            tutorialBubble.gameObject.SetActive(visible);
            tutorialAbilityRing.gameObject.SetActive(visible && tutorialDisplayStep == 3);
            if (!visible) return;
            tutorialBubble.localScale = scale;
            tutorialBubble.anchorMin = tutorialBubble.anchorMax = new Vector2(.5f, .5f);
            tutorialBubble.pivot = new Vector2(.5f, 0);
            tutorialBubble.sizeDelta = new Vector2(300, 64);
            Vector2 screen = tutorialTarget is RectTransform ui
                ? RectTransformUtility.WorldToScreenPoint(null, ui.TransformPoint(ui.rect.center))
                : tutorialTarget != null && tutorialCamera != null ? (Vector2)tutorialCamera.WorldToScreenPoint(tutorialTarget.position) : new Vector2(Screen.width * .5f, Screen.height * .5f);
            var root = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out var local);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Screen.safeArea.min, null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, Screen.safeArea.max, null, out var max);
            tutorialBubble.anchoredPosition = new Vector2(Mathf.Clamp(local.x + 45 / points.x, min.x + 150 / points.x, max.x - 150 / points.x),
                Mathf.Clamp(local.y + 20 / points.y, min.y + 60 / points.y, max.y - 116 / points.y));
            float pulse = FrontendPreferences.ReducedMotion ? 1 : 1 + .12f * Mathf.Sin(Time.unscaledTime * 5);
            tutorialArrow.localScale = Vector3.one * pulse;
            tutorialAbilityRing.transform.localScale = Vector3.one * pulse;
        }

        private void DismissPausedRadialOnBackground()
        {
            if (specializationSession == null || battleRadial == null || !battleRadial.IsOpen) return;
            Vector2 screen;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                screen = Mouse.current.position.ReadValue();
            else return;
            if (EventSystem.current != null)
            {
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
                if (hits.Count > 0) return;
            }
            battleRadial.Close();
        }
    }
}
