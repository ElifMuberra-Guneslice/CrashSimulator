using UnityEngine;

namespace CrashSimulator.Vehicle
{
    [CreateAssetMenu(menuName = "Crash Simulator/Vehicle Settings")]
    public sealed class VehicleSettings : ScriptableObject
    {
        [Header("Body")]
        [Min(100)] public float mass = 1200;
        public Vector3 centerOfMass = new Vector3(0, 0.45f, 0);
        [Header("Drive (torque per driven wheel, Nm)")]
        [Min(0)] public float motorTorque = 650;
        [Min(0)] public float brakeTorque = 2200;
        [Min(1)] public float forwardSpeedKph = 110;
        [Min(1)] public float reverseSpeedKph = 25;
        [Header("Steering")]
        [Range(1, 50)] public float lowSpeedSteerAngle = 30;
        [Range(1, 30)] public float highSpeedSteerAngle = 8;
        [Min(1)] public float steeringRate = 85;
        [Header("Suspension (per wheel)")]
        [Min(0.05f)] public float wheelRadius = 0.34f;
        [Min(0.01f)] public float suspensionDistance = 0.22f;
        [Min(1)] public float spring = 35000;
        [Min(0)] public float damper = 4500;
        [Range(0, 1)] public float suspensionTarget = 0.5f;
    }
}
