using System;
using UnityEngine;

namespace DefenderChronicles.Presentation
{
    /// <summary>
    /// Extends decorative HUD rails to the physical screen edges while keeping interactive
    /// content inside the safe area. Existing prefab references stay intact after reparenting.
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

        public const float TopContentHeight = 146f;
        public const float BottomContentHeight = 170f;

        private RectTransform root;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private RectTransform topContent;
        private RectTransform bottomContent;
        private RectTransform preview;
        private RectTransform previewChrome;
        private RectTransform previewTitle;
        private RectTransform message;
        private RectTransform messageChrome;
        private RectTransform selectedPanel;
        private Rect previousSafeArea;
        private Vector2Int previousScreenSize;
        private bool initialized;

        public void Initialize()
        {
            if (initialized) return;
            root = transform as RectTransform;
            topBar = RequireRect("TopBar");
            bottomBar = RequireRect("BottomBar");
            preview = RequireRect("Preview");
            previewChrome = FindOptionalRect("PreviewChrome");
            previewTitle = FindOptionalRect("PreviewTitle");
            message = RequireRect("Message");
            messageChrome = FindOptionalRect("MessageChrome");
            selectedPanel = RequireRect("SelectedPanel");
            topContent = CreateContentRoot(topBar, "SafeContent");
            bottomContent = CreateContentRoot(bottomBar, "SafeContent");

            MoveChildren(topBar, topContent);
            MoveChildren(bottomBar, bottomContent);
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
            SetEdgeRail(topBar, true, TopContentHeight + metrics.TopInset);
            SetEdgeRail(bottomBar, false, BottomContentHeight + metrics.BottomInset);
            SetSafeContent(topContent, metrics.SafeMinX, metrics.SafeMaxX, 0f, -metrics.TopInset);
            SetSafeContent(bottomContent, metrics.SafeMinX, metrics.SafeMaxX, metrics.BottomInset, 0f);
            LayoutTopContent();
            LayoutBottomContent();

            if (previewChrome != null)
                SetTopCenteredBanner(previewChrome, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY), metrics.PreviewY,
                    new Vector2(metrics.PreviewWidth, 116f));
            if (previewTitle != null)
                SetTopCenteredBanner(previewTitle, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY),
                    metrics.PreviewY - 8f, new Vector2(metrics.PreviewWidth - 56f, 18f));
            SetTopCenteredBanner(preview, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY),
                metrics.PreviewY - 27f, new Vector2(metrics.PreviewWidth - 56f, 80f));
            if (messageChrome != null)
                SetTopCenteredBanner(messageChrome, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY),
                    metrics.MessageY, new Vector2(metrics.MessageWidth, 82f));
            SetTopCenteredBanner(message, new Vector2(metrics.SafeCenterX, metrics.SafeMaxY),
                metrics.MessageY - 4f, new Vector2(metrics.MessageWidth - 40f, 72f));

            selectedPanel.anchorMin = selectedPanel.anchorMax = new Vector2(metrics.SafeMaxX, metrics.SafeMinY);
            selectedPanel.pivot = new Vector2(1f, 0f);
            selectedPanel.anchoredPosition = new Vector2(-18f, BottomContentHeight + 18f);
            selectedPanel.sizeDelta = new Vector2(Mathf.Min(440f, metrics.SafeWidth * 0.34f), 480f);
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

        private void LayoutTopContent()
        {
            SetRange(RequireRect(topContent, "Gold"), 0f, 0.15f, 7f, 7f);
            SetRange(RequireRect(topContent, "Lives"), 0.152f, 0.285f, 7f, 7f);
            SetRange(RequireRect(topContent, "Wave"), 0.287f, 0.43f, 7f, 7f);
            MatchOptionalChrome(topContent, "Gold", "GoldChrome");
            MatchOptionalChrome(topContent, "Lives", "LivesChrome");
            MatchOptionalChrome(topContent, "Wave", "WaveChrome");
            RectTransform phase = RequireRect(topContent, "Phase");
            SetRange(phase, 0.44f, 0.67f, 7f, 7f);
            RectTransform phaseChrome = FindOptionalRect(topContent, "PhaseChrome");
            if (phaseChrome != null) MatchRect(phase, phaseChrome);
            RectTransform speed = FindOptionalRect(topContent, "Speed");
            if (speed != null) SetRange(speed, 0.80f, 0.895f, 8f, 8f);
            SetRange(RequireRect(topContent, "Pause"), 0.90f, 1f, 8f, 8f);
        }

        private void LayoutBottomContent()
        {
            SetButtonRange(RequireRect(bottomContent, "Archer"), 0.055f, 0.175f);
            SetButtonRange(RequireRect(bottomContent, "Barracks"), 0.18f, 0.30f);
            SetButtonRange(RequireRect(bottomContent, "Mage"), 0.305f, 0.425f);
            SetButtonRange(RequireRect(bottomContent, "Ability"), 0.44f, 0.575f);
            SetButtonRange(RequireRect(bottomContent, "Cancel"), 0.585f, 0.665f);
            SetButtonRange(RequireRect(bottomContent, "NextWave"), 0.73f, 0.965f);

            RectTransform buildDeck = FindOptionalRect(bottomContent, "BuildDeck");
            if (buildDeck != null) SetRange(buildDeck, 0.045f, 0.435f, 3f, 3f);
            RectTransform heroDeck = FindOptionalRect(bottomContent, "HeroDeck");
            if (heroDeck != null) SetRange(heroDeck, 0.435f, 0.68f, 3f, 3f);
            RectTransform commandDeck = FindOptionalRect(bottomContent, "CommandDeck");
            if (commandDeck != null) SetRange(commandDeck, 0.70f, 0.975f, 3f, 3f);

            RectTransform hero = RequireRect(bottomContent, "Hero");
            hero.anchorMin = new Vector2(0.44f, 0f);
            hero.anchorMax = new Vector2(0.675f, 0f);
            hero.pivot = Vector2.zero;
            hero.offsetMin = new Vector2(8f, 3f);
            hero.offsetMax = new Vector2(-8f, 27f);
        }

        private static void SetButtonRange(RectTransform rect, float minX, float maxX)
        {
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 0f);
            rect.pivot = Vector2.zero;
            rect.offsetMin = new Vector2(5f, 27f);
            rect.offsetMax = new Vector2(-5f, 158f);
        }

        private static void SetRange(RectTransform rect, float minX, float maxX, float bottom, float top)
        {
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.pivot = Vector2.zero;
            rect.offsetMin = new Vector2(8f, bottom);
            rect.offsetMax = new Vector2(-8f, -top);
        }

        private static void SetTopCenteredBanner(RectTransform rect, Vector2 anchor, float y, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }

        private static void SetEdgeRail(RectTransform rect, bool top, float height)
        {
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = Vector2.zero;
            rect.offsetMin = new Vector2(0f, top ? -height : 0f);
            rect.offsetMax = new Vector2(0f, top ? 0f : height);
        }

        private static void SetSafeContent(RectTransform rect, float minX, float maxX, float bottom, float top)
        {
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(0f, top);
        }

        private static RectTransform CreateContentRoot(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect) return existingRect;
            var content = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(parent, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            return content;
        }

        private static void MoveChildren(RectTransform source, RectTransform destination)
        {
            for (int index = source.childCount - 1; index >= 0; index--)
            {
                Transform child = source.GetChild(index);
                if (child == destination) continue;
                child.SetParent(destination, false);
                child.SetAsFirstSibling();
            }
        }

        private RectTransform RequireRect(string path)
        {
            Transform item = transform.Find(path);
            if (item is RectTransform rect) return rect;
            throw new InvalidOperationException($"HUD layout is missing RectTransform at {path}.");
        }

        private RectTransform FindOptionalRect(string path)
        {
            Transform item = transform.Find(path);
            return item as RectTransform;
        }

        private static RectTransform RequireRect(RectTransform parent, string path)
        {
            Transform item = parent.Find(path);
            if (item is RectTransform rect) return rect;
            throw new InvalidOperationException($"HUD layout is missing RectTransform at {parent.name}/{path}.");
        }

        private static RectTransform FindOptionalRect(RectTransform parent, string path)
        {
            Transform item = parent.Find(path);
            return item as RectTransform;
        }

        private static void MatchOptionalChrome(RectTransform parent, string sourceName, string chromeName)
        {
            RectTransform source = RequireRect(parent, sourceName);
            RectTransform chrome = FindOptionalRect(parent, chromeName);
            if (chrome != null) MatchRect(source, chrome);
        }

        private static void MatchRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
        }
    }
}
