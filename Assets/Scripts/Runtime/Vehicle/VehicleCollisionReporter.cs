using System;
using UnityEngine;
using CrashSimulator.Damage;

namespace CrashSimulator.Vehicle
{
    public readonly struct VehicleImpact
    {
        public readonly Vector3 Point;
        public readonly float RelativeSpeedKph;
        public readonly float Impulse;
        public VehicleImpact(Vector3 point, float speed, float impulse)
        {
            Point = point;
            RelativeSpeedKph = speed;
            Impulse = impulse;
        }
    }

    public sealed class VehicleCollisionReporter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float minimumImpactSpeed = 2;
        [SerializeField, Min(0)] private float cooldown = 0.3f;
        [SerializeField, Min(0)] private float speedWeight = 2.2f;
        [SerializeField, Min(0)] private float impulseWeight = 3;
        private readonly CollisionDeduplicator gate = new CollisionDeduplicator();
        private Rigidbody body;
        private double ignoreUntil;
        public CollisionEventDispatcher Events { get; } = new CollisionEventDispatcher();
        public CollisionData LastCollision { get; private set; }
        public event Action<VehicleImpact> Impact;
        public int ImpactCount { get; private set; }
        public VehicleImpact LastImpact { get; private set; }

        private void Awake() => body = GetComponent<Rigidbody>();

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.fixedTimeAsDouble < ignoreUntil || collision.contactCount == 0 || body == null) return;
            var data = ImpactAnalyzer.Analyze(collision, transform, body.mass, speedWeight, impulseWeight, minimumImpactSpeed);
            if (data.Severity <= 0 || !gate.Accept(data.OtherId, data.Timestamp, cooldown)) return;
            ImpactCount++;
            data.Sequence = (uint)ImpactCount;
            LastCollision = data;
            LastImpact = new VehicleImpact(data.Primary.Point, data.NormalSpeed * 3.6f, data.Impulse.magnitude);
            Events.Publish(in data);
            Impact?.Invoke(LastImpact);
        }

        public void ClearHistory()
        {
            ImpactCount = 0;
            LastImpact = default;
            LastCollision = default;
            gate.Clear();
            ignoreUntil = Time.fixedTimeAsDouble + cooldown;
        }
    }
}
