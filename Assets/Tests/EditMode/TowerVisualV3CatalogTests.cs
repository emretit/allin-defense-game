#if UNITY_EDITOR
using System.Linq;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class TowerVisualV3CatalogTests
    {
        private static DefenderPresentationCatalog Catalog()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            return catalog;
        }

        [Test]
        public void EverySpecializationResolvesItsOwnGeneratedModel()
        {
            var catalog = Catalog();
            var branches = new[]
            {
                OutpostSpecialization.RapidFire, OutpostSpecialization.Sharpshooter, OutpostSpecialization.ShieldGuard,
                OutpostSpecialization.AssaultGuard, OutpostSpecialization.Blast, OutpostSpecialization.Frost
            };
            var paths = branches.Select(branch =>
            {
                OutpostKind kind = new[] { OutpostKind.Archer, OutpostKind.Barracks, OutpostKind.Mage }
                    .Single(candidate => TowerProgression.Matches(candidate, branch));
                GameObject prefab = catalog.OutpostPrefab(kind, 2, branch);
                Assert.That(prefab, Is.Not.SameAs(catalog.OutpostPrefab(kind, 2)), $"{branch} must not reuse the level-2 model");
                Assert.That(catalog.OutpostScale(kind, 2, branch), Is.InRange(0.45f, 1.25f));
                return AssetDatabase.GetAssetPath(prefab);
            }).ToArray();
            Assert.That(paths.Distinct().Count(), Is.EqualTo(6));
            Assert.That(paths.All(path => path.StartsWith("Assets/Art/Models/Generated/")), Is.True);
            Assert.That(catalog.OutpostPrefab(OutpostKind.Archer, 2, OutpostSpecialization.None),
                Is.SameAs(catalog.OutpostPrefab(OutpostKind.Archer, 2)));
        }

        [Test]
        public void TowersAreOneMeshOnTheSharedTowerPalette()
        {
            var catalog = Catalog();
            Material shared = catalog.towerPaletteMaterial.material;
            Assert.That(shared, Is.Not.Null);
            Assert.That(shared.IsKeywordEnabled("_EMISSION"), Is.True, "Crystals and lanterns glow through the emission mask");
            Assert.That(shared.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive,
                Is.Not.EqualTo(MaterialGlobalIlluminationFlags.None), "Unity drops _EMISSION on first property read when GI flags are None");
            var prefabs = catalog.outpostVisuals.Select(entry => entry.prefab)
                .Concat(catalog.outpostSpecializationVisuals.Select(entry => entry.prefab)).ToArray();
            Assert.That(prefabs, Has.Length.EqualTo(12));
            foreach (GameObject prefab in prefabs)
            {
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Has.Length.EqualTo(1), $"{prefab.name} must be a single mesh");
                Material source = renderers[0].sharedMaterials.Single();
                Assert.That(catalog.ResolveLicensedAtlasMaterial(source), Is.SameAs(shared), prefab.name);
                int triangles = prefab.GetComponentInChildren<MeshFilter>().sharedMesh.triangles.Length / 3;
                Assert.That(triangles, Is.LessThanOrEqualTo(6000), $"{prefab.name} mobile triangle budget");
            }
        }
    }
}
#endif
