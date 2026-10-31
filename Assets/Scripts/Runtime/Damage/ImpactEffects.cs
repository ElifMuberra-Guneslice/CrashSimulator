using UnityEngine;

namespace CrashSimulator.Damage
{
    /// <summary>One reusable low-count particle system per vehicle; no allocation at impact time.</summary>
    [RequireComponent(typeof(VehicleDamage))]
    public sealed class ImpactEffects : MonoBehaviour
    {
        private VehicleDamage damage;
        private MechanicalDamage mechanical;
        private ParticleSystem particles;
        private Material particleMaterial;
        private float nextSmoke;
        private float nextTireSmoke;
        private void Awake()
        {
            damage = GetComponent<VehicleDamage>();
            mechanical = GetComponent<MechanicalDamage>();
            var go = new GameObject("Pooled Impact Particles");
            go.transform.SetParent(transform, false);
            particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.startLifetime = .5f;
            main.startSize = .075f;
            main.startSpeed = 2;
            main.gravityModifier = .7f;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) { particleMaterial = new Material(shader); renderer.sharedMaterial = particleMaterial; }
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnEnable() { damage.DamageApplied += Burst; damage.Restored += Clear; }
        private void OnDisable() { damage.DamageApplied -= Burst; damage.Restored -= Clear; }
        private void OnDestroy() { if (particleMaterial != null) Destroy(particleMaterial); }
        private void Clear() { if (particles != null) particles.Clear(); }
        private void Burst(in CollisionData impact)
        {
            if (damage.Quality != null && !damage.Quality.particles) return;
            if (impact.Severity < 9) return;
            int limit = damage.Quality != null ? damage.Quality.particlesPerBurst : 18;
            int count = Mathf.Clamp(Mathf.RoundToInt(impact.Severity*.28f), 2, limit);
            // Copper sparks for metal, grey dust for concrete/ground, pale shards near glass.
            Color color = impact.Category == ImpactCategory.Concrete || impact.Category == ImpactCategory.Ground
                ? new Color(.62f,.58f,.5f,1) : impact.Region == DamageRegion.Roof || impact.Category == ImpactCategory.Glass
                    ? new Color(.7f,.9f,1,1) : new Color(1,.58f,.14f,1);
            Emit(impact.Primary.Point, impact.Primary.Normal, color, count, .055f, .35f);
        }
        private void Update()
        {
            if (damage.Quality != null && !damage.Quality.particles) return;
            if (mechanical == null) return;
            if (mechanical.EngineMultiplier <= .45f && Time.time >= nextSmoke)
            {
                nextSmoke = Time.time + .4f;
                Emit(transform.TransformPoint(new Vector3(0,1,1.1f)), Vector3.up,
                    new Color(.25f,.25f,.25f,.7f), 2, .22f, 1.2f);
            }
            if (Time.time < nextTireSmoke) return;
            nextTireSmoke = Time.time + .3f;
            for (int i=0;i<4;i++)
            {
                if (mechanical.WheelCondition(i) >= .65f) continue;
                Vector3 local = new Vector3(i%2==0?-.9f:.9f,.35f,i<2?1.25f:-1.25f);
                Emit(transform.TransformPoint(local),Vector3.up,
                    new Color(.35f,.35f,.35f,.55f),1,.13f,.6f);
            }
        }
        private void Emit(Vector3 point, Vector3 direction, Color color, int count, float size, float life)
        {
            var settings = new ParticleSystem.EmitParams
            { position = point, velocity = direction.normalized*2, startColor = color, startSize = size, startLifetime = life };
            particles.Emit(settings, count);
        }
    }
}
