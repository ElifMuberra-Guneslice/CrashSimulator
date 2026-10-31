using CrashSimulator.Vehicle;
using UnityEngine;

namespace CrashSimulator.Damage
{
    public struct DamageReportSnapshot
    {
        public string VehicleId;
        public int ImpactCount, DetachedParts;
        public float LastImpactSpeedKph, LastImpulse, LastSeverity;
        public DamageRegion MainRegion;
        public Vector3 LastDirection;
        public MechanicalCondition Mechanical;
    }

    /// <summary>Presentation-independent bounded history. Scores are gameplay values, not crash-test ratings.</summary>
    [RequireComponent(typeof(VehicleDamage))]
    public sealed class DamageReport : MonoBehaviour
    {
        private const int Capacity = 32;
        private readonly CollisionData[] impacts = new CollisionData[Capacity];
        private int next, stored;
        private VehicleDamage damage;
        private MechanicalDamage mechanical;
        private DetachablePart[] parts;
        public int StoredImpacts => stored;
        public int TotalImpacts { get; private set; }
        public CollisionData GetImpact(int chronologicalIndex)
        {
            if (chronologicalIndex < 0 || chronologicalIndex >= stored) return default;
            return impacts[(next-stored+chronologicalIndex+Capacity)%Capacity];
        }
        private void Awake()
        {
            damage = GetComponent<VehicleDamage>();
            mechanical = GetComponent<MechanicalDamage>();
            parts = GetComponentsInChildren<DetachablePart>(true);
        }
        private void OnEnable()
        {
            damage.DamageApplied += Record;
            damage.Restored += Clear;
        }
        private void OnDisable()
        {
            damage.DamageApplied -= Record;
            damage.Restored -= Clear;
        }
        private void Record(in CollisionData data)
        {
            impacts[next] = data;
            next = (next+1)%Capacity;
            stored = Mathf.Min(Capacity,stored+1);
            TotalImpacts++;
        }
        public void Clear() { next = stored = TotalImpacts = 0; }
        public DamageReportSnapshot Snapshot()
        {
            int detached = 0;
            if (parts != null) foreach (var part in parts) if (part != null && part.Detached) detached++;
            CollisionData last = stored > 0 ? GetImpact(stored-1) : default;
            return new DamageReportSnapshot
            {
                VehicleId = damage != null ? damage.Identifier : name,
                ImpactCount = TotalImpacts, DetachedParts = detached,
                LastImpactSpeedKph = last.NormalSpeed*3.6f,
                LastImpulse = last.Impulse.magnitude, LastSeverity = last.Severity,
                MainRegion = last.Region, LastDirection = last.Direction,
                Mechanical = mechanical != null ? mechanical.Condition : default
            };
        }
    }
}
