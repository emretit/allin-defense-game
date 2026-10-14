using System.Linq;
using AllinDefense.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AllinDefense.Tests
{
    public sealed class BarracksPresentationTests
    {
        [Test]
        public void GuardStaysUprightWhenIdleMovingAndAttacking()
        {
            var catalog = Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog");
            var prefab = catalog.ActorPrefab(PresentationActorKind.Guard);
            var root = StylizedPresentationFactory.CreateRoot("Guard pose regression", Vector3.zero,
                new Vector3(.52f, 1.25f, .52f), prefab, catalog, PresentationCollider.Capsule,
                catalog.ActorScale(PresentationActorKind.Guard), catalog.ActorAnimator(PresentationActorKind.Guard),
                PresentationActorKind.Guard);
            try
            {
                var head = root.GetComponentsInChildren<Transform>().First(t => t.name == "Socket_Head");
                var boots = root.GetComponentsInChildren<Transform>().Where(t => t.name is "Leg.L" or "Leg.R")
                    .SelectMany(leg => leg.GetComponentsInChildren<Renderer>()).ToArray();
                Assert.That(boots, Is.Not.Empty, "Guard legs need rigid-parented geometry.");
                var animator = root.GetComponentInChildren<Animator>();
                Debug.Log($"[GUARD_POSE] imported_rotation={prefab.transform.localEulerAngles} initial_head={head.position} boots={string.Join(",", boots.Select(b => b.bounds.min.ToString()))}");
                animator.Rebind();
                foreach (var state in new[] { "Idle", "Move", "Attack" })
                {
                    animator.Play(state, 0, 0);
                    animator.Update(.1f);
                    foreach (float facing in new[] { 0f, 90f, 180f, 270f })
                    {
                        root.transform.rotation = Quaternion.Euler(0, facing, 0);
                        float feet = boots.Min(b => b.bounds.min.y);
                        Debug.Log($"[GUARD_POSE] state={state} facing={facing} head_y={head.position.y:F3} feet_y={feet:F3}");
                        Assert.That(head.position.y - feet, Is.GreaterThan(.45f), "Guard must stand above the road regardless of facing or animation.");
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
