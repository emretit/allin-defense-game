using System.Linq;
using AllinDefense.Data;
using AllinDefense.Presentation;
using AllinDefense.UI;
using UnityEngine;

namespace AllinDefense.Core
{
    public sealed partial class GameSessionController
    {
        private int tutorialStep = -1;

        private void RefreshFirstBattleGuide()
        {
            if (CampaignLevelNumber != 1 || campaignStore == null || campaignStore.ProgressFor(1)?.stars > 0 ||
                Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) return;
            int step = outposts.Count == 0 ? 0 : outposts.Count < 2 ? 1 :
                Phase == GamePhase.Preparing ? 2 : AbilityUses == 0 ? 3 :
                outposts.Count < 3 ? 4 : !outposts.Any(o=>o.Level>=2) ? 5 : 6;
            tutorialStep = step;
            Transform target = step == 0 ? slots.FirstOrDefault()?.transform :
                step == 1 || step == 4 ? slots.FirstOrDefault(slot => slot.Occupant == null)?.transform :
                step == 5 ? outposts.FirstOrDefault(o => o.Level < 2)?.transform : null;
            artHud?.SetTutorialGuide(step, target, gameCamera);
        }
    }
}
