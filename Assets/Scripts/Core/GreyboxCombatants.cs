using System.Collections.Generic;
using DefenderChronicles.Data;
using DefenderChronicles.Presentation;
using DefenderChronicles.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DefenderChronicles.Core
{
    public enum EnemyCombatState
    {
        Spawn,
        Moving,
        Blocked,
        Attacking,
        Dead
    }

    public sealed class GreyboxEnemy : MonoBehaviour, ICombatTarget
    {
        private GameSessionController session;
        private EnemyArchetype archetype;
        private EnemyStats stats;
        private IReadOnlyList<Vector3> path;
        private Renderer bodyRenderer;
        private Transform healthFill;
        private int targetWaypoint = 1;
        private int health;
        private float attackTimer;
        private float supportTimer;
        private float siegeTimer;
        private float regenerationTimer;
        private float stunRemaining;
        private bool removed;
        private int bossPhase = 1;

        public EnemyCombatState State { get; private set; } = EnemyCombatState.Spawn;
        public int Reward => stats.reward;
        public EnemyArchetype Archetype => archetype;
        public float RouteProgress => session == null ? 0f : session.ComputeRouteProgress(path, transform.position, targetWaypoint);
        public float RemainingRouteDistance => session == null ? float.MaxValue : session.ComputeRemainingRouteDistance(path, transform.position, targetWaypoint);
        public bool IsAlive => !removed && health > 0;
        public Vector3 Position => transform.position;

        public void Configure(GameSessionController owner, EnemyArchetype type, EnemyStats definition,
            IReadOnlyList<Vector3> route)
        {
            session = owner;
            archetype = type;
            stats = definition;
            path = route;
            health = Mathf.Max(1, stats.maxHealth);
            bodyRenderer = GetComponentInChildren<Renderer>();
            CreateHealthBar();
            State = EnemyCombatState.Moving;
        }

        private void Update()
        {
            if (!IsAlive || session == null || path == null || path.Count < 2) return;

            float delta = session.GameplayDeltaTime;
            attackTimer -= delta;
            supportTimer -= delta;
            siegeTimer -= delta;
            regenerationTimer -= delta;
            stunRemaining = Mathf.Max(0f, stunRemaining - delta);
            RunSpecialBehaviour();
            if (stunRemaining > 0f)
            {
                State = EnemyCombatState.Blocked;
                return;
            }

            IDamageable blocker = session.FindNearestBlocker(transform.position, AttackRange());
            if (blocker != null && blocker.IsAlive)
            {
                State = EnemyCombatState.Attacking;
                if (attackTimer <= 0f)
                {
                    blocker.TakeDamage(stats.attackDamage);
                    attackTimer = archetype == EnemyArchetype.Boss ? 0.75f : 1.05f;
                }
                return;
            }

            State = EnemyCombatState.Moving;
            MoveAlongRoute(delta);
        }

        private void RunSpecialBehaviour()
        {
            if (archetype == EnemyArchetype.Regenerator && regenerationTimer <= 0f)
            {
                Heal(Mathf.Max(5, stats.maxHealth / 25));
                session.EmitPresentation(PresentationCue.HealPulse, transform.position, gameObject,
                    GameSessionController.ActorKind(archetype));
                regenerationTimer = 1.25f;
            }

            if (archetype == EnemyArchetype.Support && supportTimer <= 0f)
            {
                Vector3 healTargetPosition = transform.position;
                if (session.HealEnemies(transform.position, 3.5f, 24, this, out healTargetPosition))
                {
                    session.EmitPresentation(PresentationCue.HealPulse, healTargetPosition, gameObject,
                        GameSessionController.ActorKind(archetype));
                }
                supportTimer = 2.4f;
            }

            if ((archetype == EnemyArchetype.Siege || archetype == EnemyArchetype.Boss) && siegeTimer <= 0f)
            {
                GreyboxOutpost outpost = session.FindNearestOutpost(transform.position,
                    archetype == EnemyArchetype.Boss ? 6.5f : 4.5f);
                if (outpost != null)
                {
                    outpost.DisableTemporarily(archetype == EnemyArchetype.Boss ? 5f : 3f);
                    session.EmitPresentation(archetype == EnemyArchetype.Boss ? PresentationCue.BossAttack : PresentationCue.OutpostDisrupted,
                        outpost.Position, gameObject, GameSessionController.ActorKind(archetype));
                }
                siegeTimer = archetype == EnemyArchetype.Boss ? 8f : 6f;
            }

            if (archetype != EnemyArchetype.Boss) return;
            float fraction = health / (float)Mathf.Max(1, stats.maxHealth);
            int nextPhase = fraction <= 0.33f ? 3 : fraction <= 0.66f ? 2 : 1;
            if (nextPhase == bossPhase) return;
            bossPhase = nextPhase;
            GetComponent<BossPhaseVisual>()?.SetPhase(bossPhase);
            session.DamageEnemies(transform.position, 3.5f, 0);
            session.EmitPresentation(PresentationCue.BossPhase, transform.position, gameObject,
                PresentationActorKind.Boss, value: bossPhase);
        }

        private void MoveAlongRoute(float delta)
        {
            if (targetWaypoint >= path.Count)
            {
                ReachKeep();
                return;
            }

            Vector3 target = path[targetWaypoint];
            Vector3 current = transform.position;
            target.y = current.y;
            float phaseMultiplier = archetype == EnemyArchetype.Boss ? 1f + (bossPhase - 1) * 0.22f : 1f;
            transform.position = Vector3.MoveTowards(current, target,
                stats.speed * phaseMultiplier * delta);

            Vector3 direction = target - current;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction.normalized), delta * 8f);

            if ((transform.position - target).sqrMagnitude <= 0.025f)
            {
                targetWaypoint++;
                if (targetWaypoint >= path.Count) ReachKeep();
            }
        }

        private float AttackRange() => archetype == EnemyArchetype.Boss ? 1.8f : 1.2f;

        private void ReachKeep()
        {
            if (removed) return;
            removed = true;
            State = EnemyCombatState.Dead;
            session.EnemyReachedKeep(this, stats.baseDamage);
            Destroy(gameObject);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            health = CombatMath.ApplyDamage(health, amount);
            UpdateHealthBar();
            session?.EmitPresentation(PresentationCue.EnemyHit, transform.position, gameObject,
                GameSessionController.ActorKind(archetype));
            if (health > 0) return;
            removed = true;
            State = EnemyCombatState.Dead;
            session.EnemyRemoved(this, true);
            Destroy(gameObject);
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            health = Mathf.Min(stats.maxHealth, health + amount);
            UpdateHealthBar();
        }

        public void ApplySlow(float factor, float duration)
        {
            _ = factor;
            _ = duration;
        }

        public void Stun(float duration)
        {
            if (IsAlive) stunRemaining = Mathf.Max(stunRemaining, duration);
        }

        private void CreateHealthBar()
        {
            if (session != null && session.HasProductionPresentation)
            {
                var canvasObject = new GameObject("Health Bar", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(transform, false);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rect = canvasObject.GetComponent<RectTransform>();
                rect.localPosition = Vector3.up * (archetype == EnemyArchetype.Boss ? 2.8f : 1.65f);
                rect.sizeDelta = new Vector2(100f, 10f);
                rect.localScale = Vector3.one * 0.012f;
                canvasObject.AddComponent<WorldSpaceBillboard>();
                var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
                backgroundObject.transform.SetParent(canvasObject.transform, false);
                var backgroundRect = backgroundObject.GetComponent<RectTransform>();
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
                backgroundObject.GetComponent<Image>().color = new Color(0.05f, 0.02f, 0.03f, 0.9f);
                var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(backgroundObject.transform, false);
                var fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(2f, 2f);
                fillRect.offsetMax = new Vector2(-2f, -2f);
                fillObject.GetComponent<Image>().color = new Color(0.25f, 0.95f, 0.3f);
                healthFill = fillObject.transform;
                return;
            }
            var background = GameSessionController.CreatePrimitive("Health Background", PrimitiveType.Cube,
                transform.position + Vector3.up * 1.15f, new Vector3(0.95f, 0.08f, 0.08f),
                new Color(0.12f, 0.03f, 0.03f), transform, false);
            background.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            background.transform.localRotation = Quaternion.identity;
            background.transform.localScale = new Vector3(0.95f, 0.08f, 0.08f);

            var fill = GameSessionController.CreatePrimitive("Health Fill", PrimitiveType.Cube,
                background.transform.position, new Vector3(0.9f, 0.09f, 0.09f),
                new Color(0.25f, 0.95f, 0.30f), background.transform, false);
            healthFill = fill.transform;
            healthFill.localPosition = Vector3.zero;
            healthFill.localRotation = Quaternion.identity;
            healthFill.localScale = Vector3.one;
        }

        private void UpdateHealthBar()
        {
            if (healthFill == null) return;
            float fraction = Mathf.Clamp01(health / (float)Mathf.Max(1, stats.maxHealth));
            healthFill.localScale = new Vector3(fraction, 1f, 1f);
            healthFill.localPosition = new Vector3((fraction - 1f) * 0.5f, 0f, 0f);
        }
    }

    public sealed class GreyboxOutpost : MonoBehaviour, IDamageable
    {
        private readonly List<GreyboxFriendlyUnit> soldiers = new();
        private GameSessionController session;
        private OutpostStats baseStats;
        private Renderer bodyRenderer;
        private Color activeColor;
        private float attackTimer;
        private float disabledRemaining;
        private int level = 1;
        private int invested;
        private bool sold;

        public GreyboxOutpostSlot Slot { get; private set; }
        public OutpostKind Kind { get; private set; }
        public int Level => level;
        public bool IsAlive => !sold;
        public Vector3 Position => transform.position;
        public bool CanUpgrade => IsAlive && level < 2;
        public int UpgradeCost => baseStats.upgradeCost;
        public int SellValue => CombatMath.Refund(invested);
        public int DamageValue => CurrentDamage();
        public float RangeValue => CurrentRange();
        public float AttackRateValue => CurrentRate();
        public string RoleLabel => RoleName();
        public bool IsDisrupted => disabledRemaining > 0f;
        public float DisabledRemaining => disabledRemaining;
        public string Description => GameLocalization.IsEnglish
            ? $"{GameSessionController.OutpostDisplayName(Kind)}  •  LV {level}\n\n" +
              $"Role: {RoleName()}\nDamage: {CurrentDamage()}\nRate: {CurrentRate():0.0}/sec\n" +
              $"Range: {CurrentRange():0.0}\nUpgrade: {(CanUpgrade ? UpgradeCost.ToString() : "MAX")}\n" +
              $"Sell: {SellValue}" + (disabledRemaining > 0f ? "\n\nTEMPORARILY DISRUPTED" : string.Empty)
            : $"{GameSessionController.OutpostDisplayName(Kind)}  •  SV {level}\n\n" +
              $"Rol: {RoleName()}\nHasar: {CurrentDamage()}\nHız: {CurrentRate():0.0}/sn\n" +
              $"Menzil: {CurrentRange():0.0}\nYükseltme: {(CanUpgrade ? UpgradeCost.ToString() : "TAMAM")}\n" +
              $"Satış: {SellValue}" + (disabledRemaining > 0f ? "\n\nGEÇİCİ OLARAK BOZULDU" : string.Empty);

        public void Configure(GameSessionController owner, GreyboxOutpostSlot slot, OutpostKind kind,
            OutpostStats stats)
        {
            session = owner;
            Slot = slot;
            Kind = kind;
            baseStats = stats;
            invested = stats.cost;
            bodyRenderer = GetComponentInChildren<Renderer>();
            activeColor = bodyRenderer != null ? bodyRenderer.material.color : Color.white;
            if (Kind == OutpostKind.Barracks) SpawnSoldiers();
        }

        private void Update()
        {
            if (!IsAlive || session == null) return;
            float delta = session.GameplayDeltaTime;
            attackTimer -= delta;
            if (disabledRemaining > 0f)
            {
                disabledRemaining = Mathf.Max(0f, disabledRemaining - delta);
                if (disabledRemaining <= 0f && bodyRenderer != null) bodyRenderer.material.color = activeColor;
                return;
            }

            if (Kind == OutpostKind.Barracks)
            {
                soldiers.RemoveAll(unit => unit == null);
                if (soldiers.Count < 2 && attackTimer <= 0f)
                {
                    SpawnSoldier(soldiers.Count);
                    attackTimer = 4.5f;
                }
                return;
            }

            if (attackTimer > 0f) return;
            GreyboxEnemy target = session.FindFirstEnemy(transform.position, CurrentRange());
            if (target == null) return;
            if (Kind == OutpostKind.Mage)
            {
                session.DamageEnemies(target.Position, 1.65f, CurrentDamage());
                session.EmitPresentation(PresentationCue.MageImpact, target.Position, gameObject, outpost: Kind);
            }
            else
            {
                target.TakeDamage(CurrentDamage());
                session.EmitPresentation(PresentationCue.OutpostAttacked, target.Position, gameObject, outpost: Kind);
            }
            attackTimer = 1f / Mathf.Max(0.05f, CurrentRate());
        }

        public void Upgrade()
        {
            if (!CanUpgrade) return;
            level = 2;
            invested += UpgradeCost;
            if (session == null || !session.HasProductionPresentation) transform.localScale *= 1.15f;
            foreach (var soldier in soldiers)
                if (soldier != null) soldier.SetUpgradeLevel(level);
        }

        internal void RefreshPresentationRenderer()
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
            activeColor = bodyRenderer != null ? bodyRenderer.material.color : Color.white;
        }

        public void DisableTemporarily(float duration)
        {
            if (!IsAlive) return;
            disabledRemaining = Mathf.Max(disabledRemaining, duration);
            if (bodyRenderer != null) bodyRenderer.material.color = new Color(0.25f, 0.25f, 0.25f);
            session?.EmitPresentation(PresentationCue.OutpostDisrupted, transform.position, gameObject, outpost: Kind);
        }

        public void TakeDamage(int amount)
        {
            // Greybox siege attacks disrupt outposts instead of permanently destroying them.
            if (amount > 0) DisableTemporarily(Mathf.Clamp(amount * 0.1f, 1f, 5f));
        }

        public void Sell()
        {
            if (sold) return;
            sold = true;
            if (Slot != null) Slot.SetOccupant(null);
            foreach (var soldier in soldiers)
                if (soldier != null) Destroy(soldier.gameObject);
            soldiers.Clear();
            Destroy(gameObject);
        }

        private void SpawnSoldiers()
        {
            SpawnSoldier(0);
            SpawnSoldier(1);
        }

        private void SpawnSoldier(int index)
        {
            if (!IsAlive || session == null || soldiers.Count >= 2) return;
            Vector3 rally = FindBarracksRallyPoint(index);
            GameObject go = session.CreateFriendlyVisual($"Barracks Guard {index + 1}", rally);
            if (go == null)
                go = GameSessionController.CreatePrimitive($"Barracks Guard {index + 1}", PrimitiveType.Capsule,
                    rally + Vector3.up * 0.55f, new Vector3(0.42f, 0.55f, 0.42f),
                    new Color(0.72f, 0.76f, 0.82f), transform.parent, true);
            var soldier = go.AddComponent<GreyboxFriendlyUnit>();
            soldier.Configure(session, this, rally, level);
            soldiers.Add(soldier);
            session.RegisterFriendly(soldier);
        }

        private Vector3 FindBarracksRallyPoint(int index)
        {
            return session.BarracksRallyPoint(transform.position, index % 2 == 0 ? -0.45f : 0.45f);
        }

        private int CurrentDamage() => Mathf.RoundToInt(baseStats.damage * (level == 2 ? 1.55f : 1f));
        private float CurrentRange() => baseStats.range * (level == 2 ? 1.12f : 1f);
        private float CurrentRate() => baseStats.attacksPerSecond * (level == 2 ? 1.20f : 1f);
        private string RoleName() => Kind switch
        {
            OutpostKind.Archer => GameLocalization.Text("Hızlı tek hedef", "Fast single target"),
            OutpostKind.Barracks => GameLocalization.Text("İki engelleyici birlik", "Two blocking guards"),
            OutpostKind.Mage => GameLocalization.Text("Alan hasarı", "Area damage"),
            _ => GameLocalization.Text("Savunma", "Defence")
        };
    }

    public sealed class GreyboxFriendlyUnit : MonoBehaviour, ICombatTarget
    {
        private const int LevelOneHealth = 110;
        private const int LevelTwoHealth = 175;
        private const int LevelOneDamage = 16;
        private const int LevelTwoDamage = 26;
        private const float AttackInterval = 0.75f;

        private GameSessionController session;
        private GreyboxOutpost owner;
        private Vector3 rally;
        private int health;
        private int maxHealth;
        private int damage;
        private float attackTimer;
        private bool dead;

        public bool IsAlive => !dead && health > 0 && owner != null && owner.IsAlive;
        public Vector3 Position => transform.position;

        public void Configure(GameSessionController gameSession, GreyboxOutpost source, Vector3 rallyPoint, int level)
        {
            session = gameSession;
            owner = source;
            rally = rallyPoint;
            SetUpgradeLevel(level);
        }

        public void SetUpgradeLevel(int level)
        {
            maxHealth = level >= 2 ? LevelTwoHealth : LevelOneHealth;
            health = Mathf.Max(health, maxHealth);
            damage = level >= 2 ? LevelTwoDamage : LevelOneDamage;
        }

        private void Update()
        {
            if (!IsAlive || session == null) return;
            float delta = session.GameplayDeltaTime;
            attackTimer -= delta;
            GreyboxEnemy target = session.FindFirstEnemy(transform.position, 1.45f);
            if (target != null)
            {
                if (attackTimer <= 0f)
                {
                    target.TakeDamage(damage);
                    session.EmitPresentation(PresentationCue.OutpostAttacked, target.Position, gameObject,
                        PresentationActorKind.Guard, owner.Kind);
                    attackTimer = AttackInterval;
                }
                return;
            }

            Vector3 targetPosition = rally;
            targetPosition.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 2.8f * delta);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            health = CombatMath.ApplyDamage(health, amount);
            if (health > 0) return;
            dead = true;
            session?.EmitPresentation(PresentationCue.UnitDefeated, transform.position, gameObject,
                PresentationActorKind.Guard);
            Destroy(gameObject);
        }
    }

    public sealed class GreyboxHero : MonoBehaviour, ICombatTarget
    {
        private const int MaxHealth = 240;
        private const float RespawnSeconds = 10f;
        private const float AbilityCooldown = 12f;
        private GameSessionController session;
        private Vector3 guardianPosition;
        private Renderer bodyRenderer;
        private Collider bodyCollider;
        private int health;
        private float attackTimer;
        private bool downed;

        public bool IsAlive => !downed && health > 0;
        public Vector3 Position => transform.position;
        public float HealthFraction => Mathf.Clamp01(health / (float)MaxHealth);
        public float AbilityCooldownDuration => AbilityCooldown;
        public float AbilityRemaining { get; private set; }
        public float RespawnRemaining { get; private set; }

        public void Configure(GameSessionController owner, Vector3 initialGuardianPosition)
        {
            session = owner;
            guardianPosition = initialGuardianPosition;
            health = MaxHealth;
            bodyRenderer = GetComponentInChildren<Renderer>();
            bodyCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            float delta = session != null
                ? session.GameplayDeltaTime
                : Time.deltaTime * GameSessionController.BaselineGameplayPace;
            AbilityRemaining = CombatMath.TickCooldown(AbilityRemaining, delta);
            if (downed)
            {
                RespawnRemaining = Mathf.Max(0f, RespawnRemaining - delta);
                if (RespawnRemaining <= 0f) Respawn();
                return;
            }

            attackTimer -= delta;
            GreyboxEnemy enemy = session == null ? null : session.FindFirstEnemy(transform.position, 1.65f);
            if (enemy != null)
            {
                if (attackTimer <= 0f)
                {
                    enemy.TakeDamage(20);
                    session.EmitPresentation(PresentationCue.HeroAttacked, enemy.Position, gameObject,
                        PresentationActorKind.Knight);
                    attackTimer = 0.75f;
                }
                return;
            }

            // The commander is a stationary keep defender. Player strategy comes from placing
            // structures, not steering a character around the battlefield.
        }

        public bool TryUseShieldBash()
        {
            if (!IsAlive || AbilityRemaining > 0f || session == null) return false;
            session.DamageEnemies(transform.position, 3.2f, 90);
            foreach (var enemy in session.Enemies)
            {
                if (enemy != null && enemy.IsAlive &&
                    (enemy.Position - transform.position).sqrMagnitude <= 3.2f * 3.2f)
                    enemy.Stun(2f);
            }
            AbilityRemaining = AbilityCooldown;
            session.EmitPresentation(PresentationCue.HeroAbility, transform.position, gameObject,
                PresentationActorKind.Knight);
            StartCoroutine(AbilityPreview());
            return true;
        }

        private System.Collections.IEnumerator AbilityPreview()
        {
            if (session != null && session.HasProductionPresentation)
            {
                yield return new WaitForSeconds(GameSessionController.GameplayDelay(0.28f));
                yield break;
            }
            var preview = GameSessionController.CreatePrimitive("Shield Bash Range", PrimitiveType.Cylinder,
                new Vector3(transform.position.x, 0.08f, transform.position.z), new Vector3(3.2f, 0.03f, 3.2f),
                new Color(0.25f, 0.75f, 1f, 0.45f), transform.parent, false);
            yield return new WaitForSeconds(GameSessionController.GameplayDelay(0.28f));
            if (preview != null) Destroy(preview);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            health = CombatMath.ApplyDamage(health, amount);
            if (health > 0) return;
            downed = true;
            RespawnRemaining = RespawnSeconds;
            SetVisuals(false);
            if (bodyCollider != null) bodyCollider.enabled = false;
        }

        private void Respawn()
        {
            health = MaxHealth;
            downed = false;
            transform.position = session != null && session.HasProductionPresentation
                ? guardianPosition
                : guardianPosition + Vector3.up;
            SetVisuals(true);
            if (bodyCollider != null) bodyCollider.enabled = true;
        }

        private void SetVisuals(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = visible;
        }
    }

    public sealed class WorldSpaceBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            transform.rotation = camera.transform.rotation;
        }
    }
}
