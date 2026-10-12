#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AllinDefense.Presentation;
using AllinDefense.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.U2D;

namespace AllinDefense.Editor
{
    /// <summary>Deterministic CPU rasterization; no blur, grab-pass, or downloaded assets.</summary>
    public static class UiDesignSystemAssetBuilder
    {
        public const string Root = "Assets/Art/GeneratedPresentation";
        public const string SpriteRoot = Root + "/UiPrimitives";
        public static void ConnectFrontendTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<AllinDefense.Presentation.FrontendPresentationTheme>("Assets/Resources/Presentation/FrontendPresentationTheme.asset");
            theme.designSystem = AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>(SetPath);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
        }

        public const string SetPath = Root + "/UiDesignSystemAssets.asset";
        public const string AtlasPath = "Assets/Art/UI/AllinDefenseUI.spriteatlas";
        public const string IconRoot = Root + "/UiIcons";
        private const int Size = 128;
        private const string RegularPath = "Assets/Art/Fonts/NotoSans-Regular.ttf";
        private const string BoldPath = "Assets/Art/Fonts/NotoSans-Bold.ttf";

        [MenuItem("ALLIN: Defense/Build UI Design System Assets")]
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
            set.goldGradient = WriteSprite("GoldGradient", UiTokens.RadiusMedium + 2, (x, y) =>
            {
                float t = y / (Size - 1f);
                Color color = t < .55f ? Color.Lerp(UiTokens.GoldLow, UiTokens.Gold, t / .55f)
                    : Color.Lerp(UiTokens.Gold, UiTokens.GoldHigh, (t - .55f) / .45f);
                color.a = Coverage(RoundedDistance(x, y, 64, UiTokens.RadiusMedium));
                return color;
            });
            set.filledCircle = WriteSprite("FilledCircle", 0, (x, y) =>
                UiTokens.WithAlpha(Color.white, Coverage(Radius(x, y) - 62f)));
            set.cooldownRing = WriteSprite("CooldownRing", 0, (x, y) =>
                UiTokens.WithAlpha(Color.white, Coverage(Mathf.Abs(Radius(x, y) - 56f) - 6f)));

            BuildSystemIcons();
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

