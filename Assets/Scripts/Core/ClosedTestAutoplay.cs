using System;
using System.Linq;
using DefenderChronicles.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefenderChronicles.Core
{
    // Explicit development-only device harness. Uses production purchases and the normal 1x clock.
    public sealed class ClosedTestAutoplay : MonoBehaviour
    {
        public static bool Enabled => Debug.isDebugBuild && Environment.GetEnvironmentVariable("DC_CLOSED_TEST_AUTOPLAY") == "1";
        private static float started;
        private static int run;
        public static int Level => new[] { 1, 4, 10 }[run % 3];
        private GameSessionController session;
        private float nextAction, startWaveAt;
        private bool leaving;
        private void Start()
        {
            session = GetComponent<GameSessionController>();
            if (started <= 0) started = Time.realtimeSinceStartup;
            startWaveAt = Time.realtimeSinceStartup + 5;
            Debug.Log($"[CLOSED_TEST_DEVICE_RUN] build={ClosedTestBuild.Id} run={run} level={Level} duration=1800 speed=1");
        }
        private void Update()
        {
            if (leaving || session == null) return;
            if (Time.realtimeSinceStartup - started >= 1800)
            {
                leaving = true;
                Debug.Log($"[CLOSED_TEST_DEVICE_COMPLETE] build={ClosedTestBuild.Id} elapsed={Time.realtimeSinceStartup-started:F1} runs={run} model={SystemInfo.deviceModel}");
                Application.Quit(); return;
            }
            if (session.Phase is GamePhase.Victory or GamePhase.Defeat)
            {
                leaving = true; run++; Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return;
            }
            if (Time.realtimeSinceStartup < nextAction) return;
            nextAction = Time.realtimeSinceStartup + 1;
            for (int slot=0;slot<session.SlotCount;slot++)
            {
                var tower=session.Outposts.FirstOrDefault(o=>o!=null&&o.Slot.Index==slot);
                var kind=Level==1?OutpostKind.Archer:(OutpostKind)(slot%3);
                if(tower==null)session.TryBuildOutpost(slot,kind);
                else if(tower.CanUpgrade)session.TryUpgradeOutpost(slot);
                else if(tower.CanSpecialize&&session.BattleElapsedSeconds>60)session.TrySpecializeOutpost(slot,TowerProgression.Branch(tower.Kind,run%2));
            }
            if(session.Phase==GamePhase.Preparing&&Time.realtimeSinceStartup>=startWaveAt)session.StartNextWave();
            if(session.Phase==GamePhase.Wave)session.Hero?.TryUseShieldBash();
        }
    }
}
