using System;
using UnityEngine;
using AllinDefense.UI;
using UnityEngine.UI;
using TMPro;

namespace AllinDefense.Presentation
{
    /// <summary>
    /// Positions the kit frame in logical points inside the safe area.
    /// Context panels retain their existing reference-canvas layout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DefenderHudResponsiveLayout : MonoBehaviour
    {
        public readonly struct LayoutMetrics
        {
            public LayoutMetrics(float safeMinX, float safeMaxX, float safeMinY, float safeMaxY,
                float bottomInset, float topInset, float safeCenterX, float safeWidth, float previewWidth, float messageWidth,
                float previewY, float messageY)
            {
                SafeMinX = safeMinX;
                SafeMaxX = safeMaxX;
                SafeMinY = safeMinY;
                SafeMaxY = safeMaxY;
                BottomInset = bottomInset;
                TopInset = topInset;
                SafeCenterX = safeCenterX;
                SafeWidth = safeWidth;
                PreviewWidth = previewWidth;
                MessageWidth = messageWidth;
                PreviewY = previewY;
                MessageY = messageY;
            }

            public float SafeMinX { get; }
            public float SafeMaxX { get; }
            public float SafeMinY { get; }
            public float SafeMaxY { get; }
            public float BottomInset { get; }
            public float TopInset { get; }
            public float SafeCenterX { get; }
            public float SafeWidth { get; }
            public float PreviewWidth { get; }
            public float MessageWidth { get; }
            public float PreviewY { get; }
            public float MessageY { get; }
        }

        public const float TopContentHeight = 56f;
        public const float BottomContentHeight = 110f;

        public const float InformationCardWidth = 260f;
        private float informationCardHeight = 120f;
        private RectTransform root;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private RectTransform preview;
        private RectTransform message;
        private RectTransform selectedPanel;
        private Rect previousSafeArea;
        private Vector2Int previousScreenSize;
        private bool initialized;
        private bool contextualSelectionActive;
        private bool contextualSelectionIsEmptySlot;
        private Vector2 contextualTarget;

        public void Initialize()
        {
            if (initialized) return;
            root = transform as RectTransform;
            topBar = RequireRect("TopBar");
            bottomBar = RequireRect("BottomBar");
            preview = RequireRect("Preview");
            message = RequireRect("Message");
            selectedPanel = RequireRect("SelectedPanel");
            initialized = true;
            Apply(true);
        }

        private void Update()
        {
            if (!initialized) return;
            Vector2Int screenSize = new(Screen.width, Screen.height);
            if (screenSize == previousScreenSize && Screen.safeArea == previousSafeArea) return;
            Apply(true);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (initialized) Apply(false);
        }

        private void Apply(bool screenChanged)
        {
            if (root == null || root.rect.width <= 0f || root.rect.height <= 0f ||
                Screen.width <= 0 || Screen.height <= 0) return;

            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
            if (screenChanged)
            {
                previousSafeArea = safeArea;
                previousScreenSize = new Vector2Int(Screen.width, Screen.height);
            }

            LayoutMetrics metrics = CalculateLayoutMetrics(root.rect.size, safeArea, previousScreenSize);
            LayoutFrame(metrics);

            var points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            selectedPanel.anchorMin = selectedPanel.anchorMax = new Vector2(metrics.SafeMinX, metrics.SafeMinY);
            selectedPanel.pivot = Vector2.zero;
            selectedPanel.anchoredPosition = new Vector2(96 / points.x, 8 / points.y);
            selectedPanel.sizeDelta = new Vector2(InformationCardWidth, informationCardHeight);
            selectedPanel.localScale = new Vector3(1 / points.x, 1 / points.y, 1);
        }

        public void SetContextualSelectionFromScreenPoint(Vector2 screenPoint, bool emptySlot)
        {
            if (!initialized || root == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, null, out Vector2 localPoint))
                return;

            contextualSelectionActive = true;
            bool presentationChanged = contextualSelectionIsEmptySlot != emptySlot;
            bool targetMoved = (contextualTarget - localPoint).sqrMagnitude > 0.25f;
            contextualSelectionIsEmptySlot = emptySlot;
            contextualTarget = localPoint;
            if (!presentationChanged && !targetMoved && selectedPanel.gameObject.activeSelf) return;
            Apply(false);
        }

        public void ClearContextualSelection()
        {
            contextualSelectionActive = false;
        }

        public void SetInformationCardHeight(float height)
        {
            if (Mathf.Approximately(informationCardHeight, height)) return;
            informationCardHeight = height;
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (initialized) Apply(false);
        }

