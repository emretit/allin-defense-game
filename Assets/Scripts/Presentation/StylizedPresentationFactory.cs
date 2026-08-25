using System.Collections;
using System.Linq;
using DefenderChronicles.Core;
using DefenderChronicles.Data;
using UnityEngine;

namespace DefenderChronicles.Presentation
{
    public enum PresentationCollider
    {
        None,
        Capsule,
        Cylinder
    }

    public static class StylizedPresentationFactory
    {
        private static Mesh groundingRingMesh;
        private static Mesh contactShadowMesh;

        public static GameObject CreateRoot(string name, Vector3 position, Vector3 colliderSize,
            GameObject visualPrefab, DefenderPresentationCatalog catalog, PresentationCollider collider,
            float visualScale = 1f, RuntimeAnimatorController actorAnimator = null,
            PresentationActorKind actorKind = PresentationActorKind.Raider)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            switch (collider)
            {
                case PresentationCollider.Capsule:
                {
                    var body = root.AddComponent<CapsuleCollider>();
                    body.radius = Mathf.Max(colliderSize.x, colliderSize.z) * 0.5f;
                    body.height = Mathf.Max(body.radius * 2f, colliderSize.y);
                    body.center = Vector3.up * body.height * 0.5f;
                    break;
                }
                case PresentationCollider.Cylinder:
                {
                    var body = root.AddComponent<CapsuleCollider>();
                    body.radius = Mathf.Max(colliderSize.x, colliderSize.z) * 0.5f;
                    body.height = Mathf.Max(0.2f, colliderSize.y);
                    body.center = Vector3.up * body.height * 0.5f;
                    CreateOutpostRuntimeSockets(root.transform, name, colliderSize);
                    break;
                }
            }

            if (visualPrefab != null)
            {
                var motion = new GameObject("Motion");
                motion.transform.SetParent(root.transform, false);
                var visual = Object.Instantiate(visualPrefab, motion.transform);
                visual.name = "Art";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * visualScale;
                ApplyPalette(visual, catalog, true);
                if (collider == PresentationCollider.Capsule)
                {
                    root.AddComponent<StylizedActorView>().Configure(motion.transform, catalog, actorAnimator,
                        actorKind);
                    if (actorKind == PresentationActorKind.Boss)
                        root.AddComponent<BossPhaseVisual>().Configure(visual.transform, catalog);
                    CreateGroundingAccent(root.transform, name, colliderSize.x, catalog);
                }
            }
            return root;
        }

        public static string[] RuntimeSocketNamesForOutpost(OutpostKind kind)
        {
            string[] common =
            {
                "Socket_Root", "Socket_Ground", "Socket_Target", "Socket_Projectile",
                "Socket_VFX", "Socket_Banner", "Socket_Upgrade"
            };
            string[] role = kind switch
            {
                OutpostKind.Archer => new[] { "Socket_Muzzle" },
                OutpostKind.Barracks => new[] { "Socket_Spawn" },
                OutpostKind.Mage => new[] { "Socket_Crystal", "Socket_Cast" },
                _ => System.Array.Empty<string>()
            };
            return common.Concat(role).Distinct().ToArray();
        }

        private static void CreateOutpostRuntimeSockets(Transform root, string identity, Vector3 colliderSize)
        {
            OutpostKind kind = OutpostKind.Archer;
            string value = identity?.ToLowerInvariant() ?? string.Empty;
            if (value.Contains("barracks")) kind = OutpostKind.Barracks;
            else if (value.Contains("mage")) kind = OutpostKind.Mage;

            float radius = Mathf.Max(colliderSize.x, colliderSize.z) * 0.5f;
            float height = Mathf.Max(0.2f, colliderSize.y);
            foreach (string socketName in RuntimeSocketNamesForOutpost(kind))
                CreateSocket(root, socketName, OutpostSocketPosition(socketName, radius, height));
        }

        private static Vector3 OutpostSocketPosition(string socketName, float radius, float height)
        {
            return socketName switch
            {
                "Socket_Ground" => Vector3.up * 0.03f,
                "Socket_Target" => Vector3.up * (height * 0.65f),
                "Socket_Projectile" => new Vector3(0f, height * 0.84f, radius * 0.38f),
                "Socket_VFX" => Vector3.up * 0.08f,
                "Socket_Banner" => new Vector3(radius * 0.35f, height * 1.08f, 0f),
                "Socket_Upgrade" => Vector3.up * (height * 1.12f),
                "Socket_Muzzle" => new Vector3(0f, height * 0.92f, radius * 0.46f),
                "Socket_Spawn" => new Vector3(0f, 0.05f, -radius * 0.45f),
                "Socket_Crystal" => Vector3.up * (height * 1.04f),
                "Socket_Cast" => new Vector3(0f, height * 0.95f, radius * 0.18f),
                _ => Vector3.zero
            };
        }

        private static void CreateSocket(Transform root, string name, Vector3 localPosition)
        {
            var socket = new GameObject(name);
            socket.transform.SetParent(root, false);
            socket.transform.localPosition = localPosition;
        }

