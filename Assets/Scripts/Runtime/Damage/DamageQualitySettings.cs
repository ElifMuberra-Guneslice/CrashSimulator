using UnityEngine;
namespace CrashSimulator.Damage
{
    [CreateAssetMenu(menuName="Crash Simulator/Damage Quality")]
    public sealed class DamageQualitySettings : ScriptableObject
    {
        public bool meshDeformation = true;
        public bool detachableParts = true;
        public bool particles = true;
        public bool audio = true;
        public bool cameraShake = true;
        [Range(32,1024)] public int verticesPerImpact = 256;
        [Range(128,4096)] public int maximumMeshVertices = 2048;
        [Range(1,16)] public int activeDebris = 8;
        [Range(2,30)] public float debrisLifetime = 12;
        [Range(4,64)] public int particlesPerBurst = 18;
    }
}
