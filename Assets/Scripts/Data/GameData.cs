using UnityEngine;

namespace DefenderChronicles.Data
{
    public enum TowerType
    {
        Archer,
        Mage,
        Warrior,
        Catapult
    }

    public enum EnemyType
    {
        Goblin,
        Orc,
        Troll,
        Dragon,
        Support,
        Siege,
        Boss
    }

    public enum EnemyRole
    {
        Fast,
        Tank,
        Swarm,
        Ranged,
        Flying,
        Support,
        Siege,
        Boss
    }

    public enum HeroType
    {
        Elven,
        Knight,
        Wizard,
        Ranger
    }

    public enum ProjectileType
    {
        Arrow,
        Fireball,
        Rock,
        IceShard
    }

    public enum CurrencyType
    {
        BattleGold,
        Valor
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Charm
    }

    public enum EquipmentRarity
    {
        Common,
        Rare,
        Epic
    }

    [System.Serializable]
    public class TowerData
    {
        public string towerName;
        public TowerType towerType;
        public int cost;
        public int upgradeCost;
        public float attackRange;
        public float attackSpeed;
        public int damage;
        public int level;
        public ProjectileType projectileType;
        public Sprite icon;
    }

    [System.Serializable]
    public class EnemyData
    {
        public string enemyName;
        public EnemyType enemyType;
        public EnemyRole enemyRole;
        public int maxHealth;
        public float moveSpeed;
        public int goldReward;
        public int damage;
        public Sprite sprite;
    }

    [System.Serializable]
    public class HeroData
    {
        public string heroName;
        public HeroType heroType;
        public int cost;
        public float attackRange;
        public float attackSpeed;
        public int damage;
        public int maxHealth;
        public int level;
        public int experienceToNextLevel;
        public Sprite portrait;
        public string specialAbilityName;
        public string specialAbilityDescription;
    }

    [System.Serializable]
    public class EquipmentData
    {
        public string id;
        public string displayName;
        public EquipmentSlot slot;
        public EquipmentRarity rarity;
        public int power;
        public string gameplayModifierId;
        public Sprite icon;
    }

    [System.Serializable]
    public class RewardData
    {
        public CurrencyType currencyType;
        public int amount;
        public string unlockContentId;
        public EquipmentData equipment;
    }

    [System.Serializable]
    public class WaveData
    {
        public int waveNumber;
        public EnemySpawnInfo[] enemies;
        public float timeBetweenSpawns;
        public int goldBonus;
        public int threatBudget;
        public string previewLabel;
        public bool isBossWave;
    }

    [System.Serializable]
    public class EnemySpawnInfo
    {
        public EnemyType enemyType;
        public int count;
        public float spawnDelay;
        public int entryPointIndex;
        public string groupId;
        public int threatCost;
        public bool isSpecialThreat;
    }

    /// <summary>Shallow copies stats so runtime upgrades / Warrior range tweaks do not mutate shared assets.</summary>
    public static class GameDataClone
    {
        public static TowerData Tower(TowerData s)
        {
            if (s == null) return null;
            return new TowerData
            {
                towerName = s.towerName,
                towerType = s.towerType,
                cost = s.cost,
                upgradeCost = s.upgradeCost,
                attackRange = s.attackRange,
                attackSpeed = s.attackSpeed,
                damage = s.damage,
                level = s.level,
                projectileType = s.projectileType,
                icon = s.icon
            };
        }

        public static EnemyData Enemy(EnemyData s)
        {
            if (s == null) return null;
            return new EnemyData
            {
                enemyName = s.enemyName,
                enemyType = s.enemyType,
                enemyRole = s.enemyRole,
                maxHealth = s.maxHealth,
                moveSpeed = s.moveSpeed,
                goldReward = s.goldReward,
                damage = s.damage,
                sprite = s.sprite
            };
        }

        public static HeroData Hero(HeroData s)
        {
            if (s == null) return null;
            return new HeroData
            {
                heroName = s.heroName,
                heroType = s.heroType,
                cost = s.cost,
                attackRange = s.attackRange,
                attackSpeed = s.attackSpeed,
                damage = s.damage,
                maxHealth = s.maxHealth,
                level = s.level,
                experienceToNextLevel = s.experienceToNextLevel,
                portrait = s.portrait,
                specialAbilityName = s.specialAbilityName,
                specialAbilityDescription = s.specialAbilityDescription
            };
        }
    }
}
