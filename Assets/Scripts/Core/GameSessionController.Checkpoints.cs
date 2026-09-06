using System;
using System.Collections.Generic;
using System.Linq;
using DefenderChronicles.Data;
using UnityEngine;

namespace DefenderChronicles.Core
{
    [Serializable] public sealed class ActorCheckpoint
    {
        public int health;
        public float attack, ability, respawn;
        public bool downed;
        public bool hasPosition, deployed;
        public int formationIndex;
        public Vector3 position;
    }
    [Serializable] public sealed class SavedOutpost
    {
        public int slot, level;
        public OutpostKind kind;
        public OutpostSpecialization specialization;
        public float attack, disabled;
        public List<ActorCheckpoint> guards = new();
    }
    [Serializable] public sealed class BattleCheckpoint
    {
        public string ruleVersion = FirstRegionCampaign.Version;
        public string owner;
        public int level, nextWave, gold, lives;
        public float elapsed;
        public ActorCheckpoint hero;
        public List<SavedOutpost> outposts = new();
    }
    public sealed partial class GameSessionController
    {
        private CampaignProgressStore campaignStore;
        private bool restoringCheckpoint;
        public bool CheckpointRestored { get; private set; }
        public CampaignCompletionReward CompletionReward { get; private set; }
        public void ConfigureCampaignStore(CampaignProgressStore store)
        {
            if (initialized) throw new InvalidOperationException("Configure save before battle initialization.");
            campaignStore = store;
            ConfigureProgress(store.Data.unlockedLevel);
        }
        private void SaveWaveCheckpoint()
        {
            if (campaignStore == null || restoringCheckpoint) return;
            campaignStore.Data.checkpoint = new BattleCheckpoint
            {
                owner = campaignStore.ProfileKey, level = CampaignLevelNumber, nextWave = currentWaveIndex,
                gold = Gold, lives = Lives, elapsed = BattleElapsedSeconds, hero = hero?.CaptureCheckpoint(),
                outposts = outposts.Where(o => o != null).Select(o => o.CaptureCheckpoint()).ToList()
            };
            campaignStore.Save();
        }
        private void RestoreWaveCheckpoint()
        {
            var cp = campaignStore?.Data.checkpoint;
            if (cp == null || cp.level != CampaignLevelNumber) return;
            if (!ValidCheckpoint(cp))
            {
                campaignStore.Data.checkpoint = null;
                campaignStore.Save(); // Campaign progress is deliberately independent of this checkpoint.
                return;
            }
            restoringCheckpoint = true;
            foreach (var saved in cp.outposts)
            {
                // Reconstruct exactly the paid inventory, then restore the saved currency.
                Gold = 100000;
                TryBuildOutpost(saved.slot, saved.kind);
                if (saved.level >= 2) TryUpgradeOutpost(saved.slot);
                if (saved.specialization != OutpostSpecialization.None) TrySpecializeOutpost(saved.slot, saved.specialization);
                slots[saved.slot].Occupant.RestoreCheckpoint(saved);
            }
            if (cp.hero != null) hero.RestoreCheckpoint(cp.hero);
            Gold = cp.gold; Lives = cp.lives; currentWaveIndex = cp.nextWave;
            BattleElapsedSeconds = cp.elapsed;
            CheckpointRestored = true;
            restoringCheckpoint = false;
            ShowMessage(DefenderChronicles.UI.GameLocalization.Text("Dalga başındaki savunman geri yüklendi.", "Your defence at the start of the wave was restored."), 5);
        }
        private static bool ValidTimer(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= -1000 && value <= 100000;
        private static bool ValidActor(ActorCheckpoint actor,int maxHealth) => actor != null && actor.health >= 0 && actor.health <= maxHealth &&
            ValidTimer(actor.attack) && ValidTimer(actor.ability) && ValidTimer(actor.respawn);
        private static bool ValidGuard(ActorCheckpoint guard) => ValidActor(guard,400) && guard.health > 0 &&
            (!guard.hasPosition || (guard.formationIndex >= 0 && guard.formationIndex < TowerProgression.BarracksSquadSize &&
             float.IsFinite(guard.position.x) && float.IsFinite(guard.position.y) && float.IsFinite(guard.position.z) && guard.position.sqrMagnitude < 1000000f));
        private bool ValidCheckpoint(BattleCheckpoint cp) => cp.owner == campaignStore.ProfileKey &&
            cp.level >= 1 && cp.level <= FirstRegionCampaign.LevelCount && cp.level <= campaignStore.Data.unlockedLevel &&
            cp.ruleVersion == FirstRegionCampaign.Version && cp.nextWave >= 0 && cp.nextWave < WaveCount &&
            cp.gold >= 0 && cp.lives > 0 && cp.lives <= StartingLivesForLevel && ValidTimer(cp.elapsed) && cp.elapsed >= 0 &&
            ValidActor(cp.hero,240) && (cp.hero.health > 0 || cp.hero.downed) && cp.outposts != null && cp.outposts.All(o=>o!=null) &&
            cp.outposts.Count <= SlotCount && cp.outposts.Select(o => o.slot).Distinct().Count() == cp.outposts.Count &&
            cp.outposts.All(o => o.slot >= 0 && o.slot < SlotCount && Enum.IsDefined(typeof(OutpostKind),o.kind) &&
                CanBuild(o.kind) && ValidTimer(o.attack) && ValidTimer(o.disabled) &&
                o.guards != null && o.guards.Count <= (o.kind == OutpostKind.Barracks ? TowerProgression.BarracksSquadSize : 0) && o.guards.All(ValidGuard) &&
                o.guards.Select((g,index) => g.hasPosition ? g.formationIndex : index).Distinct().Count() == o.guards.Count &&
                o.level >= 1 && o.level <= 3 && (o.level == 3 ? UnlockedCampaignLevel >= TowerProgression.UnlockLevel(o.kind) && TowerProgression.Matches(o.kind, o.specialization) : o.specialization == OutpostSpecialization.None));
        private void ClearCheckpoint()
        {
            if (campaignStore?.Data.checkpoint == null) return;
            campaignStore.Data.checkpoint = null; campaignStore.Save();
        }
        private void OnApplicationPause(bool paused) { if (paused) PauseForBackground(); }
        private void OnApplicationFocus(bool focused) { if (!focused && !Application.runInBackground) PauseForBackground(); }
        private void PauseForBackground()
        {
            if (initialized && Phase is GamePhase.Preparing or GamePhase.Wave or GamePhase.Intermission) OnPausePressed();
        }
    }
}
