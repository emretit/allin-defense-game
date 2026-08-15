using System;
using UnityEngine;

namespace DefenderChronicles.Data
{
    public enum OutpostKind
    {
        Archer,
        Barracks,
        Mage
    }

    public enum EnemyArchetype
    {
        Raider,
        Vanguard,
        Regenerator,
        Support,
        Siege,
        Boss
    }

    [Serializable]
    public sealed class SpawnGroupDefinition
    {
        public EnemyArchetype enemy;
        [Min(1)] public int count = 1;
        [Min(0f)] public float startDelay;
        [Min(0.1f)] public float interval = 0.8f;

        public SpawnGroupDefinition() { }

        public SpawnGroupDefinition(EnemyArchetype enemy, int count, float startDelay, float interval)
        {
            this.enemy = enemy;
            this.count = Mathf.Max(1, count);
            this.startDelay = Mathf.Max(0f, startDelay);
            this.interval = Mathf.Max(0.1f, interval);
        }
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        public string id;
        public string preview;
        public int clearBonus;
        public bool bossWave;
        public SpawnGroupDefinition[] groups;

        public WaveDefinition() { }

        public WaveDefinition(string id, string preview, int clearBonus, bool bossWave, params SpawnGroupDefinition[] groups)
        {
            this.id = id;
            this.preview = preview;
            this.clearBonus = clearBonus;
            this.bossWave = bossWave;
            this.groups = groups;
        }
    }

    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Defender Chronicles/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string id = "level_greenwatch_01";
        public string displayName = "Greenwatch Pass";
        public Vector3[] path;
        public Vector3[] outpostSlots;
        public Vector3[] commandPoints;
        public WaveDefinition[] waves;
    }

    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Defender Chronicles/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string id;
        public EnemyArchetype archetype;
        public int maxHealth = 100;
        public float moveSpeed = 2f;
        public int reward = 10;
        public int baseDamage = 1;
        public Color color = Color.red;
    }

    [CreateAssetMenu(fileName = "OutpostDefinition", menuName = "Defender Chronicles/Outpost Definition")]
    public sealed class OutpostDefinition : ScriptableObject
    {
        public string id;
        public OutpostKind kind;
        public int cost = 75;
        public int upgradeCost = 60;
        public int damage = 20;
        public float range = 4f;
        public float attacksPerSecond = 1f;
    }

    [CreateAssetMenu(fileName = "HeroDefinition", menuName = "Defender Chronicles/Hero Definition")]
    public sealed class HeroDefinition : ScriptableObject
    {
        public string id = "hero_knight_commander";
        public int maxHealth = 240;
        public int damage = 20;
        public float moveSpeed = 3.5f;
        public float attackRange = 1.5f;
        public float attacksPerSecond = 1.2f;
        public float abilityCooldown = 15f;
    }
}
