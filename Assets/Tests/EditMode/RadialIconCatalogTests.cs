using System.IO;
using System.Linq;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace AllinDefense.Tests
{
    public sealed class RadialIconCatalogTests
    {
        [Test] public void PaintedBattleIconsAreCataloguedPackedAndImportedAt256()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DefenderPresentationCatalog>("Assets/Resources/Presentation/DefenderPresentationCatalog.asset");
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/Art/UI/AllinDefenseUI.spriteatlas");
            var packed = SpriteAtlasExtensions.GetPackables(atlas);
            foreach (string name in new[] { "Upgrade", "Sell", "RapidFire", "Sharpshooter", "ShieldGuard", "AssaultGuard", "Blast", "Frost" })
            {
                string path = $"Assets/Art/UI/Radial/Icon{name}.png";
                var sprite = (Sprite)typeof(DefenderPresentationCatalog).GetField($"radial{name}Icon").GetValue(catalog);
                Assert.That(sprite, Is.Not.Null, name);
                Assert.That(AssetDatabase.GetAssetPath(sprite), Is.EqualTo(path));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.maxTextureSize, Is.EqualTo(256));
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(packed, Does.Contain(AssetDatabase.LoadAssetAtPath<Texture2D>(path)));
                var texture = new Texture2D(2, 2);
                try
                {
                    texture.LoadImage(File.ReadAllBytes(path));
                    Assert.That(texture.width, Is.EqualTo(512)); Assert.That(texture.height, Is.EqualTo(512));
                    Assert.That(texture.GetPixels32().Any(pixel => pixel.a == 0), Is.True);
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }
    }
}
