#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AllinDefense.UI;
using AllinDefense.UI.Kit;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AllinDefense.Editor
{
    public static class UiKitShowcaseBuilder
    {
        [MenuItem("ALLIN: Defense/Render UI Kit Showcase")]
        public static void Render()
        {
            UiDesignSystemAssetBuilder.BuildSystemIcons();
            bool reduced = FrontendPreferences.ReducedMotion;
            try
            {
                FrontendPreferences.ReducedMotion = true;
                RenderSize(2622, 1206, "Builds/ui-kit-showcase.png");
                RenderSize(1206, 2622, "Builds/ui-kit-showcase-portrait.png");
            }
            finally { FrontendPreferences.ReducedMotion = reduced; }
        }

        private static void RenderSize(int width, int height, string path)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var root = new GameObject("UiKitShowcase");
            SceneManager.MoveGameObjectToScene(root, scene);
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var old = RenderTexture.active;
            try
            {
                var cameraObject = new GameObject("ShowcaseCamera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UiTokens.Night;
                camera.orthographic = true; camera.orthographicSize = height / 2f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 100; camera.targetTexture = texture;
                camera.cullingMask = 1 << 31;
                camera.scene = scene;
                var canvasObject = new GameObject("ShowcaseCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
                var kit = new UiKit(AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>(UiDesignSystemAssetBuilder.SetPath));
                var frame = UiKit.Node(canvas.transform, "Showcase.Frame"); UiKit.Stretch(frame);
                var layout = UiKit.Layout(frame, true, UiTokens.Space3);
                layout.childAlignment = TextAnchor.UpperLeft;
                var heading = kit.Text(frame, "Showcase.Title", "ALLIN: DEFENSE  /  UI KIT", UiTokens.TypeHeading, true);
                heading.color = UiTokens.GoldHigh;
                kit.Text(frame, "Showcase.Subtitle", "TEMİZ FANTASTİK  ·  PAYLAŞILAN BİLEŞENLER  ·  44 / 52 / 72 PT", UiTokens.TypeCaption).color = UiTokens.TextSecondary;
                var grid = UiKit.Node(frame, "Showcase.Grid"); UiKit.Size(grid, -1, -1, 1).flexibleHeight = 1;
                var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
                int columns = width > height ? 4 : 2;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gridLayout.constraintCount = columns;
                gridLayout.spacing = Vector2.one * UiTokens.Space2;
                float gridHeight = height - UiTokens.Space3 * 2 - UiTokens.TypeHeading * 1.4f - UiTokens.TypeCaption * 1.4f - UiTokens.Space1 * 2;
                gridLayout.cellSize = new Vector2((width - UiTokens.Space3 * 2 - (columns - 1) * UiTokens.Space2) / columns,
                    (gridHeight - (12 / columns - 1) * UiTokens.Space2) / (12 / columns));
                BuildCards(kit, grid);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
                Canvas.ForceUpdateCanvases();
                foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                foreach (var radial in root.GetComponentsInChildren<UiRadialMenu>())
                {
                    var rect = (RectTransform)radial.transform;
                    radial.OpenLocal(radial.name.EndsWith(".Edge") ? new Vector2(rect.rect.center.x, rect.rect.yMin + UiTokens.Space1) : rect.rect.center, rect.rect);
                    radial.Choose(0);
                }
                Canvas.ForceUpdateCanvases();
                foreach (var text in root.GetComponentsInChildren<TMP_Text>())
                {
                    text.ForceMeshUpdate();
                    if (!string.IsNullOrWhiteSpace(text.text) && text.textInfo.characterCount == 0)
                        throw new InvalidOperationException("Showcase text was clipped: " + text.name + " " + text.rectTransform.rect);
                }
                camera.Render();
                RenderTexture.active = texture;
                var png = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try { png.ReadPixels(new Rect(0, 0, width, height), 0, 0); png.Apply(); Directory.CreateDirectory("Builds"); File.WriteAllBytes(path, png.EncodeToPNG()); }
                finally { UnityEngine.Object.DestroyImmediate(png); }
                Debug.Log("[UI_KIT_SHOWCASE] " + path + " " + width + "x" + height);
            }
            finally
            {
                RenderTexture.active = old;
                UnityEngine.Object.DestroyImmediate(root);
                texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static RectTransform Section(UiKit kit, Transform parent, string id, string title)
        {
            var card = kit.Card(parent, id);
            var layout = card.GetComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(12, 12, 8, 8); layout.spacing = UiTokens.Space1 * .75f; layout.childAlignment = TextAnchor.UpperCenter;
            var label = kit.Text(card, id + ".Heading", title, UiTokens.TypeCaption, true); label.color = UiTokens.Moon;
            return card;
        }
        private static RectTransform Row(Transform parent, string id)
        {
            var row = UiKit.Node(parent, id); UiKit.Layout(row, false, 0); return row;
        }
        private static Sprite Painted(string name) => AssetDatabase.FindAssets(name + " t:Texture2D", new[] { "Assets/Art" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Sprite>).FirstOrDefault(s => s != null && s.name == name);

        private static void BuildCards(UiKit kit, Transform grid)
        {
            var surfaces = Section(kit, grid, "Surfaces", "01 / SURFACE · CARD");
            float[] radii = { UiTokens.RadiusSmall, UiTokens.RadiusMedium, UiTokens.RadiusCard, UiTokens.RadiusMedium, UiTokens.RadiusCard };
            string[] surfaceStates = { "NORMAL", "BASILI", "PASİF", "SEÇİLİ", "HAZIR" };
            foreach (UiState state in Enum.GetValues(typeof(UiState)))
            {
                float radius = radii[(int)state];
                var card = kit.Surface(surfaces, "Surface." + state, radius, false);
                card.GetComponent<HorizontalLayoutGroup>().padding.top = card.GetComponent<HorizontalLayoutGroup>().padding.bottom = (int)UiTokens.Space1;
                var glyph = kit.Icon(card, card.name + ".Shield", kit.Assets.iconShield);
                var label = kit.Text(card, card.name + ".Label", surfaceStates[(int)state] + " · Kale 20 · R" + radius, UiTokens.TypeCaption);
                card.GetComponent<UiVisualState>().BindContent(label, glyph);
                card.GetComponent<UiVisualState>().SetState(state);
                UiKit.Size(card, -1, UiMetrics.MinTouchPt);
            }
            kit.Text(surfaces, "Surface.Note", "Mavi kontur · üst ışık · yumuşak gölge", UiTokens.TypeCaption).color = UiTokens.TextSecondary;
            foreach (UiButtonVariant variant in Enum.GetValues(typeof(UiButtonVariant)))
            {
                var buttons = Section(kit, grid, "Buttons." + variant, "0" + ((int)variant + 2) + " / BUTTON · " + variant.ToString().ToUpperInvariant());
                string[] names = { "NORMAL", "BASILI", "PASİF", "SEÇİLİ", "HAZIR" };
                foreach (UiState state in Enum.GetValues(typeof(UiState)))
                {
                    var button = kit.Button(buttons, variant + "." + state, names[(int)state], variant, kit.Assets.iconUpgrade, 120);
                    button.GetComponent<UiVisualState>().SetState(state);
                }
            }
            var icons = Section(kit, grid, "Icons", "05 / ICON BUTTON · CHIP");
            foreach (UiState state in Enum.GetValues(typeof(UiState)))
            {
                var row = Row(icons, "Icons." + state);
                var button = kit.IconButton(row, "Pause." + state, kit.Assets.iconPause); button.GetComponent<UiVisualState>().SetState(state);
                kit.Chip(row, "Chip." + state, new[] { "NORMAL", "BASILI", "PASİF", "SEÇİLİ", "HAZIR" }[(int)state]);
            }
            var stats = Section(kit, grid, "Stats", "06 / STAT · SWITCH · SLIDER");
            var statrow = Row(stats, "Stat.Rows");
            kit.StatPill(statrow, "GoldStat", Painted("IconGold") ?? kit.Assets.iconSell, 1500).SetValue(1512, false);
            kit.StatPill(statrow, "Stat.Disabled", Painted("IconKeep") ?? kit.Assets.iconShield, 20).GetComponent<UiVisualState>().SetState(UiState.Disabled);
            var switches = Row(stats, "Switches"); kit.Switch(switches, "Switch.On", true); kit.Switch(switches, "Switch.Off", false);
            kit.Switch(switches, "Switch.Disabled", false).GetComponent<UiVisualState>().SetState(UiState.Disabled);
            kit.Slider(stats, "Volume", kit.Assets.iconSettings, .68f);
            kit.Slider(stats, "Volume.Disabled", kit.Assets.iconSettings, .35f).GetComponent<UiVisualState>().SetState(UiState.Disabled);
            kit.Text(stats, "Settings.Note", "Açık / kapalı  ·  simge + etiket", UiTokens.TypeCaption).color = UiTokens.TextSecondary;
            var segments = Section(kit, grid, "Segments", "07 / SEGMENTED · PROGRESS");
            kit.SegmentedControl(segments, "Speed", new LocalizedCopy[] { "1×", "2×", "3×" }, 1);
            kit.Text(segments, "Wave.Label", "DALGA  3 / 7", UiTokens.TypeBody, true);
            kit.ProgressSegments(segments, "Wave", 7).SetProgress(2.5f);
            kit.Text(segments, "Wave.Note", "Seçim · tamamlandı · devam ediyor", UiTokens.TypeCaption).color = UiTokens.TextSecondary;
            var cooldowns = Section(kit, grid, "Cooldowns", "08 / RADIAL COOLDOWN");
            var coolrow = Row(cooldowns, "Cooldown.Row");
            foreach (int remaining in new[] { 18, 9, 0 })
            {
                var cooldown = kit.RadialCooldown(coolrow, "Ability." + remaining, kit.Assets.iconShield);
                UiKit.Size((RectTransform)cooldown.transform, 112, 140); cooldown.SetCooldown(remaining, 24);
            }
            kit.Text(cooldowns, "Cooldown.Note", "Saniye · radyal dolgu · HAZIR", UiTokens.TypeCaption).color = UiTokens.TextSecondary;
            var radialCard = Section(kit, grid, "Radial", "09 / RADIAL MENU · K3");
            var radialRow = Row(radialCard, "Radial.Examples"); UiKit.Size(radialRow, -1, 230, 1);
            foreach (string mode in new[] { "Full", "Edge" })
            {
                var host = UiKit.Node(radialRow, "Radial.Host." + mode); UiKit.Size(host, -1, 230, 1);
                var menu = kit.RadialMenu(host, "BuildMenu." + mode, new[] {
                    new UiRadialOption("Archer", Painted("IconArcher") ?? kit.Assets.iconShield, 100),
                    new UiRadialOption("Barracks", Painted("IconBarracks") ?? kit.Assets.iconShield, 120),
                    new UiRadialOption("Mage", Painted("IconMage") ?? kit.Assets.iconShield, 160) });
                if (mode == "Full")
                {
                    var range = kit.Decoration(menu.transform, "Preview.Range", kit.Assets.cooldownRing, UiTokens.Hairline);
                    range.preserveAspect = true; range.transform.SetAsFirstSibling();
                }
            }
            var previewRow = Row(radialCard, "Radial.Preview");
            kit.Text(previewRow, "Radial.Note", "ÖNİZLEME · Menzil 4,7", UiTokens.TypeCompact).color = UiTokens.Gold;
            kit.Icon(previewRow, "Radial.Confirmed", kit.Assets.iconCheck, UiTokens.Space3);
            kit.Text(previewRow, "Radial.Confirmation", "TEKRAR DOKUN · ONAY", UiTokens.TypeCompact).color = UiTokens.TextSecondary;
            var modalCard = Section(kit, grid, "ModalSection", "10 / MODAL");
            var modalHost = UiKit.Node(modalCard, "Modal.Host"); UiKit.Size(modalHost, -1, 265, 1);
            var modal = kit.Modal(modalHost, "Modal", FrontendCopy.Paused);
            kit.Text(modal.Body, "Modal.Note", "Greenwatch seni bekliyor.", UiTokens.TypeCaption);
            kit.Button(modal.Body, "Modal.Resume", FrontendCopy.Resume, UiButtonVariant.Gold, kit.Assets.iconPlay);
            var sheetCard = Section(kit, grid, "SheetSection", "11 / BOTTOM SHEET");
            var sheetHost = UiKit.Node(sheetCard, "Sheet.Host"); UiKit.Size(sheetHost, -1, 265, 1);
            var sheet = kit.BottomSheet(sheetHost, "Sheet", FrontendCopy.SettingsTitle);
            kit.Switch(sheet.Body, "Sheet.Motion", true);
            var stars = Section(kit, grid, "Stars", "12 / STAR ROW · SYSTEM ICONS");
            var starrow = Row(stars, "Star.Rows"); kit.StarRow(starrow, "Stars.Two", 2); kit.StarRow(starrow, "Stars.Three", 3);
            var all = typeof(UiDesignSystemAssets).GetFields().Where(f => f.Name.StartsWith("icon")).Select(f => (Sprite)f.GetValue(kit.Assets)).ToArray();
            for (int i = 0; i < 2; i++)
            {
                var row = Row(stars, "Glyphs." + i);
                for (int j = 0; j < 7; j++) kit.Icon(row, all[i * 7 + j].name, all[i * 7 + j], UiTokens.Space4);
            }
            kit.Text(stars, "Stars.Note", "Sıralı dolum · azaltılmış hareket", UiTokens.TypeCaption).color = UiTokens.TextSecondary;
        }
    }
}
#endif
