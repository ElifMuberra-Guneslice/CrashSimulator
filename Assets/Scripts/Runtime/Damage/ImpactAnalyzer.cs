using UnityEngine;
namespace CrashSimulator.Damage
{
    public static class ImpactAnalyzer
    {
        public static DamageRegion RegionAt(Vector3 point, Vector3 normal)
        {
            Vector3 absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            if (absolute.y > absolute.x && absolute.y > absolute.z)
                return point.y > 0.9f ? DamageRegion.Roof : DamageRegion.Undercarriage;
            // Face normal disambiguates a side strike near a front/rear corner.
            if (absolute.x > absolute.z) return point.x < 0 ? DamageRegion.Left : DamageRegion.Right;
            return point.z >= 0 ? DamageRegion.Front : DamageRegion.Rear;
        }

        public static CollisionData Analyze(Collision collision, Transform vehicle, float mass,
            float speedWeight, float impulseWeight, float minimumSpeed)
        {
            var surface = collision.collider.GetComponentInParent<ImpactSurface>();
            var otherBody = collision.rigidbody;
            var result = new CollisionData
            {
                VehicleId = vehicle.GetInstanceID(),
                OtherId = otherBody != null ? otherBody.GetInstanceID()
                    : surface != null ? surface.GetInstanceID() : collision.collider.GetInstanceID(),
                Timestamp = Time.fixedTimeAsDouble,
                RelativeVelocity = collision.relativeVelocity,
                Impulse = collision.impulse,
                AvailableContactCount = collision.contactCount,
                Category = surface != null ? surface.category : ImpactCategory.Environment
            };
            int count = Mathf.Min(CollisionData.ContactCapacity, collision.contactCount);
            for (int i = 0; i < count; i++)
            {
                var c = collision.GetContact(i);
                result.AddContact(new ImpactContact
                {
                    Point = c.point, Normal = c.normal,
                    LocalPoint = vehicle.InverseTransformPoint(c.point),
                    LocalNormal = vehicle.InverseTransformDirection(c.normal),
                    NormalSpeed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, c.normal))
                });
            }
            result.Direction = result.Primary.Normal; // Incoming force points into the receiving body.
            result.Region = RegionAt(result.Primary.LocalPoint, result.Primary.LocalNormal);
            result.Severity = CollisionSeverityCalculator.Calculate(result.NormalSpeed,
                result.Impulse.magnitude, mass, speedWeight, impulseWeight, minimumSpeed);
            return result;
        }
    }

    /// <summary>Bounded per-other-body gate; independent simultaneous impacts are not suppressed globally.</summary>
    public sealed class CollisionDeduplicator
    {
        private readonly int[] ids = new int[32];
        private readonly double[] timestamps = new double[32];
        private int used;
        public bool Accept(int id, double time, float cooldown)
        {
            int oldest = 0;
            for (int i = 0; i < used; i++)
            {
                if (ids[i] == id)
                {
                    if (time - timestamps[i] < cooldown) return false;
                    timestamps[i] = time;
                    return true;
                }
                if (timestamps[i] < timestamps[oldest]) oldest = i;
            }
            int slot = used < ids.Length ? used++ : oldest;
            ids[slot] = id; timestamps[slot] = time;
            return true;
        }
        public void Clear() => used = 0;
    }
}