        private static void CreateGroundingAccent(Transform root, string identity, float actorWidth,
            DefenderPresentationCatalog catalog)
        {
            string value = identity?.ToLowerInvariant() ?? string.Empty;
            bool boss = value.Contains("boss");
            bool defender = value.Contains("knight") || value.Contains("guard");
            CreateContactShadow(root, actorWidth, catalog, boss);
            if (!boss && !defender) return;

            var accent = new GameObject(boss ? "Boss Ground Aura" : "Defender Ground Aura");
            accent.transform.SetParent(root, false);
            accent.transform.localPosition = Vector3.up * 0.045f;
            float radius = Mathf.Max(0.42f, actorWidth * (boss ? 0.76f : 0.64f));
            accent.transform.localScale = Vector3.one * radius;
            var filter = accent.AddComponent<MeshFilter>();
            groundingRingMesh ??= BuildGroundingRing(28);
            filter.sharedMesh = groundingRingMesh;
            var renderer = accent.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = boss ? catalog.fireMaterial : catalog.defenderMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            accent.AddComponent<StylizedGroundingAura>().Configure(radius, boss);
        }

        private static void CreateContactShadow(Transform root, float actorWidth, DefenderPresentationCatalog catalog,
            bool boss)
        {
            var shadow = new GameObject("Actor Contact Shadow");
            shadow.transform.SetParent(root, false);
            shadow.transform.localPosition = Vector3.up * 0.022f;
            float radius = Mathf.Max(boss ? 0.58f : 0.36f, actorWidth * (boss ? 0.88f : 0.62f));
            shadow.transform.localScale = new Vector3(radius * 1.18f, 1f, radius * 0.74f);
            var filter = shadow.AddComponent<MeshFilter>();
            contactShadowMesh ??= BuildGroundingRing(28, 0.38f);
            filter.sharedMesh = contactShadowMesh;
            var renderer = shadow.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = boss ? catalog.darkMaterial : catalog.stoneMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Mesh BuildGroundingRing(int segments, float innerRadius = 0.79f)
        {
            var mesh = new Mesh { name = "DC Grounding Aura Ring" };
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * innerRadius;
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
            mesh.RecalculateBounds();
            return mesh;
        }

        public static GameObject SpawnModel(GameObject prefab, Transform parent, DefenderPresentationCatalog catalog,
            string name = null, bool limitToTwoMaterials = false)
        {
            if (prefab == null) return null;
            var instance = Object.Instantiate(prefab, parent);
            if (!string.IsNullOrWhiteSpace(name)) instance.name = name;
            ApplyPalette(instance, catalog, limitToTwoMaterials);
            return instance;
        }

        public static void ApplyPalette(GameObject instance, DefenderPresentationCatalog catalog, bool limitToTwoMaterials = false)
        {
            if (instance == null || catalog == null) return;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Material teamMaterial = null;
            if (limitToTwoMaterials)
            {
                string allNames = string.Join(" ", renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).Select(material => material.name.ToLowerInvariant()));
                string identity = instance.name.ToLowerInvariant();
                teamMaterial = identity.Contains("mage") || identity.Contains("support") ? catalog.magicMaterial
                    : identity.Contains("regenerator") ? catalog.forestMaterial
                    : identity.Contains("archer") || identity.Contains("barracks") || identity.Contains("knight") || identity.Contains("guard")
                        ? catalog.defenderMaterial
                    : identity.Contains("raider") || identity.Contains("vanguard") || identity.Contains("boss") || identity.Contains("siege")
                        ? catalog.enemyMaterial
                    : allNames.Contains("magic") ? catalog.magicMaterial
                    : allNames.Contains("forest") ? catalog.forestMaterial
                    : allNames.Contains("enemy") ? catalog.enemyMaterial
                    : allNames.Contains("defender") ? catalog.defenderMaterial
                    : allNames.Contains("fire") ? catalog.fireMaterial
                    : catalog.stoneMaterial;
            }
            foreach (var renderer in renderers)
            {
                if (renderer.name.Contains("BlobShadow", System.StringComparison.OrdinalIgnoreCase))
                {
                    renderer.enabled = false;
                    renderer.sharedMaterials = System.Array.Empty<Material>();
                    continue;
                }

                var source = renderer.sharedMaterials;
                var mapped = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    Material licensedAtlas = catalog.ResolveLicensedAtlasMaterial(source[i]);
                    if (licensedAtlas != null)
                    {
                        mapped[i] = licensedAtlas;
                        continue;
                    }
                    string sourceName = source[i] != null ? source[i].name : renderer.name;
                    bool neutral = sourceName.Contains("stone", System.StringComparison.OrdinalIgnoreCase) ||
                                   sourceName.Contains("dark", System.StringComparison.OrdinalIgnoreCase) ||
                                   sourceName.Contains("metal", System.StringComparison.OrdinalIgnoreCase);
                    mapped[i] = limitToTwoMaterials ? (neutral ? catalog.stoneMaterial : teamMaterial) :
                        catalog.ResolveMaterial(sourceName);
                }
                renderer.sharedMaterials = mapped;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }

