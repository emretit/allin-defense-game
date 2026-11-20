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

        public const float TopContentHeight = 50f;
        public const float BottomContentHeight = 110f;

        public const float InformationCardWidth = 260f;
        public const float InformationCardMaxHeight = 148f;
        private const float ToastHeight = 64f;
        public const float AbilityDiameter = 76f;
        private const float AbilityRightInset = UiTokens.Space1;
        private const float AbilityHeight = AbilityDiameter + 24;
        private const float NextWaveWidth = 208f;
        private const float NextWaveRightInset = AbilityRightInset + AbilityDiameter + UiTokens.Space1;
        private const float HeroLabelBottom = UiTokens.Space1 + AbilityHeight + UiTokens.Space1;
        private const float BottomClusterWidth = NextWaveRightInset + NextWaveWidth;
        private int informationCardCorner;
        private float informationCardClearance = UiTokens.Space1;
        private Vector2 messageRestPosition, messageChromeRestPosition;
        private float messageEntranceOffset;
        private LayoutMetrics currentMetrics;
        private float informationCardHeight = 120f;
        private RectTransform root;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private RectTransform message;
        private RectTransform selectedPanel;
        private Rect previousSafeArea;
        private Vector2Int previousScreenSize;
        private bool initialized;
        private bool contextualSelectionActive;
        private Vector2 contextualTarget;

        public void Initialize()
        {
            if (initialized) return;
            root = transform as RectTransform;
            topBar = RequireRect("TopBar");
            bottomBar = RequireRect("BottomBar");
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
            ApplyLayout(metrics);
        }

        private void ApplyLayout(LayoutMetrics metrics)
        {
            currentMetrics = metrics;
            LayoutFrame(metrics);
            var points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            float width = metrics.SafeWidth * points.x;
            float height = (metrics.SafeMaxY - metrics.SafeMinY) * root.rect.height * points.y;
            Rect reserve = CardCornerRect(informationCardCorner, width, height, informationCardClearance);
            bool top = informationCardCorner == 2 || informationCardCorner == 3 || informationCardCorner >= 5;
            selectedPanel.anchorMin = selectedPanel.anchorMax = new Vector2(metrics.SafeMinX, metrics.SafeMinY);
            selectedPanel.pivot = new Vector2(0, top ? 1 : 0);
            selectedPanel.anchoredPosition = new Vector2(reserve.x / points.x, (top ? reserve.yMax : reserve.yMin) / points.y);
            selectedPanel.sizeDelta = new Vector2(InformationCardWidth, informationCardHeight);
            selectedPanel.localScale = new Vector3(1 / points.x, 1 / points.y, 1);
            // This card contains information only; world slots underneath must remain reachable.
            foreach (var graphic in selectedPanel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        }

        private static float ToastWidth(float width) => Mathf.Min(360, width - BottomClusterWidth - 3 * UiTokens.Space1);

        private static Rect CardCornerRect(int corner, float width, float height, float gap)
        {
            bool right = corner % 2 == 1;
            float x = right ? width - InformationCardWidth - UiTokens.Space1 : UiTokens.Space1;
            float bottom = corner >= 2 ? height - TopContentHeight - gap - InformationCardMaxHeight :
                right ? HeroLabelBottom + 18 + gap : ToastHeight + UiTokens.Space1 + gap;
            if (corner == 4) { x = width - AbilityRightInset - 120 - gap - InformationCardWidth; bottom = ToastHeight + UiTokens.Space1 + gap; }
            if (corner == 5) x = width * .3f - gap - InformationCardWidth;
            if (corner == 6) x = width * .7f + gap;
            return new Rect(x, bottom, InformationCardWidth, InformationCardMaxHeight);
        }

        private void ChooseInformationCardCorner()
        {
            var points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            float width = currentMetrics.SafeWidth * points.x;
            float height = (currentMetrics.SafeMaxY - currentMetrics.SafeMinY) * root.rect.height * points.y;
            Vector2 safeOrigin = root.rect.min + Vector2.Scale(root.rect.size,
                new Vector2(currentMetrics.SafeMinX, currentMetrics.SafeMinY));
            Vector2 target = Vector2.Scale(contextualTarget - safeOrigin, points);
            Rect touch = new(target - Vector2.one * (DefenderSlotVisual.MinimumTouchPoints / 2),
                Vector2.one * DefenderSlotVisual.MinimumTouchPoints);
            // Reserve persistent frame slots even when their current content is hidden.
            Rect[] obstacles = {
                new(0, height - TopContentHeight, width, TopContentHeight),
                new(width * .3f, height - TopContentHeight - 40, width * .4f, 32),
                new(8, 8, ToastWidth(width), ToastHeight),
                new(width - AbilityRightInset - AbilityDiameter - 4, 4, AbilityDiameter + 8, AbilityHeight + 4),
                new(width - AbilityRightInset - 120, HeroLabelBottom, 120, 18),
                new(width - BottomClusterWidth, 8, NextWaveWidth, 54)
            };
            informationCardCorner = 0;
            informationCardClearance = UiTokens.Space1;
            for (int side = 0; side < 2; side++)
            foreach (float gap in new[] { (float)UiTokens.Space1, 1f })
            {
                float bestDistance = -1;
                for (int corner = 0; corner < 7; corner++)
                {
                    Rect candidate = CardCornerRect(corner, width, height, gap);
                    bool opposite = (candidate.center.x > width / 2) != (target.x > width / 2);
                    if (opposite != (side == 0)) continue;
                    if (candidate.Overlaps(touch) || candidate.xMin < 0 || candidate.yMin < 0 ||
                        candidate.xMax > width || candidate.yMax > height) continue;
                    bool clear = true;
                    var clearance = Rect.MinMaxRect(candidate.xMin - gap, candidate.yMin - gap,
                        candidate.xMax + gap, candidate.yMax + gap);
                    foreach (var obstacle in obstacles)
                        if (clearance.Overlaps(obstacle)) { clear = false; break; }
                    float distance = (candidate.center - target).sqrMagnitude;
                    if (clear && distance > bestDistance)
                    {
                        bestDistance = distance; informationCardCorner = corner; informationCardClearance = gap;
                    }
                }
                if (bestDistance >= 0) return;
            }
        }

        public void SetContextualSelectionFromScreenPoint(Vector2 screenPoint, bool emptySlot)
        {
            if (!initialized || root == null || contextualSelectionActive) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, null, out contextualTarget)) return;
            contextualSelectionActive = true;
            ChooseInformationCardCorner();
            Apply(false);
        }

        public void ClearContextualSelection()
        {
            contextualSelectionActive = false;
        }

        public void SetInformationCardHeight(float height)
        {
            height = Mathf.Clamp(height, 120, InformationCardMaxHeight);
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

        private static readonly string[] framePaths = { "TopBar/Gold", "TopBar/WavePanel", "TopBar/Speed", "TopBar/Pause",
            "TopBar/WaveForecast", "MessageChrome", "Message", "BottomBar/AbilityCooldown", "BottomBar/AbilityCooldown/AbilityCooldown.Action/AbilityReadyGlow", "BottomBar/Hero", "BottomBar/NextWave", "SelectedPanel" };
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

        public Rect ToastScreenRect()
        {
            var points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            Vector2 origin = root.rect.min + Vector2.Scale(root.rect.size,
                new Vector2(currentMetrics.SafeMinX, currentMetrics.SafeMinY));
            Vector2 min = origin + new Vector2(8 / points.x, 8 / points.y);
            Vector2 max = min + new Vector2(ToastWidth(currentMetrics.SafeWidth * points.x) / points.x, ToastHeight / points.y);
            min = RectTransformUtility.WorldToScreenPoint(null, root.TransformPoint(min));
            max = RectTransformUtility.WorldToScreenPoint(null, root.TransformPoint(max));
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // Screen area the information card can occupy, shown or hidden, so the radial can reserve it up front.
        public Rect InformationCardScreenRect(float minimumHeight)
        {
            selectedPanel.GetWorldCorners(frameCorners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, frameCorners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, frameCorners[2]);
            float pixelsPerPoint = (max.y - min.y) / informationCardHeight;
            float height = Mathf.Max(informationCardHeight, minimumHeight) * pixelsPerPoint;
            return selectedPanel.pivot.y > .5f ? Rect.MinMaxRect(min.x, max.y - height, max.x, max.y) :
                Rect.MinMaxRect(min.x, min.y, max.x, min.y + height);
        }

        private void LayoutFrame(LayoutMetrics metrics)
        {
            Vector2 points = UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>());
            Vector3 scale = new(1 / points.x, 1 / points.y, 1);
            float width = metrics.SafeWidth * points.x;
            Place(topBar, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY), new Vector2(.5f, 1), Vector2.zero, new Vector2(width, TopContentHeight));
            Place(bottomBar, new Vector2(metrics.SafeCenterX, metrics.SafeMinY), new Vector2(.5f, 0), Vector2.zero, new Vector2(width, BottomContentHeight));
            topBar.localScale = bottomBar.localScale = scale;
            Place(RequireRect(topBar, "Gold"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -6), new Vector2(96, 44));
            var wave = RequireRect(topBar, "WavePanel");
            const float waveWidth = 164;
            float waveLeft = Mathf.Max(110, width * .5f - waveWidth * .5f);
            waveLeft = Mathf.Min(waveLeft, width - 124 - waveWidth);
            Place(wave, new Vector2(0, 1), new Vector2(0, 1), new Vector2(waveLeft, -6), new Vector2(waveWidth, 44));
            Place(RequireRect(wave, "Wave"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(100, 22));
            foreach (Transform child in wave)
                if (child.name == "WaveProgress" && child.gameObject.activeSelf)
                    Place((RectTransform)child, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 4), new Vector2(waveWidth - 20, 8));
            var boss = RequireRect(wave, "BossBar");
            AllinDefense.UI.Kit.UiKit.Stretch(boss);
            Place(RequireRect(boss, "BossName"), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -5), new Vector2(waveWidth - 20, 22));
            Place(RequireRect(boss, "BossHealth"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-8, 4), new Vector2(64, 14));
            Place(RequireRect(boss, "BossTrack"), new Vector2(0, 0), new Vector2(0, 0), new Vector2(10, 7), new Vector2(waveWidth - 92, 8));
            Place(RequireRect(topBar, "Speed"), Vector2.one, Vector2.one, new Vector2(-56, -6), new Vector2(60, 44));
            Place(RequireRect(topBar, "Pause"), Vector2.one, Vector2.one, new Vector2(-6, -6), new Vector2(44, 44));
            float chromeWidth = ToastWidth(width);
            float messageWidth = chromeWidth - 48;
            float messageHeight = Mathf.Clamp(Mathf.Ceil(message.GetComponent<TMP_Text>().GetPreferredValues(
                message.GetComponent<TMP_Text>().text, messageWidth, 0).y), 36, ToastHeight - 8);
            Vector2 messageAnchor = new(metrics.SafeMinX, metrics.SafeMinY);
            const float edge = UiTokens.Space1;
            const float abilityBottom = edge;
            messageChromeRestPosition = new Vector2(edge / points.x, edge / points.y);
            var messageChrome = RequireRect("MessageChrome");
            Place(messageChrome, messageAnchor, Vector2.zero, messageChromeRestPosition, new Vector2(chromeWidth, messageHeight + 8));
            messageChrome.localScale = scale;
            messageRestPosition = messageChromeRestPosition + new Vector2(38 / points.x, 4 / points.y);
            Place(message, messageAnchor, Vector2.zero, messageRestPosition, new Vector2(messageWidth, messageHeight));
            message.localScale = scale;
            SetMessageEntranceOffset(messageEntranceOffset);
            Place(RequireRect(bottomBar, "AbilityCooldown"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-AbilityRightInset, abilityBottom), new Vector2(AbilityDiameter, AbilityHeight));
            Place(RequireRect(bottomBar, "Hero"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-AbilityRightInset, HeroLabelBottom), new Vector2(120, 18));
            Place(RequireRect(bottomBar, "NextWave"), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-NextWaveRightInset, 8), new Vector2(NextWaveWidth, 54));
            foreach (string path in new[] { "PauseOverlay", "MapConfirmation", "RestartConfirmation" })
            {
                var modal = RequireRect(path);
                Place(modal, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.Scale(root.rect.size, points));
                modal.localScale = scale;
            }
        }

        public void SetMessageEntranceOffset(float offset)
        {
            messageEntranceOffset = offset;
            if (!initialized) return;
            float y = offset / UiMetrics.PointsPerCanvasUnit(GetComponentInParent<Canvas>()).y;
            RequireRect("MessageChrome").anchoredPosition = messageChromeRestPosition + Vector2.up * y;
            message.anchoredPosition = messageRestPosition + Vector2.up * y;
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
