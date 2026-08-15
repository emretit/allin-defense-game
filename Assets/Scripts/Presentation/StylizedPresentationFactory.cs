using System.Collections;
using System.Linq;
using DefenderChronicles.Core;
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
        public static GameObject CreateRoot(string name, Vector3 position, Vector3 colliderSize,
            GameObject visualPrefab, DefenderPresentationCatalog catalog, PresentationCollider collider,
            float visualScale = 1f)
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
                    root.AddComponent<StylizedActorView>().Configure(motion.transform, catalog);
            }
            return root;
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
                teamMaterial = allNames.Contains("magic") ? catalog.magicMaterial
                    : allNames.Contains("forest") ? catalog.forestMaterial
                    : allNames.Contains("enemy") ? catalog.enemyMaterial
                    : allNames.Contains("defender") ? catalog.defenderMaterial
                    : allNames.Contains("fire") ? catalog.fireMaterial
                    : catalog.stoneMaterial;
            }
            foreach (var renderer in renderers)
            {
                var source = renderer.sharedMaterials;
                var mapped = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    string sourceName = source[i] != null ? source[i].name : renderer.name;
                    bool neutral = sourceName.Contains("stone", System.StringComparison.OrdinalIgnoreCase) ||
                                   sourceName.Contains("dark", System.StringComparison.OrdinalIgnoreCase) ||
                                   sourceName.Contains("metal", System.StringComparison.OrdinalIgnoreCase);
                    mapped[i] = limitToTwoMaterials ? (neutral ? catalog.darkMaterial : teamMaterial) :
                        catalog.ResolveMaterial(sourceName);
                }
                renderer.sharedMaterials = mapped;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }

    public sealed class StylizedActorView : MonoBehaviour
    {
        private Animator animator;
        private Vector3 previousPosition;

        public void Configure(Transform target, DefenderPresentationCatalog catalog)
        {
            animator = target.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = catalog.actorAnimator;
            previousPosition = transform.position;
        }

        public void Attack() => Trigger("Attack");
        public void Hit() => Trigger("Hit");
        public void Disabled() => Trigger("Hit");
        public void Ability() => Trigger("Ability");
        public void Victory() => Trigger("Victory");
        public void Phase() => Trigger("Phase");
        public void BossAttack() => Trigger("BossAttack");

        private void Update()
        {
            if (animator == null) return;
            float moved = (transform.position - previousPosition).sqrMagnitude;
            previousPosition = transform.position;
            animator.SetBool("Moving", moved > 0.0001f);
        }

        private void Trigger(string parameter)
        {
            if (animator != null) animator.SetTrigger(parameter);
        }

        public static void PlayDefeatEcho(GameObject prefab, DefenderPresentationCatalog catalog, Vector3 position,
            float scale = 1f)
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
            animator.runtimeAnimatorController = catalog.actorAnimator;
            animator.Play("Defeat", 0, 0f);
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
