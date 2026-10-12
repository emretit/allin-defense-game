using System.Collections.Generic;
using AllinDefense.Data;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public enum DefenderVfxShape
    {
        Diamond,
        Ring
    }

    public enum DefenderVfxMaterialRole
    {
        Magic,
        Fire,
        Enemy,
        Dark,
        Defender
    }

    public enum DefenderVfxImpactClass
    {
        None,
        PhysicalLight,
        PhysicalBlock,
        PhysicalHeavy,
        Magical,
        Status,
        SpawnDeath
    }

    public enum DefenderVfxPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public readonly struct DefenderVfxQualityConfig
    {
        public readonly string qualityName;
        public readonly int prewarmPoolSize;
        public readonly int maxPlayingCount;
        public readonly int bossImpactMaxConcurrent;
        public readonly int bossTelegraphMaxConcurrent;
        public readonly float transparentCostBudget;

        public DefenderVfxQualityConfig(string qualityName, int prewarmPoolSize, int maxPlayingCount,
            int bossImpactMaxConcurrent, int bossTelegraphMaxConcurrent, float transparentCostBudget)
        {
            this.qualityName = qualityName;
            this.prewarmPoolSize = Mathf.Max(1, prewarmPoolSize);
            this.maxPlayingCount = Mathf.Max(1, maxPlayingCount);
            this.bossImpactMaxConcurrent = Mathf.Max(1, bossImpactMaxConcurrent);
            this.bossTelegraphMaxConcurrent = Mathf.Max(1, bossTelegraphMaxConcurrent);
            this.transparentCostBudget = Mathf.Max(1f, transparentCostBudget);
        }
    }

    public readonly struct DefenderVfxSpec
    {
        public readonly DefenderVfxShape shape;
        public readonly DefenderVfxMaterialRole materialRole;
        public readonly DefenderVfxImpactClass impactClass;
        public readonly DefenderVfxPriority priority;
        public readonly Color color;
        public readonly bool projectile;
        public readonly float seconds;
        public readonly float endScale;
        public readonly string sourceSocket;
        public readonly string targetSocket;
        public readonly string concurrencyGroup;
        public readonly int maxConcurrent;

        public DefenderVfxSpec(DefenderVfxShape shape, DefenderVfxMaterialRole materialRole, Color color,
            bool projectile, float seconds, float endScale, string sourceSocket = null, string targetSocket = null,
            DefenderVfxImpactClass impactClass = DefenderVfxImpactClass.None,
            DefenderVfxPriority priority = DefenderVfxPriority.Normal, string concurrencyGroup = null,
            int maxConcurrent = int.MaxValue)
        {
            this.shape = shape;
            this.materialRole = materialRole;
            this.impactClass = impactClass;
            this.priority = priority;
            this.color = color;
            this.projectile = projectile;
            this.seconds = seconds;
            this.endScale = endScale;
            this.sourceSocket = sourceSocket;
            this.targetSocket = targetSocket;
            this.concurrencyGroup = concurrencyGroup;
            this.maxConcurrent = Mathf.Max(1, maxConcurrent);
        }
    }

    public sealed class DefenderVfxPool : MonoBehaviour
    {
        private readonly List<PooledVfx> pool = new();
        private DefenderPresentationCatalog catalog;
        private Mesh diamondMesh;
        private Mesh ringMesh;
        private DefenderVfxQualityConfig qualityConfig = DefaultQualityConfig("Medium");

        public int PoolCount => pool.Count;
        public DefenderVfxQualityConfig ActiveQualityConfig => qualityConfig;
        public int PlayingCount
        {
            get
            {
                int count = 0;
                foreach (PooledVfx item in pool)
                    if (item.IsPlaying)
                        count++;
                return count;
            }
        }

        public void Initialize(DefenderPresentationCatalog value, int initialSize = 24)
        {
            Initialize(value, new DefenderVfxQualityConfig("Custom", initialSize, initialSize, 3, 2, 120f));
        }

        public void InitializeForCurrentQuality(DefenderPresentationCatalog value)
        {
            Initialize(value, QualityConfigForCurrentQuality());
        }

        public void Initialize(DefenderPresentationCatalog value, DefenderVfxQualityConfig config)
        {
            catalog = value;
            qualityConfig = config;
            if (diamondMesh == null) diamondMesh = BuildDiamond();
            if (ringMesh == null) ringMesh = BuildRing(24);
            while (pool.Count < qualityConfig.prewarmPoolSize) CreateItem();
        }

        private void OnDestroy()
        {
            if (diamondMesh != null) Destroy(diamondMesh);
            if (ringMesh != null) Destroy(ringMesh);
        }

        public void Play(PresentationSignal signal)
        {
            if (catalog == null) return;
            DefenderVfxSpec spec = ApplyQualityCaps(Describe(signal));
            PooledVfx item = AcquireItem(spec);
            if (item == null) return;
            Vector3 from = ResolveSocketPosition(signal.source, spec.sourceSocket,
                signal.source != null ? signal.source.transform.position + Vector3.up * 0.8f : signal.position);
            Vector3 toFallback = signal.position + Vector3.up * (spec.projectile ? 0.45f : 0.08f);
            Vector3 to = ResolveSocketPosition(signal.source, spec.targetSocket, toFallback);
            item.Play(spec.shape == DefenderVfxShape.Ring ? ringMesh : diamondMesh, MaterialFor(spec.materialRole),
                from, to, spec.color, spec.seconds, spec.projectile, spec.endScale, spec.priority,
                spec.concurrencyGroup);
        }

        public int PlayingCountForGroup(string group)
        {
            int count = 0;
            foreach (PooledVfx item in pool)
                if (item.IsPlaying && item.ConcurrencyGroup == group)
                    count++;
            return count;
        }

        public int PlayingCountAtPriorityOrAbove(DefenderVfxPriority priority)
        {
            int count = 0;
            foreach (PooledVfx item in pool)
                if (item.IsPlaying && item.Priority >= priority)
                    count++;
            return count;
        }

        private PooledVfx AcquireItem(DefenderVfxSpec spec)
        {
            bool hasConcurrencyCap = !string.IsNullOrWhiteSpace(spec.concurrencyGroup) &&
                                     spec.maxConcurrent != int.MaxValue;
            if (!hasConcurrencyCap || PlayingCountForGroup(spec.concurrencyGroup) < spec.maxConcurrent)
            {
                return AcquireWithinGlobalBudget(spec);
            }

            PooledVfx oldest = null;
            foreach (PooledVfx item in pool)
            {
                if (!item.IsPlaying || item.ConcurrencyGroup != spec.concurrencyGroup || item.Priority > spec.priority)
                    continue;
                if (oldest == null || item.StartedAt < oldest.StartedAt)
                    oldest = item;
            }
            if (oldest != null) return oldest;

            PooledVfx fallback = pool.Find(candidate => !candidate.IsPlaying);
            return spec.priority == DefenderVfxPriority.Critical ? fallback ?? AcquireWithinGlobalBudget(spec) : fallback;
        }

        private PooledVfx AcquireWithinGlobalBudget(DefenderVfxSpec spec)
        {
            PooledVfx free = pool.Find(candidate => !candidate.IsPlaying);
            if (free != null) return free;
            if (PlayingCount < qualityConfig.maxPlayingCount) return CreateItem();

            PooledVfx oldest = null;
            foreach (PooledVfx item in pool)
            {
                if (!item.IsPlaying || item.Priority > spec.priority)
                    continue;
                if (oldest == null || item.StartedAt < oldest.StartedAt)
                    oldest = item;
            }
            return oldest;
        }

        private PooledVfx CreateItem()
        {
            var go = new GameObject($"Pooled VFX {pool.Count + 1}");
            go.transform.SetParent(transform, false);
            var item = go.AddComponent<PooledVfx>();
            item.Prewarm();
            pool.Add(item);
            return item;
        }

        public static DefenderVfxQualityConfig QualityConfigForCurrentQuality()
        {
            string[] names = QualitySettings.names;
            int index = QualitySettings.GetQualityLevel();
            string qualityName = names != null && index >= 0 && index < names.Length ? names[index] : "Medium";
            return QualityConfigForName(qualityName);
        }

        public static DefenderVfxQualityConfig QualityConfigForName(string qualityName)
        {
            string normalized = (qualityName ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "low")
                return new DefenderVfxQualityConfig("Low", 72, 72, 2, 1, 82f);
            if (normalized == "high")
                return new DefenderVfxQualityConfig("High", 128, 128, 3, 2, 120f);
            return DefaultQualityConfig(string.IsNullOrWhiteSpace(qualityName) ? "Medium" : qualityName);
        }

        private static DefenderVfxQualityConfig DefaultQualityConfig(string qualityName)
        {
            return new DefenderVfxQualityConfig(qualityName, 96, 96, 3, 2, 104f);
        }

        private DefenderVfxSpec ApplyQualityCaps(DefenderVfxSpec spec)
        {
            if (spec.concurrencyGroup == "BossImpact")
                return CopySpecWithMaxConcurrent(spec, qualityConfig.bossImpactMaxConcurrent);
            if (spec.concurrencyGroup == "BossTelegraph")
                return CopySpecWithMaxConcurrent(spec, qualityConfig.bossTelegraphMaxConcurrent);
            return spec;
        }

        private static DefenderVfxSpec CopySpecWithMaxConcurrent(DefenderVfxSpec spec, int maxConcurrent)
        {
            return new DefenderVfxSpec(spec.shape, spec.materialRole, spec.color, spec.projectile, spec.seconds,
                spec.endScale, spec.sourceSocket, spec.targetSocket, spec.impactClass, spec.priority,
                spec.concurrencyGroup, maxConcurrent);
        }

        public static DefenderVfxSpec Describe(PresentationSignal signal)
        {
            if (signal.cue == PresentationCue.HeroAttacked && signal.actor == PresentationActorKind.Knight)
                return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Fire,
                    new Color(1f, 0.76f, 0.18f), true, 0.22f, 0.82f, "Socket_Weapon_Tip",
                    impactClass: DefenderVfxImpactClass.PhysicalLight);

            if (signal.cue == PresentationCue.HeroAbility && signal.actor == PresentationActorKind.Knight)
                return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender,
                    new Color(0.34f, 0.86f, 1f), false, 0.42f, 3.2f, "Socket_Ground",
                    impactClass: DefenderVfxImpactClass.PhysicalBlock);

            if (signal.actor == PresentationActorKind.Guard)
            {
                if (signal.cue == PresentationCue.OutpostAttacked)
                    return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Defender,
                        new Color(0.60f, 0.92f, 1f), false, 0.20f, 0.56f, "Socket_Weapon_Tip",
                        impactClass: DefenderVfxImpactClass.PhysicalLight);
                if (signal.cue == PresentationCue.HeroAbility)
                    return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender,
                        new Color(0.40f, 0.78f, 1f), false, 0.28f, 1.65f, "Socket_Chest",
                        impactClass: DefenderVfxImpactClass.PhysicalBlock);
            }

            if (signal.actor == PresentationActorKind.Raider)
            {
                if (signal.cue == PresentationCue.UnitSpawned)
                    return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark,
                        new Color(0.82f, 0.16f, 0.18f), false, 0.32f, 0.95f, "Socket_Ground",
                        impactClass: DefenderVfxImpactClass.SpawnDeath);
                if (signal.cue == PresentationCue.EnemyHit)
                    return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy,
                        new Color(1f, 0.28f, 0.12f), true, 0.16f, 0.52f, "Socket_Chest",
                        impactClass: DefenderVfxImpactClass.PhysicalLight);
                if (signal.cue == PresentationCue.EnemyDefeated)
                    return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Enemy,
                        new Color(1f, 0.22f, 0.08f), false, 0.36f, 1.18f, "Socket_Ground",
                        impactClass: DefenderVfxImpactClass.SpawnDeath);
            }

            if (signal.actor == PresentationActorKind.Vanguard && signal.cue == PresentationCue.OutpostDisrupted)
                return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy,
                    new Color(0.95f, 0.18f, 0.10f), false, 0.34f, 0.88f, "Socket_Weapon_Tip",
                    impactClass: DefenderVfxImpactClass.PhysicalHeavy, priority: DefenderVfxPriority.High,
                    concurrencyGroup: "VanguardImpact", maxConcurrent: 6);

            if (signal.actor == PresentationActorKind.Siege && signal.cue == PresentationCue.OutpostDisrupted)
                return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Dark,
                    new Color(0.98f, 0.34f, 0.10f), true, 0.38f, 1.12f, "Socket_Muzzle",
                    impactClass: DefenderVfxImpactClass.PhysicalHeavy, priority: DefenderVfxPriority.High,
                    concurrencyGroup: "SiegeImpact", maxConcurrent: 4);

            if (signal.cue == PresentationCue.BossAttack)
                return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Enemy,
                    new Color(1f, 0.30f, 0.08f), true, 0.32f, 1.05f, "Socket_Weapon_Tip",
                    impactClass: DefenderVfxImpactClass.PhysicalHeavy, priority: DefenderVfxPriority.High,
                    concurrencyGroup: "BossImpact", maxConcurrent: 3);

            if (signal.cue == PresentationCue.BossPhase)
                return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark,
                    new Color(1f, 0.16f, 0.08f), false, 0.54f, 2.7f, "Socket_Ground",
                    impactClass: DefenderVfxImpactClass.Status, priority: DefenderVfxPriority.Critical,
                    concurrencyGroup: "BossTelegraph", maxConcurrent: 2);

            if (signal.cue == PresentationCue.OutpostAttacked && signal.outpost == OutpostKind.Archer)
                return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Fire,
                    new Color(1f, 0.68f, 0.18f), true, 0.18f, 0.44f, "Socket_Muzzle",
                    impactClass: DefenderVfxImpactClass.PhysicalLight);

            if (signal.cue == PresentationCue.MageImpact && signal.outpost == OutpostKind.Mage)
                return new DefenderVfxSpec(DefenderVfxShape.Diamond, DefenderVfxMaterialRole.Magic,
                    new Color(0.24f, 0.75f, 0.94f), true, 0.26f, 0.72f, "Socket_Cast",
                    impactClass: DefenderVfxImpactClass.Magical);

            if (signal.cue == PresentationCue.HealPulse && signal.actor == PresentationActorKind.Support)
                return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Defender,
                    new Color(0.34f, 1f, 0.48f), true, 0.42f, 0.96f, "Socket_Cast",
                    impactClass: DefenderVfxImpactClass.Magical, concurrencyGroup: "SupportHeal",
                    maxConcurrent: 6);

            if (signal.cue == PresentationCue.HealPulse && signal.actor == PresentationActorKind.Regenerator)
                return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Enemy,
                    new Color(0.18f, 0.92f, 0.34f), false, 0.34f, 1.18f, "Socket_Core", "Socket_Ground",
                    impactClass: DefenderVfxImpactClass.Magical, concurrencyGroup: "RegeneratorPulse",
                    maxConcurrent: 8);

            if (signal.cue == PresentationCue.SlowApplied)
                return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Magic,
                    new Color(0.24f, 0.75f, 0.94f), false, 0.48f, 1.25f, "Socket_Chest",
                    impactClass: DefenderVfxImpactClass.Status);

            if (signal.cue == PresentationCue.OutpostDisrupted)
                return new DefenderVfxSpec(DefenderVfxShape.Ring, DefenderVfxMaterialRole.Dark,
                    new Color(0.85f, 0.12f, 0.16f), false, 0.46f, 1.35f, "Socket_Ground",
                    impactClass: DefenderVfxImpactClass.Status);

            bool projectile = signal.cue is PresentationCue.MageImpact
                or PresentationCue.HeroAttacked or PresentationCue.BossAttack;
            bool ring = signal.cue is PresentationCue.HealPulse or PresentationCue.OutpostDisrupted
                or PresentationCue.SlowApplied or PresentationCue.HeroAbility or PresentationCue.BossPhase
                or PresentationCue.UnitSpawned or PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated
                or PresentationCue.Victory or PresentationCue.Defeat;
            DefenderVfxMaterialRole role = projectile ? DefenderVfxMaterialRole.Fire : DefenderVfxMaterialRole.Magic;
            if (signal.cue == PresentationCue.OutpostAttacked)
                role = DefenderVfxMaterialRole.Fire;
            if (signal.cue == PresentationCue.MageImpact)
                role = DefenderVfxMaterialRole.Magic;
            if (signal.cue is PresentationCue.EnemyHit or PresentationCue.EnemyDefeated or PresentationCue.BossAttack)
                role = DefenderVfxMaterialRole.Enemy;
            if (signal.cue is PresentationCue.OutpostDisrupted or PresentationCue.BossPhase)
                role = DefenderVfxMaterialRole.Dark;
            if (signal.cue is PresentationCue.HealPulse or PresentationCue.UnitSpawned)
                role = DefenderVfxMaterialRole.Defender;
            string sourceSocket = signal.cue switch
            {
                PresentationCue.OutpostAttacked or PresentationCue.BossAttack => "Socket_Weapon_Tip",
                PresentationCue.MageImpact => "Socket_Cast",
                PresentationCue.HealPulse or PresentationCue.OutpostDisrupted or PresentationCue.SlowApplied
                    or PresentationCue.BossPhase or PresentationCue.UnitSpawned or PresentationCue.EnemyDefeated
                    or PresentationCue.UnitDefeated or PresentationCue.Victory or PresentationCue.Defeat => "Socket_Ground",
                PresentationCue.EnemyHit => "Socket_Chest",
                _ => null
            };
            return new DefenderVfxSpec(ring ? DefenderVfxShape.Ring : DefenderVfxShape.Diamond, role,
                CueColor(signal.cue), projectile, projectile ? 0.24f : 0.34f, EndScale(signal.cue), sourceSocket,
                impactClass: ImpactClass(signal.cue));
        }

        public static Vector3 ResolveSocketPosition(GameObject source, string socketName, Vector3 fallback)
        {
            if (source == null || string.IsNullOrWhiteSpace(socketName)) return fallback;
            foreach (Transform child in source.GetComponentsInChildren<Transform>(true))
                if (child.name == socketName)
                    return child.position;
            return fallback;
        }

        private Material MaterialFor(DefenderVfxMaterialRole role) => role switch
        {
            DefenderVfxMaterialRole.Fire => catalog.fireMaterial,
            DefenderVfxMaterialRole.Enemy => catalog.enemyMaterial,
            DefenderVfxMaterialRole.Dark => catalog.darkMaterial,
            DefenderVfxMaterialRole.Defender => catalog.defenderMaterial,
            _ => catalog.magicMaterial
        };

        private static float EndScale(PresentationCue cue) => cue switch
        {
            PresentationCue.HeroAbility => 3.2f,
            PresentationCue.BossPhase => 2.7f,
            PresentationCue.Victory or PresentationCue.Defeat => 2.2f,
            PresentationCue.HealPulse => 1.45f,
            PresentationCue.UnitSpawned => 0.95f,
            PresentationCue.OutpostDisrupted or PresentationCue.SlowApplied => 1.05f,
            PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated => 1.18f,
            PresentationCue.EnemyHit => 0.36f,
            _ => 0.62f
        };

        private static DefenderVfxImpactClass ImpactClass(PresentationCue cue) => cue switch
        {
            PresentationCue.HeroAttacked or PresentationCue.OutpostAttacked => DefenderVfxImpactClass.PhysicalLight,
            PresentationCue.HeroAbility => DefenderVfxImpactClass.PhysicalBlock,
            PresentationCue.BossAttack => DefenderVfxImpactClass.PhysicalHeavy,
            PresentationCue.MageImpact or PresentationCue.HealPulse => DefenderVfxImpactClass.Magical,
            PresentationCue.OutpostDisrupted or PresentationCue.SlowApplied or PresentationCue.BossPhase =>
                DefenderVfxImpactClass.Status,
            PresentationCue.UnitSpawned or PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated
                or PresentationCue.Victory or PresentationCue.Defeat => DefenderVfxImpactClass.SpawnDeath,
            _ => DefenderVfxImpactClass.None
        };

        private static Color CueColor(PresentationCue cue) => cue switch
        {
            PresentationCue.EnemyHit or PresentationCue.EnemyDefeated or PresentationCue.BossAttack => new Color(1f, 0.38f, 0.12f),
            PresentationCue.HealPulse => new Color(0.34f, 1f, 0.48f),
            PresentationCue.OutpostDisrupted or PresentationCue.BossPhase => new Color(0.85f, 0.12f, 0.16f),
            PresentationCue.SlowApplied or PresentationCue.HeroAbility or PresentationCue.MageImpact => new Color(0.24f, 0.75f, 0.94f),
            PresentationCue.Victory => new Color(1f, 0.72f, 0.16f),
            _ => new Color(0.35f, 0.72f, 1f)
        };

        private static Mesh BuildDiamond()
        {
            var mesh = new Mesh { name = "DC VFX Diamond" };
            mesh.vertices = new[]
            {
                new Vector3(0f, .35f, 0f), new Vector3(.22f, 0f, 0f), new Vector3(0f, 0f, .22f),
                new Vector3(-.22f, 0f, 0f), new Vector3(0f, 0f, -.22f), new Vector3(0f, -.35f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildRing(int segments)
        {
            var mesh = new Mesh { name = "DC VFX Ring" };
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * 0.82f;
                vertices[i * 2 + 1] = direction;
                int next = (i + 1) % segments;
                int offset = i * 6;
                triangles[offset] = i * 2;
                triangles[offset + 1] = next * 2 + 1;
                triangles[offset + 2] = i * 2 + 1;
                triangles[offset + 3] = i * 2;
                triangles[offset + 4] = next * 2;
                triangles[offset + 5] = next * 2 + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    public sealed class PooledVfx : MonoBehaviour
    {
        private MeshFilter filter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock block;
        private Vector3 from;
        private Vector3 to;
        private float duration;
        private float elapsed;
        private bool projectile;
        private float endScale;
        public bool IsPlaying { get; private set; }
        public DefenderVfxPriority Priority { get; private set; }
        public string ConcurrencyGroup { get; private set; }
        public float StartedAt { get; private set; }

        public void Prewarm()
        {
            filter ??= gameObject.AddComponent<MeshFilter>();
            meshRenderer ??= gameObject.AddComponent<MeshRenderer>();
            block ??= new MaterialPropertyBlock();
            IsPlaying = false;
            gameObject.SetActive(false);
        }

        public void Play(Mesh mesh, Material material, Vector3 start, Vector3 end, Color color, float seconds,
            bool moving, float targetScale, DefenderVfxPriority priority, string concurrencyGroup)
        {
            Prewarm();
            filter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
            from = start;
            to = end;
            duration = Mathf.Max(0.05f, seconds);
            elapsed = 0f;
            projectile = moving;
            endScale = targetScale;
            Priority = priority;
            ConcurrencyGroup = concurrencyGroup;
            StartedAt = Time.realtimeSinceStartup;
            IsPlaying = true;
            gameObject.SetActive(true);
            transform.position = from;
            transform.localScale = Vector3.one * (moving ? 0.7f : 0.15f);
        }

        private void Update()
        {
            if (!IsPlaying) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (projectile)
            {
                transform.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                transform.Rotate(210f * Time.deltaTime, 290f * Time.deltaTime, 0f);
            }
            else
            {
                transform.position = to;
                transform.localScale = Vector3.one * Mathf.Lerp(0.12f, endScale, t);
                transform.Rotate(0f, 220f * Time.deltaTime, 0f);
            }
            if (t < 1f) return;
            IsPlaying = false;
            gameObject.SetActive(false);
        }
    }
}
