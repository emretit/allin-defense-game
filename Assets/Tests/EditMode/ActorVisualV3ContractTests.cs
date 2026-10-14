using System;
using System.Collections.Generic;
using System.Linq;
using AllinDefense.Data;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AllinDefense.Tests
{
    public sealed class ActorVisualV3ContractTests
    {
        private static readonly string[] BossProfileIds =
            { "region-lord", "gate-master", "frost-sovereign", "deep-lord", "last-lord" };

        private static DefenderPresentationCatalog Catalog() =>
            Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");

        private static IEnumerable<PresentationActorKind> Kinds() =>
            Enum.GetValues(typeof(PresentationActorKind)).Cast<PresentationActorKind>();

        [Test]
        public void ActorsShareTheTowerPaletteAndKeepOneMeshPerBone()
        {
            DefenderPresentationCatalog catalog = Catalog();
            foreach (PresentationActorKind kind in Kinds())
            {
                GameObject prefab = catalog.ActorPrefab(kind);
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers.SelectMany(r => r.sharedMaterials).Select(m => m.name).Distinct(),
                    Is.EquivalentTo(new[] { "tower_palette" }), $"{kind} materials");
                Assert.That(renderers.SelectMany(r => r.sharedMaterials)
                        .Select(m => catalog.ResolveLicensedAtlasMaterial(m)).Distinct().Single(),
                    Is.SameAs(catalog.towerPaletteMaterial.material), $"{kind} resolves to the shared atlas");
                int body = renderers.Count(r => !r.name.StartsWith("Phase") && !r.name.StartsWith("Ornament_"));
                Assert.That(body, Is.InRange(4, 7), $"{kind} body renderers");
            }
        }

        [Test]
        public void ApprovedActorScalesAreBound()
        {
            DefenderPresentationCatalog catalog = Catalog();
            var expected = new Dictionary<PresentationActorKind, float>
            {
                [PresentationActorKind.Knight] = .92f, [PresentationActorKind.Guard] = .82f,
                [PresentationActorKind.Raider] = .90f, [PresentationActorKind.Vanguard] = .85f,
                [PresentationActorKind.Regenerator] = .84f, [PresentationActorKind.Support] = .87f,
                [PresentationActorKind.Siege] = 1.15f, [PresentationActorKind.Boss] = 1.20f
            };
            foreach (var pair in expected)
                Assert.That(catalog.ActorScale(pair.Key), Is.EqualTo(pair.Value).Within(.001f), pair.Key.ToString());
        }

        [Test]
        public void BossShowsExactlyOneRegionOrnamentPerProfile()
        {
            DefenderPresentationCatalog catalog = Catalog();
            GameObject instance = Object.Instantiate(catalog.ActorPrefab(PresentationActorKind.Boss));
            try
            {
                var phaseVisual = instance.AddComponent<BossPhaseVisual>();
                phaseVisual.Configure(instance.transform, catalog);
                Transform[] parts = instance.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("Ornament_")).ToArray();
                foreach (string id in BossProfileIds)
                {
                    Assert.That(BossEncounterProfile.Resolve(id), Is.Not.Null, id);
                    phaseVisual.SetOrnament(id);
                    Assert.That(phaseVisual.Ornament, Is.EqualTo(id));
                    Transform[] shown = parts.Where(t => t.gameObject.activeSelf).ToArray();
                    Assert.That(shown, Is.Not.Empty, $"{id} ornament");
                    Assert.That(shown.All(t => t.name.StartsWith($"Ornament_{id}_")), Is.True, $"{id} shows only its own dressing");
                }
                phaseVisual.SetOrnament("unknown-lord");
                Assert.That(phaseVisual.Ornament, Is.EqualTo(BossPhaseVisual.DefaultOrnament));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void RoleRigCurvesBindToTheImportedBoneHierarchy()
        {
            DefenderPresentationCatalog catalog = Catalog();
            foreach (PresentationActorKind kind in Kinds())
            {
                var motion = new GameObject("Motion");
                try
                {
                    GameObject art = Object.Instantiate(catalog.ActorPrefab(kind), motion.transform);
                    art.name = "Art";
                    var controller = (AnimatorOverrideController)catalog.ActorAnimator(kind);
                    var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    controller.GetOverrides(overrides);
                    foreach (AnimationClip clip in overrides.Select(pair => pair.Value).Where(clip => clip != null))
                        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                                     .Where(binding => binding.path.StartsWith("Art/")))
                            Assert.That(motion.transform.Find(binding.path), Is.Not.Null, $"{clip.name} {binding.path}");

                    AnimationClip attack = overrides.First(pair => pair.Key.name == PresentationActorAnimationContract.AttackState).Value;
                    Transform arm = motion.transform.Find("Art/Root/Spine/Arm.R");
                    Quaternion rest = arm.localRotation;
                    string lead = kind == PresentationActorKind.Raider ? "Socket_Hand_R" : "Socket_Weapon_Tip";
                    Transform weapon = motion.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == lead);
                    Vector3 weaponRest = weapon != null ? motion.transform.InverseTransformPoint(weapon.position) : Vector3.zero;
                    attack.SampleAnimation(motion, attack.length * .45f);
                    if (weapon != null && kind != PresentationActorKind.Siege)
                        Assert.That(motion.transform.InverseTransformPoint(weapon.position).z - weaponRest.z, Is.GreaterThan(.05f),
                            $"{kind} attack drives {lead} toward the facing direction (+Z)");
                    Assert.That(Quaternion.Angle(rest, arm.localRotation), Is.GreaterThan(5f), $"{kind} attack moves Arm.R");
                }
                finally
                {
                    Object.DestroyImmediate(motion);
                }
            }
        }
#endif
    }
}
