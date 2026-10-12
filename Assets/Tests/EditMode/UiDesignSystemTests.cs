using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace DefenderChronicles.Tests
{
    public sealed class UiDesignSystemTests
    {
        private const string Root = "Assets/Art/GeneratedPresentation";
        private static UiDesignSystemAssets Set =>
            AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>(Root + "/UiDesignSystemAssets.asset");

        [TestCase("NotoSans-DC")]
        [TestCase("NotoSans-Bold-DC")]
        public void TurkishGlyphsAreAlreadyInTheSerializedAtlas(string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/" + name + ".asset");
            Assert.That(font, Is.Not.Null);
            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
            foreach (char character in UiTokens.TurkishCharacters)
            {
                // Inspect actual tables; do not dynamically add characters or consult fallbacks.
                var entry = font.characterTable.SingleOrDefault(value => value.unicode == character);
                Assert.That(entry, Is.Not.Null, name + ": " + character);
                var glyph = font.glyphTable.Single(value => value.index == entry.glyphIndex);
                Assert.That(glyph.glyphRect.width, Is.GreaterThan(0));
                Assert.That(glyph.glyphRect.height, Is.GreaterThan(0));
                Assert.That(glyph.atlasIndex, Is.LessThan(font.atlasTextures.Length));
                Assert.That(font.atlasTextures[glyph.atlasIndex], Is.Not.Null);
            }
        }

        [Test]
        public void ExistingFrontendPunctuationAndAccentedLettersRemainPrewarmed()
        {
            const string retainedCharacters = "ÂÉÎ×Ûâéîû–—’•‹›";
            foreach (TMP_FontAsset font in new[] { Set.regularFont, Set.boldFont })
                foreach (char value in retainedCharacters)
                    Assert.That(font.characterTable.Any(character => character.unicode == value), Is.True,
                        font.name + ": " + value);
        }

        [Test]
        public void SemiBoldAndBoldRolesResolveToRealBoldTypeface()
        {
            Assert.That(Set, Is.Not.Null);
            Assert.That(Set.boldFont.faceInfo.familyName, Is.EqualTo("Noto Sans"));
            Assert.That(Set.boldFont.faceInfo.styleName, Is.EqualTo("Bold"));
            Assert.That(Set.regularFont.fontWeightTable[6].regularTypeface, Is.SameAs(Set.semiBoldFont));
            Assert.That(Set.regularFont.fontWeightTable[7].regularTypeface, Is.SameAs(Set.boldFont));
            Assert.That(Set.boldFont.boldStyle, Is.Zero, "The real Bold face must not receive synthetic weight.");
            foreach (FontWeight weight in new[] { FontWeight.SemiBold, FontWeight.Bold })
                foreach (char value in UiTokens.TurkishCharacters)
                {
                    var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(value,
                        Set.regularFont, false, FontStyles.Normal, weight, out bool alternative);
                    Assert.That(alternative, Is.True);
                    Assert.That(character.textAsset, Is.SameAs(Set.boldFont));
                }
        }

        [Test]
        public void EssentialTextMeetsContrastOnAllTokenSurfaces()
        {
            Color[] surfaces = { UiTokens.Night, UiTokens.NightDeep, UiTokens.Surface,
                UiTokens.SurfaceRaised, UiTokens.ButtonBlue, UiTokens.HudPanel, UiTokens.HudButtonReady, UiTokens.HudButtonPressed,
                UiTokens.ProfileSurface, UiTokens.TacticalSurface, UiTokens.ResultCard, UiTokens.PauseCard };
            foreach (Color surface in surfaces)
            {
                Color opaque = Over(surface, UiTokens.Night);
                foreach (Color text in new[] { UiTokens.TextPrimary, UiTokens.TextSecondary })
                    Assert.That(Contrast(text, opaque), Is.GreaterThanOrEqualTo(4.5f), $"{text} on {surface}");
            }
            // Glass is supported over a dark backing. Test its brightest modeled highlight as well.
            foreach (Color glass in new[] { UiTokens.Glass, UiTokens.GlassStrong })
            {
                Color surface = Over(UiTokens.GlassHighlight, Over(glass, UiTokens.Night));
                Assert.That(Contrast(UiTokens.TextPrimary, surface), Is.GreaterThanOrEqualTo(4.5f));
                Assert.That(Contrast(UiTokens.TextSecondary, surface), Is.GreaterThanOrEqualTo(4.5f));
            }
            foreach (Color gold in new[] { UiTokens.GoldLow, UiTokens.Gold, UiTokens.GoldHigh })
                Assert.That(Contrast(UiTokens.GoldText, gold), Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void TokensMatchApprovedPaletteGridRadiiAndMotion()
        {
            Assert.That((Color32)UiTokens.Surface, Is.EqualTo(new Color32(9, 18, 34, 255)));
            Assert.That((Color32)UiTokens.Gold, Is.EqualTo(new Color32(242, 184, 75, 255)));
            Assert.That(UiTokens.Glass.a, Is.EqualTo(.68f).Within(.0001f));
            Assert.That(UiTokens.GlassStrong.a, Is.EqualTo(.9f).Within(.0001f));
            foreach (float space in new[] { UiTokens.Space1, UiTokens.Space2, UiTokens.Space3,
                UiTokens.Space4, UiTokens.Space6, UiTokens.Space8 }) Assert.That(space % 8f, Is.Zero);
            Assert.That(new[] { UiTokens.RadiusSmall, UiTokens.RadiusMedium, UiTokens.RadiusCard },
                Is.EqualTo(new[] { 10f, 14f, 20f }));
            Assert.That(UiTokens.ButtonPressDuration, Is.LessThan(UiTokens.FadeDuration));
            Assert.That(UiTokens.ReducedMotionDuration, Is.LessThan(UiTokens.FadeDuration));
            Assert.That(UiTokens.EvaluateEaseOut(0), Is.Zero);
            Assert.That(UiTokens.EvaluateEaseOut(1), Is.EqualTo(1f));
            Assert.That(UiTokens.EaseOut.Evaluate(.5f), Is.EqualTo(UiTokens.EvaluateEaseOut(.5f)).Within(.0001f));
            var curve = UiTokens.EaseOut;
            curve.AddKey(.5f, 0);
            Assert.That(UiTokens.EaseOut.length, Is.EqualTo(2), "A caller cannot mutate the shared curve.");
        }

        [Test]
        public void ThemeAndHudShareTokenPalette()
        {
            var theme = AssetDatabase.LoadAssetAtPath<FrontendPresentationTheme>(
                "Assets/Resources/Presentation/FrontendPresentationTheme.asset");
            Assert.That(theme.panel, Is.EqualTo(UiTokens.GlassStrong));
            Assert.That(theme.commandGold, Is.EqualTo(UiTokens.Gold));
            Assert.That(theme.textPrimary, Is.EqualTo(UiTokens.TextPrimary));
            Assert.That(theme.textMuted, Is.EqualTo(UiTokens.TextSecondary));
            foreach (var pair in new[] { ("TextPrimary", UiTokens.TextPrimary), ("TextMuted", UiTokens.TextSecondary),
                ("GoldAccent", UiTokens.Gold), ("KeepAccent", UiTokens.Danger) })
            {
                var field = typeof(DefenderHudController).GetField(pair.Item1, BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That((Color)field.GetValue(null), Is.EqualTo(pair.Item2));
            }
        }

        [Test]
        public void MenuButtonLabelsMeetContrastOnGoldAndBlueSurfaces()
        {
            var root = new GameObject("Button Contrast Test", typeof(RectTransform));
            var flow = root.AddComponent<AppFlowController>();
            try
            {
                typeof(AppFlowController).GetField("theme", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(flow, AssetDatabase.LoadAssetAtPath<FrontendPresentationTheme>(
                        "Assets/Resources/Presentation/FrontendPresentationTheme.asset"));
                typeof(AppFlowController).GetField("catalog", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(flow, AssetDatabase.LoadAssetAtPath<DefenderPresentationCatalog>(
                        "Assets/Resources/Presentation/DefenderPresentationCatalog.asset"));
                var method = typeof(AppFlowController).GetMethod("CreateButton", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (Color surface in new[] { UiTokens.Gold, UiTokens.ButtonBlue })
                {
                    var button = (UnityEngine.UI.Button)method.Invoke(flow, new object[]
                    {
                        "Contrast Button", root.transform, FrontendCopy.Cancel, new Vector2(350, 132), surface, (Action)(() => { })
                    });
                    var label = button.GetComponentInChildren<TextMeshProUGUI>();
                    Assert.That(Contrast(label.color, surface), Is.GreaterThanOrEqualTo(4.5f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void AllNinePrimitivesAreInExistingAtlasWithCorrectBorders()
        {
            var set = Set;
            Assert.That(set, Is.Not.Null);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Art/UI/DefenderChroniclesUI.spriteatlas");
            Assert.That(atlas, Is.Not.Null);
            var packables = SpriteAtlasExtensions.GetPackables(atlas);
            var sprites = new[] { (set.roundedSmall, 12f), (set.roundedMedium, 16f), (set.roundedCard, 22f),
                (set.pill, 63f), (set.softShadow, 46f), (set.verticalGradient, 0f), (set.goldGradient, 0f),
                (set.filledCircle, 0f), (set.cooldownRing, 0f) };
            foreach (var pair in sprites)
            {
                Sprite sprite = pair.Item1;
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.border, Is.EqualTo(Vector4.one * pair.Item2), sprite.name);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(128, 128)));
                Assert.That(packables, Does.Contain(AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sprite))), sprite.name);
                Assert.That(atlas.CanBindTo(sprite), Is.True, sprite.name);
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
                Assert.That(importer.mipmapEnabled, Is.False);
            }
            var packing = SpriteAtlasExtensions.GetPackingSettings(atlas);
            Assert.That(packing.enableRotation, Is.False);
            Assert.That(packing.enableTightPacking, Is.False);
            foreach (string name in new[] { "DefenderHudPanel", "MedievalButton", "MedievalPanel", "IconAbility",
                "IconArcher", "IconBarracks", "IconGold", "IconKeep", "IconMage", "IconWave" })
                Assert.That(packables.Select(value => value.name), Does.Contain(name));
            // Imported atlas caches can still contain the old ten sprites until the target is packed.
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            Assert.That(atlas.spriteCount, Is.GreaterThanOrEqualTo(19), "All original and new sprites pack successfully.");
        }

        [Test]
        public void PrimitivePixelsHaveRoundedCornersSoftFalloffAndHollowRing()
        {
            WithPixels(Set.roundedCard, image =>
            {
                Assert.That(image.GetPixel(0, 0).a, Is.Zero);
                Assert.That(image.GetPixel(64, 64).a, Is.EqualTo(1));
                Assert.That(image.GetPixel(1, 64).a, Is.GreaterThan(0));
            });
            WithPixels(Set.softShadow, image =>
            {
                Assert.That(image.GetPixel(64, 64).a, Is.EqualTo(1));
                Assert.That(image.GetPixel(112, 64).a, Is.InRange(.1f, .8f));
                Assert.That(image.GetPixel(127, 64).a, Is.LessThan(.01f));
            });
            WithPixels(Set.filledCircle, image =>
            {
                Assert.That(image.GetPixel(64, 64).a, Is.EqualTo(1));
                Assert.That(image.GetPixel(0, 0).a, Is.Zero);
            });
            WithPixels(Set.cooldownRing, image =>
            {
                Assert.That(image.GetPixel(64, 64).a, Is.Zero);
                Assert.That(image.GetPixel(120, 64).a, Is.EqualTo(1));
                Assert.That(image.GetPixel(0, 0).a, Is.Zero);
            });
            WithPixels(Set.goldGradient, image =>
            {
                Assert.That(image.GetPixel(64, 127), Is.EqualTo(UiTokens.GoldHigh).Using(ColorComparer));
                Assert.That(image.GetPixel(64, 0), Is.EqualTo(UiTokens.GoldLow).Using(ColorComparer));
            });
        }

        [Test]
        public void RebuildingPrimitivesPreservesPngBytesAndGuids()
        {
            var paths = Directory.GetFiles(Root + "/UiPrimitives", "*.png").OrderBy(path => path).ToArray();
            Assert.That(paths.Length, Is.EqualTo(9));
            var bytes = paths.Select(File.ReadAllBytes).ToArray();
            var guids = paths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            Type builder = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(
                "DefenderChronicles.Editor.UiDesignSystemAssetBuilder")).First(type => type != null);
            builder.GetMethod("Build", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            for (int i = 0; i < paths.Length; i++)
            {
                Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(bytes[i]));
                Assert.That(AssetDatabase.AssetPathToGUID(paths[i]), Is.EqualTo(guids[i]));
            }
        }

        private static readonly System.Collections.Generic.IEqualityComparer<Color> ColorComparer =
            new UnityEngine.TestTools.Utils.ColorEqualityComparer(.005f);
        private static void WithPixels(Sprite sprite, Action<Texture2D> check)
        {
            var image = new Texture2D(2, 2);
            try { image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite))); check(image); }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
        private static Color Over(Color foreground, Color background) =>
            UiTokens.WithAlpha(foreground * foreground.a + background * (1 - foreground.a), 1);
        private static float Contrast(Color a, Color b) =>
            (Mathf.Max(Luminance(a), Luminance(b)) + .05f) / (Mathf.Min(Luminance(a), Luminance(b)) + .05f);
        private static float Luminance(Color c) => .2126f * Linear(c.r) + .7152f * Linear(c.g) + .0722f * Linear(c.b);
        private static float Linear(float value) => value <= .04045f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
    }
}
