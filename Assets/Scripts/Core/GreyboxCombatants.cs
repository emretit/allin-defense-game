using System.Collections.Generic;
using DefenderChronicles.Data;
using DefenderChronicles.Combat;
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
        private PooledActorVisualState visualState;
        private Transform healthFill;
        private int targetWaypoint = 1;
        private int health;
        private float attackTimer;
        private float supportTimer;
        private float siegeTimer;
        private float regenerationTimer;
        private float stunRemaining;
        private readonly SlowEffect slow = new();
        private float siegeWindup;
        private GreyboxOutpost siegeTarget;
        private CombatRangeVisual siegeWarning;
        public float StunRemaining => stunRemaining;
        public float SlowFactor => slow.Factor;
        public int Health => health;
        public bool IsWindingUp => siegeTarget != null && siegeWindup > 0f;
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
            targetWaypoint = 1;
            health = Mathf.Max(1, stats.maxHealth);
            attackTimer = supportTimer = siegeTimer = regenerationTimer = 0f;
            stunRemaining = siegeWindup = 0f;
            slow.Reset();
            siegeTarget = null;
            removed = false;
            bossPhase = 1;
            if (healthFill == null) CreateHealthBar();
            if (siegeWarning == null && type is EnemyArchetype.Siege or EnemyArchetype.Boss)
                siegeWarning = CombatRangeVisual.Create(transform, "Siege warning", new Color(1,.15f,.05f));
            if (visualState == null) visualState = new PooledActorVisualState(gameObject);
            else visualState.Reset();
            bodyRenderer = GetComponentInChildren<Renderer>();
            UpdateHealthBar();
            if (siegeWarning != null) siegeWarning.Show(Position, 1f, false);
            var phaseVisual = GetComponent<BossPhaseVisual>();
            if (phaseVisual != null) phaseVisual.SetPhase(1);
            var view = GetComponent<StylizedActorView>();
            if (view != null) view.ResetForReuse();
            State = EnemyCombatState.Moving;
        }

        internal void Tick(float delta)
        {
            if (!IsAlive || session == null || path == null || path.Count < 2) return;

            slow.Tick(delta);
            if (stunRemaining > 0f)
            {
                stunRemaining = Mathf.Max(0f, stunRemaining - delta);
                State = EnemyCombatState.Blocked;
                return;
            }
            attackTimer = CombatMath.TickCooldown(attackTimer, delta);
            supportTimer -= delta;
            siegeTimer -= delta;
            regenerationTimer -= delta;
            RunSpecialBehaviour(delta);
            if (siegeWarning != null) siegeWarning.Show(siegeTarget != null ? siegeTarget.Position : Position, 1.3f, IsWindingUp);

            IDamageable blocker = session.FindNearestBlocker(transform.position, AttackRange());
            if (blocker != null && blocker.IsAlive)
            {
                State = EnemyCombatState.Attacking;
                if (attackTimer <= 0f)
                {
                    blocker.TakeDamage(stats.attackDamage);
                    session.EmitPresentation(PresentationCue.OutpostAttacked, blocker.Position, gameObject,
                        GameSessionController.ActorKind(archetype), OutpostKind.Barracks);
                    attackTimer = BattleRules.LegacySeconds(archetype == EnemyArchetype.Boss ? .75f : 1.05f);
                }
                return;
            }

            State = EnemyCombatState.Moving;
            MoveAlongRoute(delta);
        }

        private void RunSpecialBehaviour(float delta)
        {
            if (archetype == EnemyArchetype.Regenerator && regenerationTimer <= 0f)
            {
                Heal(Mathf.Max(5, stats.maxHealth / 25));
                session.EmitPresentation(PresentationCue.HealPulse, transform.position, gameObject,
                    GameSessionController.ActorKind(archetype));
                regenerationTimer += .625f;
            }

            if (archetype == EnemyArchetype.Support && supportTimer <= 0f)
            {
                Vector3 healTargetPosition = transform.position;
                if (session.HealEnemies(transform.position, 3.5f, 24, this, out healTargetPosition))
                {
                    session.EmitPresentation(PresentationCue.HealPulse, healTargetPosition, gameObject,
                        GameSessionController.ActorKind(archetype));
                }
                supportTimer += 1.2f;
            }

            if (siegeTarget != null)
            {
                siegeWindup -= delta;
                if (siegeWindup <= 0f)
                {
                    if (siegeTarget.IsAlive)
                    {
                        siegeTarget.DisableTemporarily(archetype == EnemyArchetype.Boss ? 2.5f : 1.5f);
                        session.EmitPresentation(PresentationCue.OutpostDisrupted, siegeTarget.Position, gameObject,
                            GameSessionController.ActorKind(archetype));
                    }
                    siegeTarget = null;
                    siegeTimer = archetype == EnemyArchetype.Boss ? 4f : 3f;
                }
            }
            else if ((archetype == EnemyArchetype.Siege || archetype == EnemyArchetype.Boss) && siegeTimer <= 0f)
            {
                siegeTarget = session.FindNearestOutpost(transform.position,
                    archetype == EnemyArchetype.Boss ? 6.5f : 4.5f);
                if (siegeTarget != null)
                {
                    siegeWindup = 1.2f;
                    session.EmitPresentation(PresentationCue.BossAttack, siegeTarget.Position, gameObject,
                        GameSessionController.ActorKind(archetype));
                }
            }

            if (archetype != EnemyArchetype.Boss) return;
            float fraction = health / (float)Mathf.Max(1, stats.maxHealth);
            int nextPhase = fraction <= 0.33f ? 3 : fraction <= 0.66f ? 2 : 1;
            while (bossPhase < nextPhase)
            {
                bossPhase++;
                if (bossPhase == 2) session.SpawnBossReinforcements();
                var phaseVisual = GetComponent<BossPhaseVisual>();
                if (phaseVisual != null) phaseVisual.SetPhase(bossPhase);
                session.EmitPresentation(PresentationCue.BossPhase, transform.position, gameObject,
                    PresentationActorKind.Boss, value: bossPhase);
            }
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
            float phaseMultiplier = archetype == EnemyArchetype.Boss && bossPhase == 3 ? 1.44f : 1f;
            transform.position = Vector3.MoveTowards(current, target,
                stats.speed * phaseMultiplier * slow.Factor * delta);

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
            session.ReturnEnemy(this);
        }

        public void TakeDamage(int amount) => TakeDamage(amount, DamageType.Physical);

        public void TakeDamage(int amount, DamageType type)
        {
            if (!IsAlive || amount <= 0) return;
            int resolved = BattleRules.ResolveDamage(amount, type, archetype == EnemyArchetype.Vanguard ? .5f : 0f);
            health = CombatMath.ApplyDamage(health, resolved);
            UpdateHealthBar();
            session?.EmitPresentation(PresentationCue.EnemyHit, transform.position, gameObject,
                GameSessionController.ActorKind(archetype));
            if (health > 0) return;
            removed = true;
            State = EnemyCombatState.Dead;
            session.EnemyRemoved(this, true);
            session.ReturnEnemy(this);
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            health = Mathf.Min(stats.maxHealth, health + amount);
            UpdateHealthBar();
        }

        public void ApplySlow(float factor, float duration)
        {
            slow.Apply(archetype == EnemyArchetype.Boss ? Mathf.Max(.75f, factor) : factor, duration);
        }

        public void Stun(float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            stunRemaining = Mathf.Max(stunRemaining, duration);
            siegeTarget = null;
            if (siegeWarning != null) siegeWarning.Show(Position,1,false);
            siegeWindup = 0f;
            siegeTimer = archetype == EnemyArchetype.Boss ? 4f : 3f;
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
                fillObject.GetComponent<Image>().color = archetype == EnemyArchetype.Boss
                    ? new Color(1f,.5f,.15f) : new Color(.95f,.22f,.25f);
                if (archetype == EnemyArchetype.Vanguard) ShieldStatusIcon.Attach(canvasObject.transform);
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
        public OutpostSpecialization Specialization { get; private set; }
        public TowerCombatStats CombatStats { get; private set; }
        public bool CanSpecialize => IsAlive && level == 2 && Specialization == OutpostSpecialization.None;
        private readonly List<GreyboxFriendlyUnit> soldiers = new();
        public IReadOnlyList<GreyboxFriendlyUnit> Soldiers => soldiers;
        private GameSessionController session;
        private OutpostStats baseStats;
        private Renderer bodyRenderer;
        private Color activeColor;
        private MaterialPropertyBlock colorBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private int colorPropertyId;
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
            CombatStats = TowerProgression.Stats(kind, 1);
            invested = stats.cost;
            bodyRenderer = GetComponentInChildren<Renderer>();
            CacheActiveColor();
            if (Kind == OutpostKind.Barracks) SpawnSoldiers();
        }

        internal void Tick(float delta)
        {
            if (!IsAlive || session == null) return;
            attackTimer = CombatMath.TickCooldown(attackTimer, delta);
            if (disabledRemaining > 0f)
            {
                disabledRemaining = Mathf.Max(0f, disabledRemaining - delta);
                if (disabledRemaining <= 0f) SetBodyColor(activeColor);
                return;
            }

            if (Kind == OutpostKind.Barracks)
            {
                soldiers.RemoveAll(unit => unit == null || !unit.IsAlive);
                if (soldiers.Count < CombatStats.guardCount && attackTimer <= 0f)
                {
                    for (int index = 0; index < CombatStats.guardCount; index++)
                    {
                        if (soldiers.Exists(unit => unit.FormationIndex == index)) continue;
                        SpawnSoldier(index);
                        break;
                    }
                    attackTimer = 2.25f;
                }
                return;
            }

            if (attackTimer > 0f) return;
            GreyboxEnemy target = CombatStats.priorityTargets
                ? session.FindPriorityEnemy(transform.position, CurrentRange())
                : session.FindFirstEnemy(transform.position, CurrentRange());
            if (target == null) return;
            if (Kind == OutpostKind.Mage)
            {
                session.DamageEnemies(target.Position, CombatStats.splashRadius, CurrentDamage(),
                    CombatStats.slowFactor, CombatStats.slowSeconds, CombatStats.damageType);
                session.EmitPresentation(PresentationCue.MageImpact, target.Position, gameObject, outpost: Kind);
            }
            else
            {
                target.TakeDamage(CurrentDamage(), CombatStats.damageType);
                session.EmitPresentation(PresentationCue.OutpostAttacked, target.Position, gameObject, outpost: Kind);
            }
            attackTimer = 1f / Mathf.Max(0.05f, CurrentRate());
        }

        internal SavedOutpost CaptureCheckpoint() => new SavedOutpost
        {
            slot = Slot.Index, kind = Kind, level = level, specialization = Specialization,
            attack = attackTimer, disabled = disabledRemaining,
            guards = soldiers.FindAll(g => g != null && g.IsAlive).ConvertAll(g => g.CaptureCheckpoint())
        };
        internal void RestoreCheckpoint(SavedOutpost data)
        {
            attackTimer = data.attack; disabledRemaining = data.disabled;
            foreach (var soldier in soldiers) if (soldier != null) soldier.Dismiss();
            soldiers.Clear();
            for (int i = 0; i < data.guards.Count; i++)
            {
                var saved = data.guards[i];
                SpawnSoldier(saved.hasPosition ? saved.formationIndex : i)?.RestoreCheckpoint(saved);
            }
        }

        public void Upgrade()
        {
            if (!CanUpgrade) return;
            level = 2;
            CombatStats = TowerProgression.Stats(Kind, level);
            invested += UpgradeCost;
            if (session == null || !session.HasProductionPresentation) transform.localScale *= 1.15f;
            foreach (var soldier in soldiers)
                if (soldier != null) soldier.SetUpgradeLevel(level);
        }

        public bool Specialize(OutpostSpecialization branch)
        {
            if (!CanSpecialize || !TowerProgression.Matches(Kind, branch)) return false;
            Specialization = branch;
            level = 3;
            invested += TowerProgression.Cost(Kind);
            CombatStats = TowerProgression.Stats(Kind, level, branch);
            foreach (var soldier in soldiers) if (soldier != null) soldier.SetUpgradeLevel(level);
            return true;
        }

        internal void RefreshPresentationRenderer()
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
            CacheActiveColor();
        }

        private void CacheActiveColor()
        {
            if (colorBlock == null) colorBlock = new MaterialPropertyBlock();
            Material material = bodyRenderer != null ? bodyRenderer.sharedMaterial : null;
            colorPropertyId = material != null && material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            activeColor = material != null && material.HasProperty(colorPropertyId)
                ? material.GetColor(colorPropertyId) : Color.white;
        }

        private void SetBodyColor(Color color)
        {
            if (bodyRenderer == null) return;
            bodyRenderer.GetPropertyBlock(colorBlock, 0);
            if (colorBlock.isEmpty) bodyRenderer.GetPropertyBlock(colorBlock);
            colorBlock.SetColor(colorPropertyId, color);
            bodyRenderer.SetPropertyBlock(colorBlock, 0);
        }

        public void DisableTemporarily(float duration)
        {
            if (!IsAlive) return;
            disabledRemaining = Mathf.Max(disabledRemaining, duration);
            SetBodyColor(new Color(0.25f, 0.25f, 0.25f));
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
                if (soldier != null) soldier.Dismiss();
            soldiers.Clear();
            Destroy(gameObject);
        }

        private void SpawnSoldiers()
        {
            for (int i = 0; i < CombatStats.guardCount; i++) SpawnSoldier(i);
        }

        private GreyboxFriendlyUnit SpawnSoldier(int index)
        {
            if (!IsAlive || session == null || soldiers.Count >= CombatStats.guardCount) return null;
            Vector3 rally = FindBarracksRallyPoint(index);
            Vector3 direction = rally - Position;
            direction.y = 0;
            direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector3.forward;
            Vector3 exit = Position + direction * .7f + Vector3.Cross(Vector3.up, direction) * ((index - 1) * .3f);
            GameObject go = session.CreateFriendlyVisual($"Barracks Guard {index + 1}", exit);
            if (go == null)
            {
                go = new GameObject($"Barracks Guard {index + 1}");
                go.transform.SetParent(transform.parent, true);
                go.transform.position = exit;
                GameSessionController.CreatePrimitive("Guard body", PrimitiveType.Capsule,
                    exit + Vector3.up * .55f, new Vector3(0.42f, 0.55f, 0.42f),
                    new Color(0.72f, 0.76f, 0.82f), go.transform, true);
            }
            var soldier = go.AddComponent<GreyboxFriendlyUnit>();
            soldier.Configure(session, this, rally, level, index);
            soldiers.Add(soldier);
            session.RegisterFriendly(soldier);
            return soldier;
        }

        private Vector3 FindBarracksRallyPoint(int index)
        {
            IReadOnlyList<Vector3> route = session.Path;
            float progress = CombatMath.ClosestRouteDistance(route, Position);
            float roadLength = 0;
            for (int i = 0; i + 1 < route.Count; i++) roadLength += Vector3.Distance(route[i], route[i + 1]);
            // Form up beyond the building so the middle soldier is not hidden behind its model.
            float spacing = Mathf.Min(.8f, roadLength * .5f);
            float centre = Mathf.Clamp(progress + 1.6f, spacing, roadLength - spacing);
            // Sample along the actual road so short segments and corners never throw a guard off it.
            Vector3 closest = CombatMath.PointOnRoute(route, centre + (index - 1) * spacing, out _);
            closest.y = transform.position.y;
            return closest;
        }

        private int CurrentDamage() => CombatStats.damage;
        private float CurrentRange() => CombatStats.range;
        private float CurrentRate() => CombatStats.attacksPerSecond;
        private string RoleName() => Specialization != OutpostSpecialization.None ? TowerProgression.Description(Specialization) : Kind switch
        {
            OutpostKind.Archer => GameLocalization.Text("Hızlı tek hedef", "Fast single target"),
            OutpostKind.Barracks => GameLocalization.Text("Üç asker · Yakın dövüş", "Three melee soldiers"),
            OutpostKind.Mage => GameLocalization.Text("Alan hasarı", "Area damage"),
            _ => GameLocalization.Text("Savunma", "Defence")
        };
    }

    public sealed class GreyboxFriendlyUnit : MonoBehaviour, ICombatTarget
    {
        private const float MoveSpeed = 2.8f;
        private const float PatrolRange = 2.6f;

        private GameSessionController session;
        private GreyboxOutpost owner;
        private Vector3 rally;
        private int health;
        private int maxHealth;
        private int damage;
        private float attackTimer;
        private bool dead;
        private float rallyProgress, routeProgress;
        private GreyboxEnemy target;

        public bool IsAlive => !dead && health > 0 && owner != null && owner.IsAlive;
        public Vector3 Position => transform.position;
        public int Health => health;
        public int FormationIndex { get; private set; }
        public Vector3 RallyPoint => rally;
        public bool IsDeployed { get; private set; }
        public bool CanBlock => IsAlive && IsDeployed;

        public void Configure(GameSessionController gameSession, GreyboxOutpost source, Vector3 rallyPoint, int level, int formationIndex = 0)
        {
            session = gameSession;
            owner = source;
            rally = rallyPoint;
            FormationIndex = formationIndex;
            rallyProgress = CombatMath.ClosestRouteDistance(session.Path, rally);
            SetUpgradeLevel(level);
        }

        public void SetUpgradeLevel(int level)
        {
            maxHealth = owner.CombatStats.guardHealth;
            health = Mathf.Max(health, maxHealth);
            damage = owner.CombatStats.damage;
        }

        internal ActorCheckpoint CaptureCheckpoint() => new ActorCheckpoint
        {
            health = health, attack = attackTimer, hasPosition = true, position = Position,
            deployed = IsDeployed, formationIndex = FormationIndex
        };
        internal void RestoreCheckpoint(ActorCheckpoint data)
        {
            health = data.health; attackTimer = Mathf.Max(0, data.attack);
            transform.position = data.hasPosition ? data.position : rally;
            IsDeployed = !data.hasPosition || data.deployed;
            routeProgress = CombatMath.ClosestRouteDistance(session.Path, Position);
        }
        internal void Tick(float delta)
        {
            if (!IsAlive || session == null) return;
            attackTimer = CombatMath.TickCooldown(attackTimer, delta);
            if (owner.IsDisrupted) return;
            if (!IsDeployed)
            {
                MoveTo(rally, delta);
                if ((Position - rally).sqrMagnitude < .001f)
                {
                    IsDeployed = true;
                    routeProgress = rallyProgress;
                }
                return;
            }
            if (target == null || !target.IsAlive || Mathf.Abs(CombatMath.ClosestRouteDistance(session.Path, target.Position) - rallyProgress) > PatrolRange + .5f)
                target = FindTarget();

            float destination = rallyProgress;
            float lane = 0f;
            if (target != null)
            {
                float enemyProgress = CombatMath.ClosestRouteDistance(session.Path, target.Position);
                destination = enemyProgress + (rallyProgress < enemyProgress ? -.55f : .55f);
                lane = (FormationIndex - 1) * .32f;
            }
            routeProgress = Mathf.MoveTowards(routeProgress, destination, MoveSpeed * delta);
            Vector3 roadPosition = CombatMath.PointOnRoute(session.Path, routeProgress, out var direction);
            roadPosition += Vector3.Cross(Vector3.up, direction) * lane;
            roadPosition.y = Position.y;
            MoveTo(roadPosition, delta);

            Vector3 toTarget = target != null ? target.Position - Position : Vector3.zero;
            toTarget.y = 0; // Combat takes place on the road; a model's pivot height is only presentation.
            if (target != null && toTarget.sqrMagnitude <= owner.CombatStats.range * owner.CombatStats.range)
            {
                Face(target.Position - Position);
                if (attackTimer <= 0)
                {
                    target.TakeDamage(damage);
                    session.EmitPresentation(PresentationCue.OutpostAttacked, target.Position, gameObject,
                        PresentationActorKind.Guard, owner.Kind);
                    attackTimer = 1f / owner.CombatStats.attacksPerSecond;
                }
            }
        }

        internal void ForgetTarget(GreyboxEnemy enemy)
        {
            if (target == enemy) target = null;
        }

        private GreyboxEnemy FindTarget()
        {
            GreyboxEnemy best = null;
            float bestScore = float.MaxValue;
            foreach (var enemy in session.Enemies)
            {
                if (enemy == null || !enemy.IsAlive || (enemy.Position - rally).sqrMagnitude > PatrolRange * PatrolRange) continue;
                if (Mathf.Abs(CombatMath.ClosestRouteDistance(session.Path, enemy.Position) - rallyProgress) > PatrolRange) continue;
                float score = (enemy.Position - Position).sqrMagnitude;
                foreach (var guard in owner.Soldiers)
                    if (guard != null && guard != this && guard.IsAlive && guard.target == enemy) { score += 100; break; }
                if (score >= bestScore) continue;
                bestScore = score; best = enemy;
            }
            return best;
        }

        private void MoveTo(Vector3 point, float delta)
        {
            point.y = Position.y;
            Face(point - Position);
            transform.position = Vector3.MoveTowards(Position, point, MoveSpeed * delta);
        }

        private void Face(Vector3 direction)
        {
            direction.y = 0;
            if (direction.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(direction);
        }

        internal void Dismiss()
        {
            dead = true;
            Destroy(gameObject);
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
        private const float RespawnSeconds = 5f;
        private const float AbilityCooldown = 6f;
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

        internal ActorCheckpoint CaptureCheckpoint() => new ActorCheckpoint
        { health = health, attack = attackTimer, ability = AbilityRemaining, respawn = RespawnRemaining, downed = downed };
        internal void RestoreCheckpoint(ActorCheckpoint data)
        {
            health = data.health; attackTimer = data.attack; AbilityRemaining = data.ability;
            RespawnRemaining = data.respawn; downed = data.downed;
            SetVisuals(!downed); if (bodyCollider != null) bodyCollider.enabled = !downed;
        }
        internal void Tick(float delta)
        {
            AbilityRemaining = CombatMath.TickCooldown(AbilityRemaining, delta);
            if (downed)
            {
                RespawnRemaining = Mathf.Max(0f, RespawnRemaining - delta);
                if (RespawnRemaining <= 0f) Respawn();
                return;
            }

            attackTimer = CombatMath.TickCooldown(attackTimer, delta);
            GreyboxEnemy enemy = session == null ? null : session.FindFirstEnemy(transform.position, 1.65f);
            if (enemy != null)
            {
                if (attackTimer <= 0f)
                {
                    enemy.TakeDamage(20);
                    session.EmitPresentation(PresentationCue.HeroAttacked, enemy.Position, gameObject,
                        PresentationActorKind.Knight);
                    attackTimer = .35f;
                }
                return;
            }

            // The commander is a stationary keep defender. Player strategy comes from placing
            // structures, not steering a character around the battlefield.
        }

        public bool TryUseShieldBash()
        {
            if (!IsAlive || AbilityRemaining > 0f || session == null || session.Phase is GamePhase.Paused or GamePhase.Victory or GamePhase.Defeat) return false;
            int targets = session.CountEnemies(transform.position, 3.2f);
            if (targets == 0) return false;
            session.RecordAbility(targets);
            session.DamageEnemies(transform.position, 3.2f, 90);
            foreach (var enemy in session.Enemies)
            {
                if (enemy != null && enemy.IsAlive &&
                    (enemy.Position - transform.position).sqrMagnitude <= 3.2f * 3.2f)
                    enemy.Stun(1f);
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