        [MenuItem("ALLIN: Defense/Build UI System Icons")]
        public static void BuildSystemIcons()
        {
            Directory.CreateDirectory(IconRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var set = AssetDatabase.LoadAssetAtPath<UiDesignSystemAssets>(SetPath);
            if (set == null) throw new InvalidOperationException("Build the design system primitives first.");
            set.iconEnemy = Glyph("Enemy", p => Mathf.Min(Polygon(p, new[] { new Vector2(.25f,.74f), new Vector2(.5f,.86f),
                new Vector2(.75f,.74f), new Vector2(.79f,.46f), new Vector2(.64f,.35f), new Vector2(.64f,.19f),
                new Vector2(.36f,.19f), new Vector2(.36f,.35f), new Vector2(.21f,.46f) }),
                Mathf.Min(Line(p,.32f,.56f,.42f,.53f), Line(p,.58f,.53f,.68f,.56f))));
            set.iconPause = Glyph("Pause", p => Mathf.Min(Line(p, .36f, .25f, .36f, .75f), Line(p, .64f, .25f, .64f, .75f)));
            set.iconPlay = Glyph("Play", p => Polygon(p, new[] { new Vector2(.32f,.22f), new Vector2(.78f,.5f), new Vector2(.32f,.78f) }));
            set.iconFastForward = Glyph("FastForward", p => Mathf.Min(Polygon(p, new[] { new Vector2(.16f,.25f), new Vector2(.5f,.5f), new Vector2(.16f,.75f) }),
                Polygon(p, new[] { new Vector2(.52f,.25f), new Vector2(.86f,.5f), new Vector2(.52f,.75f) })));
            set.iconClose = Glyph("Close", p => Mathf.Min(Line(p,.28f,.28f,.72f,.72f), Line(p,.28f,.72f,.72f,.28f)));
            set.iconBack = Glyph("Back", p => Mathf.Min(Line(p,.62f,.24f,.35f,.5f), Line(p,.35f,.5f,.62f,.76f)));
            set.iconCheck = Glyph("Check", p => Mathf.Min(Line(p,.22f,.5f,.43f,.29f), Line(p,.43f,.29f,.8f,.72f)));
            set.iconUpgrade = Glyph("Upgrade", p => Mathf.Min(Line(p,.5f,.2f,.5f,.8f), Mathf.Min(Line(p,.25f,.55f,.5f,.8f), Line(p,.5f,.8f,.75f,.55f))));
            var star = Enumerable.Range(0, 10).Select(i => new Vector2(.5f,.5f) +
                new Vector2(Mathf.Cos(Mathf.PI/2 + i*Mathf.PI/5), Mathf.Sin(Mathf.PI/2 + i*Mathf.PI/5)) * (i % 2 == 0 ? .36f : .16f)).ToArray();
            set.iconStarEmpty = Glyph("StarEmpty", p => Polygon(p, star));
            set.iconStarFilled = Glyph("StarFilled", p => Polygon(p, star, true));
            set.iconMap = Glyph("Map", p => Mathf.Min(Polygon(p, new[] { new Vector2(.17f,.23f),new Vector2(.39f,.32f),new Vector2(.61f,.23f),new Vector2(.83f,.32f),
                new Vector2(.83f,.77f),new Vector2(.61f,.68f),new Vector2(.39f,.77f),new Vector2(.17f,.68f) }),
                Mathf.Min(Line(p,.39f,.32f,.39f,.77f),Line(p,.61f,.23f,.61f,.68f))));
            set.iconShield = Glyph("Shield", p => Polygon(p, new[] { new Vector2(.5f,.16f),new Vector2(.23f,.39f),new Vector2(.23f,.73f),new Vector2(.5f,.84f),new Vector2(.77f,.73f),new Vector2(.77f,.39f) }));
            set.iconLock = Glyph("Lock", p => Mathf.Min(Polygon(p,new[] {new Vector2(.25f,.2f),new Vector2(.75f,.2f),new Vector2(.75f,.55f),new Vector2(.25f,.55f)}),
                p.y >= .55f ? Mathf.Abs((p-new Vector2(.5f,.58f)).magnitude-.19f) : Line(p,.5f,.33f,.5f,.43f)));
            set.iconSettings = Glyph("Settings", p =>
            {
                float angle = Mathf.Atan2(p.y-.5f,p.x-.5f);
                float radius = .29f + .035f * Mathf.Cos(angle * 8);
                return Mathf.Min(Mathf.Abs((p-new Vector2(.5f,.5f)).magnitude-radius), Mathf.Abs((p-new Vector2(.5f,.5f)).magnitude-.11f));
            });
            set.iconSell = Glyph("Sell", p => Mathf.Min(Mathf.Abs((p-new Vector2(.43f,.5f)).magnitude-.28f),
                Mathf.Min(Line(p,.42f,.5f,.86f,.5f), Mathf.Min(Line(p,.72f,.64f,.86f,.5f),Line(p,.86f,.5f,.72f,.36f)))));
            set.iconRapidFire = Glyph("RapidFire", p => Mathf.Min(Arrow(p, .28f), Mathf.Min(Arrow(p, .5f), Arrow(p, .72f))));
            set.iconSharpshooter = Glyph("Sharpshooter", p => Mathf.Min(Mathf.Abs((p - new Vector2(.5f,.5f)).magnitude - .23f),
                Mathf.Min(Line(p,.5f,.12f,.5f,.88f), Line(p,.12f,.5f,.88f,.5f))));
            set.iconAssaultGuard = Glyph("AssaultGuard", p => Mathf.Min(Line(p,.28f,.2f,.72f,.78f),
                Mathf.Min(Line(p,.27f,.48f,.54f,.28f), Polygon(p, new[] { new Vector2(.61f,.8f), new Vector2(.8f,.87f), new Vector2(.78f,.66f) }))));
            var blast = Enumerable.Range(0, 16).Select(i => new Vector2(.5f,.5f) +
                new Vector2(Mathf.Cos(i*Mathf.PI/8), Mathf.Sin(i*Mathf.PI/8)) * (i % 2 == 0 ? .38f : .16f)).ToArray();
            set.iconBlast = Glyph("Blast", p => Polygon(p, blast));
            set.iconFrost = Glyph("Frost", p =>
            {
                float distance = float.MaxValue;
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI / 3;
                    Vector2 tip = new Vector2(.5f,.5f) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .36f;
                    Vector2 joint = Vector2.Lerp(new Vector2(.5f,.5f), tip, .65f);
                    distance = Mathf.Min(distance, Line(p,.5f,.5f,tip.x,tip.y));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 branch = joint + new Vector2(Mathf.Cos(angle + side * Mathf.PI / 3), Mathf.Sin(angle + side * Mathf.PI / 3)) * .14f;
                        distance = Mathf.Min(distance, Line(p,joint.x,joint.y,branch.x,branch.y));
                    }
                }
                return distance;
            });
            Sprite[] sprites = { set.iconEnemy, set.iconPause, set.iconPlay, set.iconFastForward, set.iconSettings, set.iconClose,
                set.iconBack, set.iconMap, set.iconLock, set.iconStarFilled, set.iconStarEmpty, set.iconCheck, set.iconShield, set.iconUpgrade, set.iconSell, set.iconRapidFire, set.iconSharpshooter, set.iconAssaultGuard, set.iconBlast, set.iconFrost };
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            var existing = SpriteAtlasExtensions.GetPackables(atlas);
            var textures = sprites.Select(sprite => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(sprite)));
            SpriteAtlasExtensions.Add(atlas, textures.Where(texture => !existing.Contains(texture)).ToArray());
            EditorUtility.SetDirty(set); EditorUtility.SetDirty(atlas); AssetDatabase.SaveAssets();
        }

        private static Sprite Glyph(string name, Func<Vector2, float> distance) => WriteSprite("System" + name, 0,
            (x,y) => UiTokens.WithAlpha(UiTokens.White, Coverage(distance(new Vector2((x+.5f)/Size,(y+.5f)/Size)) * Size - 3f)), IconRoot);

        private static float Arrow(Vector2 p, float x) => Mathf.Min(Line(p,x,.2f,x,.8f),
            Mathf.Min(Line(p,x-.08f,.65f,x,.8f), Line(p,x,.8f,x+.08f,.65f)));

        private static float Line(Vector2 p, float x1, float y1, float x2, float y2)
        {
            Vector2 a = new Vector2(x1,y1), b = new Vector2(x2,y2), d = b-a;
            return (p-a-d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)).magnitude;
        }

        private static float Polygon(Vector2 p, Vector2[] vertices, bool filled = false)
        {
            float distance = float.MaxValue; bool inside = false;
            for (int i=0,j=vertices.Length-1; i<vertices.Length; j=i++)
            {
                Vector2 a=vertices[i], b=vertices[j];
                distance = Mathf.Min(distance, Line(p,a.x,a.y,b.x,b.y));
                if ((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            return filled && inside ? -distance : distance;
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

        private static Sprite WriteSprite(string name, float border, Func<float, float, Color> pixel, string directory = SpriteRoot)
        {
            string path = directory + "/" + name + ".png";
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
