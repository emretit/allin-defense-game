using System.Collections;
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
                var visual = Object.Instantiate(visualPrefab, root.transform);
                visual.name = "Art";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * visualScale;
                ApplyPalette(visual, catalog);
                root.AddComponent<StylizedActorView>().Configure(visual.transform);
            }
            return root;
        }

        public static GameObject SpawnModel(GameObject prefab, Transform parent, DefenderPresentationCatalog catalog,
            string name = null)
        {
            if (prefab == null) return null;
            var instance = Object.Instantiate(prefab, parent);
            if (!string.IsNullOrWhiteSpace(name)) instance.name = name;
            ApplyPalette(instance, catalog);
            return instance;
        }

        public static void ApplyPalette(GameObject instance, DefenderPresentationCatalog catalog)
        {
            if (instance == null || catalog == null) return;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                var mapped = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                    mapped[i] = catalog.ResolveMaterial(source[i] != null ? source[i].name : renderer.name);
                renderer.sharedMaterials = mapped;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }

    public sealed class StylizedActorView : MonoBehaviour
    {
        private Transform visual;
        private Vector3 restPosition;
        private Vector3 restScale;
        private Quaternion restRotation;
        private Vector3 previousPosition;
        private float attackPulse;
        private float hitPulse;
        private float disabledPulse;

        public void Configure(Transform target)
        {
            visual = target;
            restPosition = target.localPosition;
            restScale = target.localScale;
            restRotation = target.localRotation;
            previousPosition = transform.position;
        }

        public void Attack() => attackPulse = 1f;
        public void Hit() => hitPulse = 1f;
        public void Disabled() => disabledPulse = 1f;

        private void LateUpdate()
        {
            if (visual == null) return;
            float moved = (transform.position - previousPosition).sqrMagnitude;
            previousPosition = transform.position;
            attackPulse = Mathf.MoveTowards(attackPulse, 0f, Time.deltaTime * 5.5f);
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, Time.deltaTime * 7f);
            disabledPulse = Mathf.MoveTowards(disabledPulse, 0f, Time.deltaTime * 2f);
            float gait = moved > 0.0001f ? Mathf.Sin(Time.time * 11f) * 0.045f : Mathf.Sin(Time.time * 2.2f) * 0.018f;
            visual.localPosition = restPosition + Vector3.up * gait + Vector3.forward * (attackPulse * 0.18f);
            visual.localRotation = restRotation * Quaternion.Euler(hitPulse * -10f, 0f, gait * 32f);
            float scale = 1f + attackPulse * 0.08f - hitPulse * 0.06f;
            visual.localScale = restScale * scale;
        }

        public static void PlayDefeatEcho(GameObject prefab, DefenderPresentationCatalog catalog, Vector3 position,
            float scale = 1f)
        {
            if (prefab == null) return;
            var echo = Object.Instantiate(prefab);
            echo.name = "Defeat Echo";
            echo.transform.position = position;
            echo.transform.localScale = Vector3.one * scale;
            StylizedPresentationFactory.ApplyPalette(echo, catalog);
            foreach (var collider in echo.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var host = echo.AddComponent<DefeatEcho>();
            host.Begin();
        }
    }

    public sealed class DefeatEcho : MonoBehaviour
    {
        private Vector3 startScale;
        private float elapsed;

        public void Begin() => startScale = transform.localScale;

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.58f);
            transform.rotation = Quaternion.Euler(72f * t, transform.eulerAngles.y, 18f * t);
            transform.localScale = startScale * (1f - t * 0.35f);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
