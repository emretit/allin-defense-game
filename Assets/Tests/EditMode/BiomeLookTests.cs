using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class BiomeLookTests
    {
        [Test]
        public void RegionProfilesHaveDistinctGroundAndSilhouetteFamilies()
        {
            BiomeLook[] profiles = Enumerable.Range(0, 5).Select(BiomeLook.ForRegion).ToArray();
            Assert.That(profiles.Select(p => p.GroundColor).Distinct().Count(), Is.EqualTo(5));
            Assert.That(profiles.Select(p => p.Silhouette).Distinct().Count(), Is.EqualTo(5));
            Assert.That(profiles.Select(p => p.DecorativeAccentColor).Distinct().Count(), Is.EqualTo(5));
            foreach (BiomeLook profile in profiles)
            {
                BiomeDefinition palette = CampaignLevelFactory.Biome(profile.Index);
                Assert.That(profile.AccentColor, Is.EqualTo(palette.accentColor));
                Assert.That(profile.KeyLightColor, Is.EqualTo(palette.keyLightColor));
                Assert.That(profile.FogColor, Is.EqualTo(palette.fogColor));
                Assert.That(profile.SceneryPalette().Length, Is.EqualTo(8));
            }
        }

        [Test]
        public void GreenwatchRetainsItsOriginalColorsAndLighting()
        {
            BiomeLook profile = BiomeLook.ForRegion(0);
            Assert.That(profile.GroundColor, Is.EqualTo(new Color(.21f, .25f, .25f)));
            Assert.That(profile.MossColor, Is.EqualTo(new Color(.46f, .91f, .65f)));
            Assert.That(profile.GroundTextureCenter, Is.EqualTo(new Color(.79f, .87f, .86f)));
            Assert.That(profile.GroundTextureEdge, Is.EqualTo(new Color(.29f, .39f, .48f)));
            Assert.That(profile.LightBlend, Is.EqualTo(.08f));
            Assert.That(profile.EquatorBlend, Is.EqualTo(.12f));
            Assert.That(profile.FogBlend, Is.EqualTo(.18f));
            CollectionAssert.AreEqual(new[] { new Color(.26f, .43f, .36f), new Color(.36f, .48f, .34f),
                new Color(.40f, .56f, .50f), new Color(.32f, .29f, .24f), new Color(.44f, .54f, .57f),
                new Color(.31f, .46f, .36f), new Color(.55f, .66f, .36f), Color.white }, profile.SceneryPalette());
        }

        [Test]
        public void FrostTerrainStaysBelowRoadBrightness()
        {
            BiomeLook frost = BiomeLook.ForRegion(2);
            Color road = new(.43f, .40f, .33f);
            Assert.That(frost.GroundColor.grayscale * frost.GroundTextureCenter.grayscale,
                Is.LessThan(road.grayscale * .8f));
            Assert.That(frost.GroundColor.grayscale * frost.MossColor.grayscale,
                Is.LessThan(road.grayscale * .8f));
            Assert.That(frost.LightBlend, Is.LessThanOrEqualTo(BiomeLook.ForRegion(0).LightBlend));
            Assert.That(frost.FogBlend, Is.LessThanOrEqualTo(BiomeLook.ForRegion(0).FogBlend));
        }

        [Test]
        public void CitadelStoneFacesPointOutward()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            Type dressing = typeof(DefenderBattlefieldDressing);
            Type batchType = dressing.GetNestedType("SceneryBatch", BindingFlags.NonPublic);
            object batch = Activator.CreateInstance(batchType, true);
            dressing.GetMethod("AddCitadelStone", flags).Invoke(null,
                new[] { batch, (object)Vector3.zero, .4f, new Vector3(.86f, 1.06f, .40f) });
            var vertices = (List<Vector3>)batchType.GetField("vertices").GetValue(batch);
            var indices = (List<int>)batchType.GetField("indices").GetValue(batch);
            for (int i = 0; i < indices.Count; i += 3)
            {
                Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
                Assert.That(Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f),
                    Is.GreaterThan(0), "Stone top and sides must receive light on their outside faces");
            }
        }

        [TestCase(-1, 0)]
        [TestCase(5, 4)]
        public void RegionIndexClampsLikeTheCampaignPalette(int index, int expected)
        {
            Assert.That(BiomeLook.ForRegion(index), Is.SameAs(BiomeLook.ForRegion(expected)));
        }
    }
}