    public sealed class StylizedGroundingAura : MonoBehaviour
    {
        private float baseRadius;
        private float speed;
        private float amplitude;
        private float phase;

        public void Configure(float radius, bool boss)
        {
            baseRadius = radius;
            speed = boss ? 2.1f : 1.25f;
            amplitude = boss ? 0.10f : 0.045f;
            phase = transform.parent != null ? transform.parent.GetInstanceID() * 0.17f : 0f;
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * speed + phase) * amplitude;
            transform.localScale = Vector3.one * baseRadius * pulse;
            transform.Rotate(0f, (speed * 14f) * Time.deltaTime, 0f, Space.Self);
        }
    }

    public sealed class BossPhaseVisual : MonoBehaviour
    {
        private GameObject[] phaseTwo = System.Array.Empty<GameObject>();
        private GameObject[] phaseThree = System.Array.Empty<GameObject>();

        public int CurrentPhase { get; private set; } = 1;

        public void Configure(Transform visualRoot, DefenderPresentationCatalog catalog)
        {
            if (visualRoot == null) return;
            phaseTwo = visualRoot.GetComponentsInChildren<Transform>(true)
                .Where(child => child != visualRoot && child.name.StartsWith("Phase2_",
                    System.StringComparison.Ordinal))
                .Select(child => child.gameObject).ToArray();
            phaseThree = visualRoot.GetComponentsInChildren<Transform>(true)
                .Where(child => child != visualRoot && child.name.StartsWith("Phase3_",
                    System.StringComparison.Ordinal))
                .Select(child => child.gameObject).ToArray();

            foreach (GameObject part in phaseTwo)
                SetPhaseMaterial(part, part.name.Contains("Core", System.StringComparison.Ordinal)
                    ? catalog.fireMaterial : catalog.enemyMaterial);
            foreach (GameObject part in phaseThree)
                SetPhaseMaterial(part, part.name.Contains("Core", System.StringComparison.Ordinal)
                    ? catalog.magicMaterial : catalog.darkMaterial);
            SetPhase(1);
        }

        public void SetPhase(int phase)
        {
            CurrentPhase = Mathf.Clamp(phase, 1, 3);
            foreach (GameObject part in phaseTwo)
                if (part != null) part.SetActive(CurrentPhase >= 2);
            foreach (GameObject part in phaseThree)
                if (part != null) part.SetActive(CurrentPhase >= 3);
        }

        private static void SetPhaseMaterial(GameObject part, Material material)
        {
            if (part == null || material == null) return;
            foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }
    }

    public sealed class StylizedActorView : MonoBehaviour
    {
        private Animator animator;
        private Vector3 previousPosition;

        public void Configure(Transform target, DefenderPresentationCatalog catalog,
            RuntimeAnimatorController actorAnimatorOverride = null,
            PresentationActorKind actorKind = PresentationActorKind.Raider)
        {
            animator = target.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = actorAnimatorOverride != null ? actorAnimatorOverride : catalog.actorAnimator;
            var relay = target.gameObject.AddComponent<PresentationAnimationEventRelay>();
            relay.Configure(gameObject, actorKind);
            previousPosition = transform.position;
        }

        public void Attack() => Trigger(PresentationActorAnimationContract.AttackState);
        public void Hit() => Trigger(PresentationActorAnimationContract.HitState);
        public void Disabled() => Trigger(PresentationActorAnimationContract.HitState);
        public void Ability() => Trigger(PresentationActorAnimationContract.AbilityState);
        public void Victory() => Trigger(PresentationActorAnimationContract.VictoryState);
        public void Phase() => Trigger(PresentationActorAnimationContract.PhaseState);
        public void BossAttack() => Trigger(PresentationActorAnimationContract.BossAttackState);

        private void Update()
        {
            if (animator == null) return;
            float moved = (transform.position - previousPosition).sqrMagnitude;
            previousPosition = transform.position;
            animator.SetBool(PresentationActorAnimationContract.MovingParameter, moved > 0.0001f);
        }

        private void Trigger(string parameter)
        {
            if (animator != null) animator.SetTrigger(parameter);
        }

        public static void PlayDefeatEcho(GameObject prefab, DefenderPresentationCatalog catalog, Vector3 position,
            float scale = 1f, RuntimeAnimatorController actorAnimatorOverride = null)
        {
            if (prefab == null) return;
            var echo = new GameObject("Defeat Echo");
            echo.name = "Defeat Echo";
            echo.transform.position = position;
            var visual = Object.Instantiate(prefab, echo.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;
            StylizedPresentationFactory.ApplyPalette(visual, catalog, true);
            var animator = echo.AddComponent<Animator>();
            animator.runtimeAnimatorController = actorAnimatorOverride != null ? actorAnimatorOverride : catalog.actorAnimator;
            animator.Play(PresentationActorAnimationContract.DefeatState, 0, 0f);
            var host = echo.AddComponent<DefeatEcho>();
            host.Begin();
        }
    }

    public sealed class DefeatEcho : MonoBehaviour
    {
        private float elapsed;

        public void Begin() => elapsed = 0f;

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.58f);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