        public static Vector2 ClampContextPanelPosition(Vector2 desired, Vector2 panelSize, Vector2 rootSize,
            LayoutMetrics metrics, float topHeight = TopContentHeight, float bottomHeight = BottomContentHeight)
        {
            float rootMinX = -rootSize.x * 0.5f;
            float rootMinY = -rootSize.y * 0.5f;
            float minX = rootMinX + metrics.SafeMinX * rootSize.x + panelSize.x * 0.5f + 12f;
            float maxX = rootMinX + metrics.SafeMaxX * rootSize.x - panelSize.x * 0.5f - 12f;
            float minY = rootMinY + metrics.SafeMinY * rootSize.y + bottomHeight + panelSize.y * 0.5f + 12f;
            float maxY = rootMinY + metrics.SafeMaxY * rootSize.y - topHeight - panelSize.y * 0.5f - 12f;
            return new Vector2(Mathf.Clamp(desired.x, minX, Mathf.Max(minX, maxX)),
                Mathf.Clamp(desired.y, minY, Mathf.Max(minY, maxY)));
        }

        public static LayoutMetrics CalculateLayoutMetrics(Vector2 rootSize, Rect safeArea, Vector2Int screenSize)
        {
            if (rootSize.x <= 0f || rootSize.y <= 0f || screenSize.x <= 0 || screenSize.y <= 0)
                return default;

            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, screenSize.x, screenSize.y);

