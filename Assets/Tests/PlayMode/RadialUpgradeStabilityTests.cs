using System.Collections;
using System.Reflection;
using AllinDefense.Core;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AllinDefense.Tests
{
    // Emre 2026-10-15: the ring kept moving after upgrades and as towers grew. Every ring on a slot now uses the same
    // three positions: build choices, then Upgrade/branches in the first places and Sell always last.
    public sealed class RadialUpgradeStabilityTests
    {
        private GameSessionController session;
        private DefenderHudController hud;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private Button Choice(string id) => UiQuery.FindById<Button>(hud.BattleRadial, id);
        private Vector2 At(string id) => RectTransformUtility.WorldToScreenPoint(null, Choice(id).transform.position);

        [UnitySetUp] public IEnumerator Setup()
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return null;
            session = new GameObject("Radial upgrade fixture").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(10); session.Initialize();
            hud = session.GetComponentInChildren<DefenderHudController>(); Time.timeScale = 0;
            yield return null;
            Physics.SyncTransforms();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Object.Destroy(session.gameObject); yield return null;
            Time.timeScale = 1;
        }

        private static void Same(Vector2 expected, Vector2 actual, string step) =>
            Assert.That(Vector2.Distance(expected, actual), Is.LessThan(.5f), $"{step}: {expected} → {actual}");

        private IEnumerator Reopen(GreyboxOutpostSlot slot)
        {
            Call(session, "CancelSelection"); yield return null;
            Call(session, "SelectSlot", slot); yield return null; Canvas.ForceUpdateCanvases();
        }

        [UnityTest] public IEnumerator EveryRingOnASlotOpensInTheSameThreePlaces()
        {
            Call(session, "EarnGold", 50000);
            string first = TowerProgression.Branch(OutpostKind.Archer, 0).ToString();
            string second = TowerProgression.Branch(OutpostKind.Archer, 1).ToString();
            foreach (var slot in session.GetComponentsInChildren<GreyboxOutpostSlot>())
            {
                Call(session, "SelectSlot", slot); yield return null; Canvas.ForceUpdateCanvases();
                Vector2 p0 = At("Archer"), p1 = At("Barracks"), p2 = At("Mage");
                string name = $"slot {slot.Index}";

                Choice("Archer").onClick.Invoke(); yield return null;
                hud.RadialAction.onClick.Invoke(); yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(slot.Occupant, Is.Not.Null, name);
                Same(p0, At("Upgrade"), name + " built/Upgrade");
                Same(p2, At("Sell"), name + " built/Sell");

                Choice("Upgrade").onClick.Invoke(); yield return null;
                hud.RadialAction.onClick.Invoke(); yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(slot.Occupant.CanSpecialize, Is.True, name);
                Same(p0, At(first), name + " upgraded/branch 1");
                Same(p1, At(second), name + " upgraded/branch 2");
                Same(p2, At("Sell"), name + " upgraded/Sell");

                yield return Reopen(slot);
                Same(p0, At(first), name + " reopened/branch 1");
                Same(p2, At("Sell"), name + " reopened/Sell");

                Call(session, "CancelSelection"); yield return null;
                Assert.That(session.TrySellOutpost(slot.Index), Is.True);
                Assert.That(session.TryBuildOutpost(slot.Index, OutpostKind.Mage), Is.True);
                yield return Reopen(slot);
                Same(p0, At("Upgrade"), name + " other tower/Upgrade");
                Same(p2, At("Sell"), name + " other tower/Sell");
                Call(session, "CancelSelection"); yield return null;
            }
        }
    }
}
