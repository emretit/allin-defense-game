using DefenderChronicles.Combat;
using DefenderChronicles.Core;
using DefenderChronicles.UI;
using UnityEngine;

namespace DefenderChronicles.Data
{
    public enum OutpostSpecialization { None, RapidFire, Sharpshooter, ShieldGuard, AssaultGuard, Blast, Frost }

    public struct TowerCombatStats
    {
        public int damage, guardHealth, guardCount;
        public float range, attacksPerSecond, splashRadius, slowFactor, slowSeconds;
        public DamageType damageType;
        public bool priorityTargets;
    }

    public static class TowerProgression
    {
        public const int BarracksSquadSize = 3;
        public static OutpostStats Base(OutpostKind kind) => kind switch
        {
            OutpostKind.Archer => new OutpostStats(75, 55, 24, 4.8f, 2.5f),
            OutpostKind.Barracks => new OutpostStats(100, 70, 16, 1.45f, 1f / .375f),
            OutpostKind.Mage => new OutpostStats(125, 85, 38, 4.2f, 1.3f),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind))
        };
        public static int Cost(OutpostKind kind) => kind == OutpostKind.Archer ? 160 : kind == OutpostKind.Barracks ? 180 : 220;
        public static int UnlockLevel(OutpostKind kind) => kind == OutpostKind.Archer ? 6 : kind == OutpostKind.Barracks ? 8 : 7;
        public static bool CanBuild(OutpostKind kind, int unlockedLevel) => kind switch
        { OutpostKind.Archer => true, OutpostKind.Barracks => unlockedLevel >= 2, OutpostKind.Mage => unlockedLevel >= 3, _ => false };
        public static OutpostSpecialization Branch(OutpostKind kind, int choice) => kind switch
        {
            OutpostKind.Archer => choice == 0 ? OutpostSpecialization.RapidFire : OutpostSpecialization.Sharpshooter,
            OutpostKind.Barracks => choice == 0 ? OutpostSpecialization.ShieldGuard : OutpostSpecialization.AssaultGuard,
            _ => choice == 0 ? OutpostSpecialization.Blast : OutpostSpecialization.Frost
        };
        public static bool Matches(OutpostKind kind, OutpostSpecialization branch) =>
            branch != OutpostSpecialization.None && (branch == Branch(kind, 0) || branch == Branch(kind, 1));

        public static TowerCombatStats Stats(OutpostKind kind, int level, OutpostSpecialization branch = OutpostSpecialization.None)
        {
            var data = Base(kind);
            var stats = new TowerCombatStats
            {
                damage = Mathf.RoundToInt(data.damage * (level >= 2 ? (kind == OutpostKind.Archer ? 1.9f : 1.55f) : 1f)),
                range = data.range * (level >= 2 ? 1.12f : 1f),
                attacksPerSecond = data.attacksPerSecond * (level >= 2 ? (kind == OutpostKind.Archer ? 1.44f : 1.2f) : 1f),
                splashRadius = kind == OutpostKind.Mage ? 1.65f : 0f,
                damageType = kind == OutpostKind.Mage ? DamageType.Magic : DamageType.Physical,
                slowFactor = 1f
            };
            if (kind == OutpostKind.Barracks)
            {
                stats.guardCount = BarracksSquadSize;
                stats.damage = level >= 2 ? 32 : 16;
                stats.guardHealth = level >= 2 ? 210 : 110;
                stats.range = .85f;
                stats.attacksPerSecond = 1f / .375f;
            }
            if (level < 2 || !Matches(kind, branch)) return stats;
            switch (branch)
            {
                case OutpostSpecialization.RapidFire:
                    stats.damage = Mathf.RoundToInt(stats.damage * .7f); stats.attacksPerSecond *= 1.8f; break;
                case OutpostSpecialization.Sharpshooter:
                    stats.damage = Mathf.RoundToInt(stats.damage * 1.8f); stats.attacksPerSecond *= .72f;
                    stats.range *= 1.3f; stats.priorityTargets = true; break;
                case OutpostSpecialization.ShieldGuard: stats.guardHealth = Mathf.RoundToInt(stats.guardHealth * 1.8f); break;
                case OutpostSpecialization.AssaultGuard:
                    stats.damage = Mathf.RoundToInt(stats.damage * 1.55f); stats.attacksPerSecond *= 1.15f; break;
                case OutpostSpecialization.Blast:
                    stats.damage = Mathf.RoundToInt(stats.damage * 1.2f); stats.attacksPerSecond *= .9f;
                    stats.splashRadius *= 1.45f; break;
                case OutpostSpecialization.Frost:
                    stats.damage = Mathf.RoundToInt(stats.damage * .7f); stats.slowFactor = .5f; stats.slowSeconds = 2f; break;
            }
            return stats;
        }

        public static string Name(OutpostSpecialization branch) => branch switch
        {
            OutpostSpecialization.RapidFire => GameLocalization.Text("Hızlı Atış", "Rapid Fire"),
            OutpostSpecialization.Sharpshooter => GameLocalization.Text("Keskin Nişancı", "Sharpshooter"),
            OutpostSpecialization.ShieldGuard => GameLocalization.Text("Kalkan Birliği", "Shield Guard"),
            OutpostSpecialization.AssaultGuard => GameLocalization.Text("Hücum Birliği", "Assault Guard"),
            OutpostSpecialization.Blast => GameLocalization.Text("Patlama", "Blast"),
            OutpostSpecialization.Frost => GameLocalization.Text("Buz", "Frost"),
            _ => GameLocalization.Text("Temel savunma", "Basic defence")
        };
        public static string Description(OutpostSpecialization branch) => branch switch
        {
            OutpostSpecialization.RapidFire => GameLocalization.Text("Hafif hedeflere hızlı atış", "Rapid shots against light targets"),
            OutpostSpecialization.Sharpshooter => GameLocalization.Text("Uzun menzil • Önce şifacı ve kuşatma", "Long range • Support and siege priority"),
            OutpostSpecialization.ShieldGuard => GameLocalization.Text("Üç dayanıklı askerle yolu tut", "Three durable soldiers hold the route"),
            OutpostSpecialization.AssaultGuard => GameLocalization.Text("Üç askerde daha yüksek hasar", "Three soldiers deal more damage"),
            OutpostSpecialization.Blast => GameLocalization.Text("Geniş alanda büyü hasarı", "Magic damage over a wider area"),
            OutpostSpecialization.Frost => GameLocalization.Text("Düşmanları 2 sn %50 yavaşlatır", "Slows enemies by 50% for 2 sec"),
            _ => string.Empty
        };
    }
}