            float safeMinX = Mathf.Clamp01(safeArea.xMin / screenSize.x);
            float safeMaxX = Mathf.Clamp01(safeArea.xMax / screenSize.x);
            float safeMinY = Mathf.Clamp01(safeArea.yMin / screenSize.y);
            float safeMaxY = Mathf.Clamp01(safeArea.yMax / screenSize.y);
            float bottomInset = safeMinY * rootSize.y;
            float topInset = (1f - safeMaxY) * rootSize.y;
            float safeCenterX = (safeMinX + safeMaxX) * 0.5f;
            float safeWidth = rootSize.x * Mathf.Max(0.1f, safeMaxX - safeMinX);
            // Battle is landscape-only: threat and toast surfaces should remain compact so they
            // never become a second rail across the battlefield on phones or tablets.
            float previewWidth = Mathf.Min(safeWidth * 0.50f, 1000f);
            float messageWidth = Mathf.Min(safeWidth * 0.38f, 720f);
            float previewY = -(TopContentHeight + topInset + 8f);
            float messageY = previewY - 122f;
            return new LayoutMetrics(safeMinX, safeMaxX, safeMinY, safeMaxY, bottomInset, topInset,
                safeCenterX, safeWidth, previewWidth, messageWidth, previewY, messageY);
        }

        private static readonly string[] framePaths = { "TopBar/Lives", "TopBar/Gold", "TopBar/WavePanel", "TopBar/Speed", "TopBar/Pause",
            "Preview", "TopBar/WaveForecast", "MessageChrome", "Message", "BottomBar/AbilityCooldown", "BottomBar/Hero", "BottomBar/NextWave", "SelectedPanel" };
        private RectTransform[] frameRects;
        private readonly Vector3[] frameCorners = new Vector3[4];
        public Rect[] FrameScreenRects()
        {
            var result = new System.Collections.Generic.List<Rect>();
            AppendFrameScreenRects(result);
            return result.ToArray();
        }

        public void AppendFrameScreenRects(System.Collections.Generic.List<Rect> result, bool includeInformationCard = true)
        {
            if (frameRects == null)
            {
                frameRects = new RectTransform[framePaths.Length];
                for (int i = 0; i < framePaths.Length; i++) frameRects[i] = transform.Find(framePaths[i]) as RectTransform;
            }
            foreach (var rect in frameRects)
            {
                if (rect == null || !rect.gameObject.activeInHierarchy || !includeInformationCard && rect == selectedPanel) continue;
                rect.GetWorldCorners(frameCorners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(null, frameCorners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(null, frameCorners[2]);
                result.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
            }
        }

        // Screen area the information card can occupy, shown or hidden, so the radial can reserve it up front.
        public Rect InformationCardScreenRect(float minimumHeight)
        {
            selectedPanel.GetWorldCorners(frameCorners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, frameCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, frameCorners[2]);
            float pixelsPerPoint = (max.y - min.y) / informationCardHeight;
            return Rect.MinMaxRect(min.x, min.y, max.x, min.y + Mathf.Max(informationCardHeight, minimumHeight) * pixelsPerPoint);
        }

        private void LayoutFrame(LayoutMetrics metrics)
        {
            Vector2 points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            Vector3 scale = new(1 / points.x, 1 / points.y, 1);
            float width = metrics.SafeWidth * points.x;
            bool compact = width < 760 || selectedPanel.gameObject.activeSelf && width < 900;
            Place(topBar, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY), new Vector2(.5f, 1), Vector2.zero, new Vector2(width, TopContentHeight));
            Place(bottomBar, new Vector2(metrics.SafeCenterX, metrics.SafeMinY), new Vector2(.5f, 0), Vector2.zero, new Vector2(width, BottomContentHeight));
            topBar.localScale = bottomBar.localScale = scale;
            Place(RequireRect(topBar, "Lives"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -6), new Vector2(76, 44));
            Place(RequireRect(topBar, "Gold"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -6), new Vector2(compact ? 124 : 148, 44));
            var wave = RequireRect(topBar, "WavePanel");
            float waveWidth = compact ? 200 : 220;
            float waveLeft = Mathf.Max(compact ? 222 : 246, width * .5f - waveWidth * .5f);
            waveLeft = Mathf.Min(waveLeft, width - 140 - 80 - waveWidth);
            Place(wave, new Vector2(0, 1), new Vector2(0, 1), new Vector2(waveLeft, -6), new Vector2(waveWidth, compact ? 44 : 52));
            Place(RequireRect(wave, "Wave"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(compact ? 86 : 100, 22));
            Place(RequireRect(wave, "PhaseChip"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(92, 22));
            foreach (Transform child in wave)
                if (child.name == "WaveProgress" && child.gameObject.activeSelf)
                    Place((RectTransform)child, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, compact ? 4 : 8), new Vector2(compact ? 180 : 200, 8));
            var boss = RequireRect(wave, "BossBar");
            AllinDefense.UI.Kit.UiKit.Stretch(boss);
            Place(RequireRect(boss, "BossName"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -5), new Vector2(128, 24));
            Place(RequireRect(boss, "BossHealth"), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(72, 22));
            Place(RequireRect(boss, "BossTrack"), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, compact ? 4 : 8), new Vector2(compact ? 180 : 200, 8));
            Place(RequireRect(topBar, "Speed"), Vector2.one, Vector2.one, new Vector2(-56, -6), new Vector2(76, 44));
            Place(RequireRect(topBar, "Pause"), Vector2.one, Vector2.one, new Vector2(-6, -6), new Vector2(44, 44));
            Place(preview, new Vector2(metrics.SafeMaxX, metrics.SafeMaxY), new Vector2(1, 1), new Vector2(-140 / points.x, -16 / points.y), new Vector2(72, 24));
            preview.localScale = scale;
            bool selection = selectedPanel.gameObject.activeSelf;
            float chromeWidth = Mathf.Min(378, width - (selection ? 372 : 32));
            float messageWidth = chromeWidth - 48;
            float messageHeight = Mathf.Max(36, Mathf.Ceil(message.GetComponent<TMP_Text>().GetPreferredValues(
                message.GetComponent<TMP_Text>().text, messageWidth, 0).y));
            Vector2 messageAnchor = new(selection ? metrics.SafeMaxX : metrics.SafeCenterX, metrics.SafeMinY);
            Vector2 messagePivot = new(selection ? 1 : .5f, 0);
            const float edge = UiTokens.Space1;
            const float abilityBottom = edge;
            const float heroBottom = abilityBottom + 88 + edge;
            const float heroRight = edge + 208 + edge;
            // Reserve clearance throughout the toast's downward entrance motion.
            float messageBottom = selection ? Mathf.Max(heroBottom + 18, edge + informationCardHeight) + 2 * edge :
                (width + chromeWidth) * .5f > width - heroRight - 80 - edge ? heroBottom + 18 + 2 * edge : 2 * edge;
            Vector2 chromePosition = new(selection ? -8 / points.x : 0, messageBottom / points.y);
            var messageChrome = RequireRect("MessageChrome");
            Place(messageChrome, messageAnchor, messagePivot, chromePosition, new Vector2(chromeWidth, messageHeight + 8));
            messageChrome.localScale = scale;
            Vector2 textPosition = chromePosition + new Vector2((selection ? -10 : 14) / points.x, 4 / points.y);
            Place(message, messageAnchor, messagePivot, textPosition, new Vector2(messageWidth, messageHeight));
            message.localScale = scale;
            Place(RequireRect(bottomBar, "AbilityCooldown"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-heroRight, abilityBottom), new Vector2(64, 88));
            Place(RequireRect(bottomBar, "Hero"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-heroRight + edge, heroBottom), new Vector2(80, 18));
            Place(RequireRect(bottomBar, "NextWave"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 8), new Vector2(208, 54));
            foreach (string path in new[] { "PauseOverlay", "MapConfirmation" })
            {
                var modal = RequireRect(path);
                Place(modal, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.Scale(root.rect.size, points));
                modal.localScale = scale;
            }
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private RectTransform RequireRect(string path)
        {
            Transform item = transform.Find(path);
            if (item is RectTransform rect) return rect;
            throw new InvalidOperationException($"HUD layout is missing RectTransform at {path}.");
        }

        private static RectTransform RequireRect(RectTransform parent, string path)
        {
            Transform item = parent.Find(path);
            if (item is RectTransform rect) return rect;
            throw new InvalidOperationException($"HUD layout is missing RectTransform at {parent.name}/{path}.");
        }

    }
}
