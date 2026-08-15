using System.Collections.Generic;
using UnityEngine;

namespace DefenderChronicles.Presentation
{
    public sealed class DefenderVfxPool : MonoBehaviour
    {
        private readonly List<PooledVfx> pool = new();
        private DefenderPresentationCatalog catalog;
        private Mesh diamondMesh;
        private Mesh ringMesh;

        public void Initialize(DefenderPresentationCatalog value, int initialSize = 24)
        {
            catalog = value;
            diamondMesh = BuildDiamond();
            ringMesh = BuildRing(24);
            for (int i = 0; i < initialSize; i++) CreateItem();
        }

        public void Play(PresentationSignal signal)
        {
            if (catalog == null) return;
            PooledVfx item = pool.Find(candidate => !candidate.IsPlaying) ?? CreateItem();
            Color color = CueColor(signal.cue);
            bool projectile = signal.cue is PresentationCue.OutpostAttacked or PresentationCue.MageImpact
                or PresentationCue.HeroAttacked or PresentationCue.BossAttack;
            bool ring = signal.cue is PresentationCue.HealPulse or PresentationCue.OutpostDisrupted
                or PresentationCue.SlowApplied or PresentationCue.HeroAbility or PresentationCue.BossPhase
                or PresentationCue.Victory or PresentationCue.Defeat;
            float endScale = signal.cue switch
            {
                PresentationCue.HeroAbility => 3.2f,
                PresentationCue.BossPhase => 2.7f,
                PresentationCue.Victory or PresentationCue.Defeat => 2.2f,
                PresentationCue.HealPulse => 1.45f,
                PresentationCue.OutpostDisrupted or PresentationCue.SlowApplied => 1.05f,
                PresentationCue.EnemyDefeated or PresentationCue.UnitDefeated => 0.72f,
                PresentationCue.EnemyHit => 0.36f,
                _ => 0.62f
            };
            Vector3 from = signal.source != null ? signal.source.transform.position + Vector3.up * 0.8f : signal.position;
            Vector3 to = signal.position + Vector3.up * (projectile ? 0.45f : 0.08f);
            item.Play(ring ? ringMesh : diamondMesh, projectile ? catalog.fireMaterial : catalog.magicMaterial,
                from, to, color, projectile ? 0.24f : 0.34f, projectile, endScale);
        }

        private PooledVfx CreateItem()
        {
            var go = new GameObject($"Pooled VFX {pool.Count + 1}");
            go.transform.SetParent(transform, false);
            var item = go.AddComponent<PooledVfx>();
            pool.Add(item);
            return item;
        }

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

        public void Play(Mesh mesh, Material material, Vector3 start, Vector3 end, Color color, float seconds,
            bool moving, float targetScale)
        {
            filter ??= gameObject.AddComponent<MeshFilter>();
            meshRenderer ??= gameObject.AddComponent<MeshRenderer>();
            block ??= new MaterialPropertyBlock();
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
