using System;
using System.Linq;
using System.Reflection;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AllinDefense.Tests
{
    public sealed class UiInfrastructureTests
    {
        [TestCase(375, 667)]
        [TestCase(402, 874)]
        [TestCase(874, 402)]
        [TestCase(667, 375)]
        public void MinimumTargetsAre44LogicalPointsAtEveryMatch(int width, int height)
        {
            var logical = new Vector2(width, height);
            var reference = width > height ? new Vector2(1920, 1080) : new Vector2(1080, 1920);
            foreach (float match in new[] { 0f, .5f, 1f })
            foreach (float density in new[] { 1f, 2f, 3f })
            {
                var points = UiMetrics.PointsPerCanvasUnit(logical * density, logical, reference, match);
                Vector2 hit = UiMetrics.CanvasUnitsForPoints(UiMetrics.MinTouchPt, points);
                Assert.That(hit.x * points.x, Is.EqualTo(44f).Within(.0001f));
                Assert.That(hit.y * points.y, Is.EqualTo(44f).Within(.0001f));
            }
        }

        [Test]
        public void NeighbourTargetsWithLogicalSpacingDoNotIntersect()
        {
            var logical = new Vector2(402, 874);
            var points = UiMetrics.PointsPerCanvasUnit(logical * 3, logical, new Vector2(1080, 1920), 0);
            var size = UiMetrics.CanvasUnitsForPoints(UiMetrics.MinTouchPt, points);
            var first = new Rect(-size / 2, size);
            var second = new Rect(new Vector2(60 / points.x, 0) - size / 2, size);
            Assert.That(first.Overlaps(second), Is.False);
            Assert.That((second.xMin - first.xMax) * points.x, Is.EqualTo(16).Within(.001));
        }

        [Test]
        public void ScalerMatchUsesLogarithmicInterpolation()
        {
            Assert.That(UiMetrics.CanvasScale(new Vector2(800, 600), new Vector2(400, 600), .5f),
                Is.EqualTo(Mathf.Sqrt(2)).Within(.0001));
        }

        [Test]
        public void TenSeededRandomCopyIdsHaveBothLanguages()
        {
            var fields = typeof(FrontendCopy).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(LocalizedCopy)).ToArray();
            var random = new System.Random(20261012);
            foreach (var field in fields.OrderBy(_ => random.Next()).Take(10))
            {
                var copy = (LocalizedCopy)field.GetValue(null);
                Assert.That(copy.Turkish, Is.Not.Empty, field.Name);
                Assert.That(copy.English, Is.Not.Empty, field.Name);
            }
        }

        [Test]
        public void UiIdSurvivesObjectRename()
        {
            var root = new GameObject("Original", typeof(RectTransform));
            try
            {
                UiId.Assign(root.transform, "Stable");
                root.name = "Renamed";
                Assert.That(root.GetComponent<UiId>().Id, Is.EqualTo("Stable"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
