using System.Collections;
using System.Reflection;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenderChronicles.Tests
{
    public sealed class SpecializationCombatTests
    {
        [UnityTest]
        public IEnumerator SixBranchesProvideTheirPromisedCombatAdvantage()
        {
            foreach(OutpostKind kind in new[]{OutpostKind.Archer,OutpostKind.Mage,OutpostKind.Barracks})
            foreach(int choice in new[]{0,1})
            {
                var branch=TowerProgression.Branch(kind,choice);
                float baseline=0, specialized=0;
                foreach(bool useBranch in new[]{false,true})
                {
                    var session=new GameObject("Specialization encounter fixture").AddComponent<GameSessionController>();
                    session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
                    session.SetCampaignLevel(10);session.Initialize();Time.timeScale=0;
                    // Funds isolate role mechanics; full campaign affordability is covered by FirstRegionBalanceTests.
                    typeof(GameSessionController).GetMethod("EarnGold",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,new object[]{1000});
                    session.TryBuildOutpost(0,kind);session.TryUpgradeOutpost(0);
                    if(useBranch)Assert.That(session.TrySpecializeOutpost(0,branch),Is.True);
                    var tower=session.Outposts[0];
                    // Both squads reach their road formation before the controlled encounter starts.
                    if(kind==OutpostKind.Barracks)for(int frame=0;frame<300;frame++)session.AdvanceBattle(1f/60);
                    typeof(GameSessionController).GetMethod("SpawnEnemy",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(session,new object[]{EnemyArchetype.Boss});
                    var enemy=session.Enemies[0];
                    var guards=Object.FindObjectsByType<GreyboxFriendlyUnit>(FindObjectsSortMode.None);
                    enemy.transform.position=kind==OutpostKind.Barracks?guards[0].Position:tower.Position+Vector3.forward;
                    int startHealth=enemy.Health;
                    if(branch!=OutpostSpecialization.ShieldGuard)enemy.Stun(30);
                    float metric=0;
                    for(int i=0;i<(branch==OutpostSpecialization.ShieldGuard?600:60);i++)
                    {
                        session.AdvanceBattle(1f/60);
                        if(branch==OutpostSpecialization.ShieldGuard&&guards.All(g=>g!=null&&g.IsAlive))metric+=1f/60;
                    }
                    if(branch==OutpostSpecialization.Frost)metric=1-enemy.SlowFactor;
                    else if(branch!=OutpostSpecialization.ShieldGuard)metric=startHealth-enemy.Health;
                    if(useBranch)specialized=metric;else baseline=metric;
                    Object.Destroy(session.gameObject);yield return null;
                }
                Debug.Log($"[SPECIALIZATION_ADVANTAGE] branch={branch} baseline={baseline:F3} specialized={specialized:F3} metric={(branch==OutpostSpecialization.ShieldGuard?"guard_survival_seconds":branch==OutpostSpecialization.Frost?"slow_fraction":"actual_damage")}");
                Assert.That(specialized,Is.GreaterThan(baseline),branch.ToString());
            }
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            foreach(var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None))Object.Destroy(session.gameObject);
            Time.timeScale=1;yield return null;
        }
    }
}
