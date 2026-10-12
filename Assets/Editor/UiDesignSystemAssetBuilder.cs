#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;

namespace DefenderChronicles.Editor
{
    /// <summary>Deterministic CPU rasterization; no blur, grab-pass, or downloaded assets.</summary>
    public static class UiDesignSystemAssetBuilder
    {
        public const string Root = "Assets/Art/GeneratedPresentation";
        public const string SpriteRoot = Root + "/UiPrimitives";
        public const string SetPath = Root + "/UiDesignSystemAssets.asset";
        public const string AtlasPath = "Assets/Art/UI/DefenderChroniclesUI.spriteatlas";
        private const int Size = 128;
        private const string RegularPath = "Assets/Art/Fonts/NotoSans-Regular.ttf";
        private const string BoldPath = "Assets/Art/Fonts/NotoSans-Bold.ttf";

        [MenuItem("Defender Chronicles/Build UI Design System Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(SpriteRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var set = AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UiDesignSystemAssets>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.regularFont = BuildFont(RegularPath, "NotoSans-DC");
            string bodyCharacters = string.Concat(set.regularFont.characterTable
                .Where(character => character.unicode <= char.MaxValue).Select(character => (char)character.unicode));
            set.boldFont = BuildFont(BoldPath, "NotoSans-Bold-DC", bodyCharacters);
            // Real 600 weight is not supplied locally yet. Keep an independent role using real 700 glyphs.
            set.semiBoldFont = set.boldFont;
            set.boldFont.fallbackFontAssetTable = set.regularFont.fallbackFontAssetTable.ToList();
            EditorUtility.SetDirty(set.boldFont);
            TMP_FontAsset regular = set.regularFont;
            regular.fontWeightTable[UiTokens.WeightSemiBold / 100].regularTypeface = set.semiBoldFont;
            regular.fontWeightTable[UiTokens.WeightBold / 100].regularTypeface = set.boldFont;
            EditorUtility.SetDirty(regular);

            set.roundedSmall = Rounded("Rounded10", UiTokens.RadiusSmall);
            set.roundedMedium = Rounded("Rounded14", UiTokens.RadiusMedium);
            set.roundedCard = Rounded("Rounded20", UiTokens.RadiusCard);
            set.pill = Rounded("Pill64", UiTokens.RadiusPill);
            set.softShadow = WriteSprite("SoftShadow", 46, (x, y) =>
            {
                float distance = RoundedDistance(x, y, 40, UiTokens.RadiusCard);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(0f, distance) / UiTokens.ShadowSoftness), 2f);
                return UiTokens.WithAlpha(Color.white, alpha);
            });
            // Stretch vertically, with no vertical 9-slice borders, to preserve the gradient.
            set.verticalGradient = WriteSprite("VerticalGradient", 0,
                (x, y) => UiTokens.WithAlpha(Color.white, .07f * Mathf.Clamp01((y / (Size - 1f) - .55f) / .45f)));
            set.goldGradient = WriteSprite("GoldGradient", 0, (x, y) =>
            {
                float t = y / (Size - 1f);
                return t < .55f ? Color.Lerp(UiTokens.GoldLow, UiTokens.Gold, t / .55f)
                    : Color.Lerp(UiTokens.Gold, UiTokens.GoldHigh, (t - .55f) / .45f);
            });
            set.filledCircle = WriteSprite("FilledCircle", 0, (x, y) =>
                UiTokens.WithAlpha(Color.white, Coverage(Radius(x, y) - 62f)));
            set.cooldownRing = WriteSprite("CooldownRing", 0, (x, y) =>
                UiTokens.WithAlpha(Color.white, Coverage(Mathf.Abs(Radius(x, y) - 56f) - 6f)));

            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if (atlas == null) throw new InvalidOperationException("Existing UI atlas is required: " + AtlasPath);
            UnityEngine.Object[] textures = new[] { set.roundedSmall, set.roundedMedium, set.roundedCard,
                set.pill, set.softShadow, set.verticalGradient, set.goldGradient, set.filledCircle, set.cooldownRing }
                .Select(sprite => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sprite))).ToArray();
            var existing = SpriteAtlasExtensions.GetPackables(atlas);
            SpriteAtlasExtensions.Add(atlas, textures.Where(texture => !existing.Contains(texture)).ToArray());
            // Rotated/tight packing is incompatible with the rectangular 9-slice primitives.
            var packing = SpriteAtlasExtensions.GetPackingSettings(atlas);
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            SpriteAtlasExtensions.SetPackingSettings(atlas, packing);
            EditorUtility.SetDirty(atlas);
            EditorUtility.SetDirty(set);
            var theme = AssetDatabase.LoadAssetAtPath<FrontendPresentationTheme>(
                "Assets/Resources/Presentation/FrontendPresentationTheme.asset");
            if (theme != null) { theme.ApplyDesignTokens(); EditorUtility.SetDirty(theme); }
            AssetDatabase.SaveAssets();
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            Debug.Log("[DC_UI_DESIGN_SYSTEM] Noto Sans TR atlas, real Bold/600 role and 9 UI primitives generated.");
        }

        private static TMP_FontAsset BuildFont(string sourcePath, string name, string additionalCharacters = "")
        {
            string path = Root + "/" + name + ".asset";
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null) throw new InvalidOperationException("Local OFL font is missing: " + sourcePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null)
            {
                font = TMP_FontAsset.CreateFontAsset(source, 72, 9, GlyphRenderMode.SDFAA,
                    1024, 1024, AtlasPopulationMode.Dynamic, true);
                font.name = name;
                AssetDatabase.CreateAsset(font, path);
            }
            // Prewarm both ASCII and Turkish in the serialized atlas, not just at first runtime use.
            string chars = string.Concat(Enumerable.Range(32, 95).Select(value => (char)value)) + UiTokens.TurkishCharacters + additionalCharacters;
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.isMultiAtlasTexturesEnabled = true;
            string pending = new string(chars.Where(character =>
                !font.characterTable.Any(entry => entry.unicode == character)).ToArray());
            // TMP returns false when every character already exists; validate the resulting table instead.
            if (pending.Length > 0) font.TryAddCharacters(pending, out _);
            string missing = new string(chars.Where(character =>
                !font.characterTable.Any(entry => entry.unicode == character)).ToArray());
            if (missing.Length > 0)
                throw new InvalidOperationException(name + " is missing required glyphs: " + missing);
            if (sourcePath == BoldPath)
            {
                font.boldStyle = 0f;
                if (font.material != null) font.material.SetFloat(ShaderUtilities.ID_WeightBold, 0f);
            }
            if (font.material != null && !AssetDatabase.Contains(font.material))
                AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures.Where(texture => texture != null))
            {
                if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, font);
                EditorUtility.SetDirty(texture);
            }
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            // Preserve the project's static-font source-reference contract for editor validation/rebuilds.
            serialized.FindProperty("m_SourceFontFile").objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (font.material != null) EditorUtility.SetDirty(font.material);
            EditorUtility.SetDirty(font);
            return font;
        }

        private static Sprite Rounded(string name, float radius) =>
            WriteSprite(name, Mathf.Min(radius + 2, Size / 2 - 1),
                (x, y) => UiTokens.WithAlpha(Color.white, Coverage(RoundedDistance(x, y, 63, radius))));

        private static float RoundedDistance(float x, float y, float halfSize, float radius)
        {
            var q = new Vector2(Mathf.Abs(x + .5f - Size / 2f), Mathf.Abs(y + .5f - Size / 2f))
                - Vector2.one * (halfSize - radius);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude
                + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
        }

        private static float Radius(float x, float y) =>
            new Vector2(x + .5f - Size / 2f, y + .5f - Size / 2f).magnitude;
        private static float Coverage(float distance) => Mathf.Clamp01(.5f - distance);

        private static Sprite WriteSprite(string name, float border, Func<float, float, Color> pixel)
        {
            string path = SpriteRoot + "/" + name + ".png";
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            try
            {
                var colors = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++) colors[y * Size + x] = pixel(x, y);
                texture.SetPixels(colors);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = Vector4.one * border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
