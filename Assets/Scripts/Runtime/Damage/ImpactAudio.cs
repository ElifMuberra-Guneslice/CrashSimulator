using UnityEngine;
using CrashSimulator.UI;

namespace CrashSimulator.Damage
{
    /// <summary>Optional clips. Missing audio is intentionally silent until free/owned clips are assigned.</summary>
    [RequireComponent(typeof(VehicleDamage))]
    public sealed class ImpactAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip lightClip, medium, heavy, glass, metal, partDetach;
        private readonly AudioSource[] sources = new AudioSource[3];
        private VehicleDamage damage;
        private DetachablePart[] parts;
        private float lastPlayed;
        private int nextSource;
        private void Awake()
        {
            damage = GetComponent<VehicleDamage>();
            parts = GetComponentsInChildren<DetachablePart>(true);
            for (int i=0;i<sources.Length;i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 1;
                sources[i].maxDistance = 45;
            }
        }
        private void OnEnable()
        {
            damage.DamageApplied += PlayImpact;
            foreach (var part in parts) part.Broken += PlayPart;
        }
        private void OnDisable()
        {
            damage.DamageApplied -= PlayImpact;
            foreach (var part in parts) if (part != null) part.Broken -= PlayPart;
        }
        private void PlayImpact(in CollisionData impact)
        {
            if (impact.Severity < 5) return;
            AudioClip clip = impact.Category == ImpactCategory.Glass && glass != null ? glass
                : impact.Category == ImpactCategory.Metal && metal != null ? metal
                : impact.Severity >= 35 ? heavy : impact.Severity >= 15 ? medium : lightClip;
            Play(clip);
        }
        private void PlayPart(DetachablePart part) => Play(partDetach);
        private void Play(AudioClip clip)
        {
            if (clip == null || Time.unscaledTime-lastPlayed < .12f || damage.Quality != null && !damage.Quality.audio) return;
            lastPlayed = Time.unscaledTime;
            var source = sources[nextSource];
            nextSource = (nextSource+1)%sources.Length;
            source.Stop(); source.clip = clip; source.volume = GameUiSettings.EffectsVolume; source.Play();
        }
    }
}
