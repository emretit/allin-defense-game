using UnityEngine;

namespace DefenderChronicles.Utils
{
    public static class SimpleVfx
    {
        public static void Burst(Vector3 worldPosition, Color color, float destroyAfter = 0.45f)
        {
            var go = new GameObject("BurstVfx");
            go.transform.position = worldPosition;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.15f;
            main.loop = false;
            main.startLifetime = 0.25f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = color;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;
            ps.Play();
            Object.Destroy(go, destroyAfter);
        }
    }
}
