using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AllinDefense.UI.Kit
{
    public sealed class UiOverlay : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card, body;
        [SerializeField] private bool bottom;
        [SerializeField] private UnityEngine.UI.Button close;
        [SerializeField] private bool dismissOnScrim = true;
        public bool DismissOnScrim { get => dismissOnScrim; set => dismissOnScrim = value; }
        private UiMotion.Handle fade;
        private Component previousFocus;
        private bool focusCaptured;
        private bool focusPending;
        private GameObject lastSelection;
        private Component firstFocus;
        private readonly List<UiAccessibleButton> buttons = new();
        private readonly List<UiAccessibleText> texts = new();
        private readonly List<Component> focusCandidates = new();
        public void SetFirstFocus(Component target) { firstFocus = target; focusPending = true; }
        public RectTransform Body => body;
        public bool IsOpen { get; private set; } = true;
        public void Configure(CanvasGroup canvasGroup, RectTransform panel, RectTransform content, bool sheet, UnityEngine.UI.Button closeButton)
        { group = canvasGroup; card = panel; body = content; bottom = sheet; close = closeButton; Wire(); BeginScope(); }
        private void OnEnable() { Wire(); if (group != null) BeginScope(); }
        private void Wire() { if (close != null) { close.onClick.RemoveListener(Hide); close.onClick.AddListener(Hide); } }
        public void Show()
        {
            gameObject.SetActive(true); fade?.Cancel(); IsOpen = true;
            BeginScope();
            UiSounds.Click();
            group.blocksRaycasts = true; group.interactable = false;
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            fade = motion.Animate(t =>
            {
                group.alpha = t;
                card.localScale = Vector3.one * Mathf.Lerp(UiTokens.PressScale, 1, t);
            }, UiTokens.FadeDuration, () => { group.interactable = true; FocusFirst(); });
        }
        public void Hide() => Hide(null);
        public void Hide(System.Action completed)
        {
            if (!IsOpen) return;
            fade?.Cancel(); IsOpen = false; group.interactable = false;
            UiSounds.Click();
            float from = group.alpha;
            var motion = GetComponent<UiMotion>() ?? gameObject.AddComponent<UiMotion>();
            fade = motion.Animate(t =>
            {
                group.alpha = from * (1 - t);
                card.localScale = Vector3.one * Mathf.Lerp(1, UiTokens.PressScale, t);
            }, UiTokens.FadeDuration, () => { group.blocksRaycasts = false; gameObject.SetActive(false); EndScope(); completed?.Invoke(); });
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (dismissOnScrim && !RectTransformUtility.RectangleContainsScreenPoint(card, eventData.position, eventData.pressEventCamera)) Hide();
        }
        private void BeginScope()
        {
            if (bottom) return;
            if (!focusCaptured)
            {
                previousFocus = UiAccessibleButton.FocusedElement;
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
                    previousFocus = EventSystem.current.currentSelectedGameObject.GetComponent<UiAccessibleButton>() ?? previousFocus;
                focusCaptured = true;
            }
            var scope = GetComponent<UiAccessibleButton>() ?? gameObject.AddComponent<UiAccessibleButton>();
            scope.enabled = true; scope.ConfigureModalScope();
            focusPending = true;
        }
        private void LateUpdate()
        {
            if (bottom) return;
            if (!IsOpen || group == null || !group.interactable) return;
            var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
            if (focusPending || selected != lastSelection && selected != null && !selected.transform.IsChildOf(transform)
                && UiAccessibleButton.IsInAccessibleScope(transform)) FocusFirst();
            lastSelection = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
        }
        public void FocusFirst()
        {
            if (bottom) return;
            focusPending = false;
            UiAccessibleButton.RefreshAll();
            if (firstFocus != null && firstFocus.transform.IsChildOf(transform) && UiAccessibleButton.Focus(firstFocus))
            { lastSelection = firstFocus.gameObject; return; }
            GetComponentsInChildren(false, buttons);
            GetComponentsInChildren(false, texts);
            focusCandidates.Clear();
            foreach (var button in buttons) focusCandidates.Add(button);
            foreach (var text in texts) focusCandidates.Add(text);
            focusCandidates.Sort(CompareFocusOrder);
            foreach (var component in focusCandidates)
            {
                if (UiAccessibleButton.Focus(component))
                { lastSelection = component.gameObject; return; }
            }
            lastSelection = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
        }
        private static int CompareFocusOrder(Component left, Component right) => left.transform == right.transform ? 0 :
            UiAccessibleButton.IsAbove(left.transform, right.transform) ? 1 : -1;
        private void OnDisable() => EndScope();
        private void EndScope()
        {
            fade?.Cancel(); focusPending = false;
            if (!focusCaptured) return;
            var scope = GetComponent<UiAccessibleButton>();
            if (scope != null) scope.enabled = false;
            UiAccessibleButton.RefreshAll();
            if (focusCaptured) UiAccessibleButton.Focus(previousFocus);
            previousFocus = null; focusCaptured = false;
        }
    }
}
