using UnityEngine;
namespace CrashSimulator.Damage
{
    public static class CollisionSeverityCalculator
    {
        // Deliberately a game score, NOT an injury metric or calibrated engineering measurement.
        public static float Calculate(float normalSpeed, float impulse, float vehicleMass,
            float speedWeight = 2.2f, float deltaVelocityWeight = 3.0f, float threshold = 2)
        {
            if (normalSpeed < threshold || impulse <= 0) return 0;
            float impulseDeltaVelocity = impulse / Mathf.Max(100, vehicleMass);
            return Mathf.Clamp((normalSpeed - threshold) * Mathf.Max(0, speedWeight)
                + impulseDeltaVelocity * Mathf.Max(0, deltaVelocityWeight), 0, 100);
        }
    }
}
