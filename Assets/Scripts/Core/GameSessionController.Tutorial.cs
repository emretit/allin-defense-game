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
        private CombatRangeVisual tutorialSite;
        private void RefreshFirstBattleGuide()
        {
            if (CampaignLevelNumber != 1 || campaignStore == null || campaignStore.ProgressFor(1)?.stars > 0 ||
                Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) return;
            int step = outposts.Count == 0 ? 0 : outposts.Count < 2 ? 1 :
                Phase == GamePhase.Preparing ? 2 : AbilityUses == 0 ? 3 :
                outposts.Count < 3 ? 4 : !outposts.Any(o=>o.Level>=2) ? 5 : 6;
            if (tutorialSite == null && slots.Count>0) tutorialSite=CombatRangeVisual.Create(transform,"First build site",new Color(1,.8f,.2f));
            tutorialSite?.Show(slots[0].transform.position,.85f,step==0);
            if (tutorialStep == step) return;
            tutorialStep = step;
            string text = step switch
            {
                0 => GameLocalization.Text($"Sarı halkalı noktaya dokun · OKÇU {TowerProgression.Base(OutpostKind.Archer).cost} altın", $"Tap the gold ring · ARCHER costs {TowerProgression.Base(OutpostKind.Archer).cost} gold"),
                1 => GameLocalization.Text("Sonraki yol kıvrımına bir okçu daha kur.", "Build one more archer at the next bend."),
                2 => GameLocalization.Text("İLK DALGAYI BAŞLAT · Düşmanlardan altın kazanırsın.", "START FIRST WAVE · Enemies reward gold."),
                3 => GameLocalization.Text($"{HeroDisplayName}: Sarsıcı Darbe yakındaki düşmanları sersemletir · Basılı tutarak alanı gör.", $"{HeroDisplayName}: Stunning Strike stuns nearby enemies · Hold to preview the area."),
                4 => GameLocalization.Text("Üçüncü yol kıvrımına bir okçu daha kur.", "Build another archer at the third bend."),
                5 => GameLocalization.Text($"Bir okçu seç · {TowerProgression.Base(OutpostKind.Archer).upgradeCost} altınla YÜKSELT.", $"Select an archer · UPGRADE for {TowerProgression.Base(OutpostKind.Archer).upgradeCost} gold."),
                _ => ""
            };
            if (text.Length>0) ShowMessage(text,8);
        }
    }
}
