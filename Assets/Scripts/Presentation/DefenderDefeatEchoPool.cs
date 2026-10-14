using System.Collections.Generic;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public sealed class DefenderDefeatEchoPool : MonoBehaviour
    {
        private readonly List<DefeatEcho>[] pools =
        {
            new(), new(), new(), new(), new(), new(), new(), new()
        };
        private DefenderPresentationCatalog catalog;
        private readonly GameObject[] prefabs = new GameObject[8];
        private readonly float[] scales = new float[8];
        private readonly RuntimeAnimatorController[] animators = new RuntimeAnimatorController[8];

        public void Initialize(DefenderPresentationCatalog value)
        {
            catalog = value;
            for (int kind = 0; kind < pools.Length; kind++)
            {
                prefabs[kind] = catalog.ActorPrefab((PresentationActorKind)kind);
                scales[kind] = catalog.ActorScale((PresentationActorKind)kind);
                animators[kind] = catalog.ActorAnimator((PresentationActorKind)kind);
                if (prefabs[kind] != null)
                    while (pools[kind].Count < 2) CreateEcho((PresentationActorKind)kind);
            }
        }

        internal void Prewarm(PresentationActorKind kind, int count)
        {
            if (prefabs[(int)kind] == null) return;
            List<DefeatEcho> pool = pools[(int)kind];
            while (pool.Count < count) CreateEcho(kind);
        }

        public void Play(PresentationActorKind kind, Vector3 position) => Play(kind, position, null);

        internal void Play(PresentationActorKind kind, Vector3 position, string bossOrnament)
        {
            if (catalog == null || prefabs[(int)kind] == null) return;
            List<DefeatEcho> pool = pools[(int)kind];
            foreach (var echo in pool)
            {
                if (echo == null || echo.gameObject.activeSelf) continue;
                echo.transform.parent.position = position;
                echo.Begin(true);
                ShowOrnament(kind, echo, bossOrnament);
                return;
            }
            DefeatEcho created = CreateEcho(kind);
            created.transform.parent.position = position;
            created.Begin(true);
            ShowOrnament(kind, created, bossOrnament);
        }

        private static void ShowOrnament(PresentationActorKind kind, DefeatEcho echo, string bossOrnament)
        {
            if (kind == PresentationActorKind.Boss && bossOrnament != null)
                BossPhaseVisual.ShowRestingParts(echo.transform, bossOrnament);
        }

        private DefeatEcho CreateEcho(PresentationActorKind kind)
        {
            // The animator may restore its root bind position; the anchor owns world placement.
            var anchor = new GameObject("Defeat Echo Anchor");
            anchor.transform.SetParent(transform, false);
            DefeatEcho echo = StylizedActorView.CreateDefeatEcho(prefabs[(int)kind], catalog,
                anchor.transform, scales[(int)kind], animators[(int)kind]);
            echo.gameObject.SetActive(false);
            pools[(int)kind].Add(echo);
            return echo;
        }
    }
}
