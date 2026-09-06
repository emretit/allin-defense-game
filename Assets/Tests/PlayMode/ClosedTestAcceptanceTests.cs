using System.Collections;
using System.IO;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DefenderChronicles.Tests
{
    public sealed class ClosedTestAcceptanceTests
    {
        private string folder;
        private GameSessionController Create(int level, CampaignProgressStore store = null)
        {
            var session = new GameObject("Closed-test acceptance").AddComponent<GameSessionController>();
            session.ConfigurePresentation(Resources.Load<DefenderPresentationCatalog>("Presentation/DefenderPresentationCatalog"));
            session.SetCampaignLevel(level);
            if (store != null) session.ConfigureCampaignStore(store);
            session.Initialize(); Time.timeScale = 0;
            return session;
        }
        [UnityTest]
        public IEnumerator GuidedFirstBattleWinsAndPersistsOnceWithoutResultScreen()
        {
            folder = Path.Combine(Application.temporaryCachePath, "guided-battle-" + System.Guid.NewGuid());
            string path = Path.Combine(folder, "save.json");
            var store = new CampaignProgressStore(path);
            var session = Create(1, store);
            for (int slot = 0; slot < 2; slot++)
                Assert.That(session.TryBuildOutpost(slot, OutpostKind.Archer), Is.True);
            session.StartNextWave();
            for (int frame = 0; frame < 180 * 30 && session.Phase != GamePhase.Victory && session.Phase != GamePhase.Defeat; frame++)
            {
                if (session.Hero.AbilityRemaining == 0) session.Hero.TryUseShieldBash();
                if (session.AbilityUses > 0)
                {
                    if (session.Outposts.Count < 3) session.TryBuildOutpost(2, OutpostKind.Archer);
                    if (session.Outposts[0].CanUpgrade) session.TryUpgradeOutpost(0);
                }
                session.AdvanceBattle(1f / 30);
                if (frame % 300 == 0) yield return null;
            }
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Victory));
            Assert.That(session.Outposts[0].Level, Is.EqualTo(2));
            Assert.That(session.AbilityUses, Is.GreaterThan(0));
            Assert.That(store.ProgressFor(1).stars, Is.GreaterThan(0));
            Assert.That(store.PendingClaimCount, Is.EqualTo(1));
            Object.Destroy(session.gameObject); yield return null;
            var restored = new CampaignProgressStore(path);
            Assert.That(restored.Data.checkpoint, Is.Null);
            Assert.That(restored.PendingClaimCount, Is.EqualTo(1));
            Assert.That(restored.ProgressFor(1).stars, Is.EqualTo(store.ProgressFor(1).stars));
        }
        [UnityTest]
        public IEnumerator WaveRestartRestoresExactResourcesWithoutPayingAgain()
        {
            folder = Path.Combine(Application.temporaryCachePath, "checkpoint-test-" + System.Guid.NewGuid());
            var store = new CampaignProgressStore(Path.Combine(folder, "save.json"));
            var session = Create(1, store);
            session.TryBuildOutpost(0, OutpostKind.Archer);
            session.TryUpgradeOutpost(0);
            session.Hero.TakeDamage(70);
            session.StartNextWave();
            int savedGold = store.Data.checkpoint.gold;
            int savedLives = store.Data.checkpoint.lives;
            float savedHealth = session.Hero.HealthFraction;
            for (int i=0;i<300;i++) session.AdvanceBattle(1f/30);
            Object.Destroy(session.gameObject); yield return null;
            store = new CampaignProgressStore(Path.Combine(folder,"save.json"));
            session = Create(1,store);
            Assert.That(session.CheckpointRestored, Is.True);
            Assert.That(session.Gold, Is.EqualTo(savedGold)); Assert.That(session.Lives,Is.EqualTo(savedLives));
            Assert.That(session.Hero.HealthFraction, Is.EqualTo(savedHealth));
            Assert.That(session.Outposts.Single().Level, Is.EqualTo(2));
            Assert.That(store.PendingClaimCount, Is.Zero);
            Assert.That(session.CurrentWaveIndex,Is.Zero);
        }
        [UnityTest]
        public IEnumerator IncompatibleCheckpointLeavesCampaignFiftyIntact()
        {
            folder = Path.Combine(Application.temporaryCachePath,"checkpoint-test-"+System.Guid.NewGuid());
            var store=new CampaignProgressStore(Path.Combine(folder,"save.json"));
            store.Data.unlockedLevel=50;
            store.Data.levels.Add(new CampaignLevelProgress{level=50,stars=3,bestLives=12});
            store.Data.checkpoint=new BattleCheckpoint{level=1,ruleVersion="old",owner=store.ProfileKey,lives=20,gold=400};store.Save();
            var session=Create(1,store);
            Assert.That(session.CheckpointRestored,Is.False);
            Assert.That(store.Data.unlockedLevel,Is.EqualTo(50));
            Assert.That(store.ProgressFor(50).stars,Is.EqualTo(3));
            yield return null;
        }
        [UnityTest]
        public IEnumerator FrameRateAndSpeedHaveIdenticalBattleResults()
        {
            string baseline=null;
            foreach(int fps in new[]{30,60}) foreach(int speed in new[]{1,2,4,8})
            {
                var session=Create(1);
                session.TryBuildOutpost(0,OutpostKind.Archer);session.TryBuildOutpost(1,OutpostKind.Archer);session.TryBuildOutpost(2,OutpostKind.Archer);
                session.StartNextWave();
                int frames=0;
                while(session.Phase is not (GamePhase.Victory or GamePhase.Defeat) && frames<600*fps/speed)
                {
                    session.AdvanceBattle(speed/(float)fps);frames++;
                    if(frames%300==0)yield return null;
                }
                string actual=$"{session.Phase}/{session.Gold}/{session.Lives}/{session.CurrentWaveIndex}/{session.BattleElapsedSeconds:F2}";
                Debug.Log($"[CLOCK_PARITY] fps={fps} speed={speed} result={actual}");
                baseline ??=actual;Assert.That(actual,Is.EqualTo(baseline));
                Object.Destroy(session.gameObject);yield return null;
            }
        }
        [UnityTest]
        public IEnumerator SpecializationIsExclusiveAndRefundIncludesItsPrice()
        {
            var session=Create(10);
            Assert.That(session.TryBuildOutpost(0,OutpostKind.Archer),Is.True);
            Assert.That(session.TryUpgradeOutpost(0),Is.True);
            Assert.That(session.TrySpecializeOutpost(0,OutpostSpecialization.RapidFire),Is.True);
            int gold=session.Gold;
            Assert.That(session.TrySpecializeOutpost(0,OutpostSpecialization.Sharpshooter),Is.False);
            Assert.That(session.Gold,Is.EqualTo(gold));
            int refund=CombatMath.Refund(75+55+160);
            Assert.That(session.Outposts[0].SellValue,Is.EqualTo(refund));
            Assert.That(session.TrySellOutpost(0),Is.True);
            Assert.That(session.Gold,Is.EqualTo(gold+refund));
            yield return null;
        }
        [UnityTest]
        public IEnumerator EmptyShieldBashDoesNotSpendCooldown()
        {
            var session=Create(1);
            Assert.That(session.Hero.TryUseShieldBash(),Is.False);
            Assert.That(session.Hero.AbilityRemaining,Is.Zero);
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var session in Object.FindObjectsByType<GameSessionController>(FindObjectsSortMode.None))Object.Destroy(session.gameObject);
            Time.timeScale=1;yield return null;
            if(folder!=null&&Directory.Exists(folder))Directory.Delete(folder,true);
        }
    }
}
