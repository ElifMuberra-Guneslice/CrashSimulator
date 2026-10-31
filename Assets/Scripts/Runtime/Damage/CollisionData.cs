using UnityEngine;

namespace CrashSimulator.Damage
{
    public enum ImpactCategory { Environment, Vehicle, Concrete, Metal, Glass, Ground, Debris }
    public enum DamageRegion { Front, Rear, Left, Right, Roof, Undercarriage,
        FrontLeft, FrontRight, RearLeft, RearRight, LeftDoor, RightDoor, Hood, Trunk,
        FrontLeftWheel, FrontRightWheel, RearLeftWheel, RearRightWheel }
    public enum DamageVisualState { Undamaged, Light, Medium, Severe }

    public struct ImpactContact
    {
        public Vector3 Point, Normal, LocalPoint, LocalNormal;
        public float NormalSpeed;
    }

    /// <summary>Value snapshot: no references to Unity's temporary contact array. Safe for future replay observers.</summary>
    public struct CollisionData
    {
        public const int ContactCapacity = 8;
        public int VehicleId, OtherId, ContactCount, AvailableContactCount;
        public uint Sequence;
        public double Timestamp;
        public Vector3 RelativeVelocity, Impulse, Direction;
        public ImpactCategory Category;
        public DamageRegion Region;
        public float NormalSpeed, Severity;
        public ImpactContact Primary;
        private ImpactContact c0, c1, c2, c3, c4, c5, c6, c7;
        public ImpactContact GetContact(int index)
        {
            switch (index)
            {
                case 0: return c0; case 1: return c1; case 2: return c2; case 3: return c3;
                case 4: return c4; case 5: return c5; case 6: return c6; case 7: return c7;
                default: return default;
            }
        }
        public void AddContact(ImpactContact contact)
        {
            switch (ContactCount)
            {
                case 0: c0 = contact; break; case 1: c1 = contact; break;
                case 2: c2 = contact; break; case 3: c3 = contact; break;
                case 4: c4 = contact; break; case 5: c5 = contact; break;
                case 6: c6 = contact; break; case 7: c7 = contact; break;
                default: return;
            }
            if (ContactCount == 0 || contact.NormalSpeed > NormalSpeed)
            { Primary = contact; NormalSpeed = contact.NormalSpeed; }
            ContactCount++;
        }
    }

    public delegate void CollisionHandler(in CollisionData data);

    /// <summary>Synchronous immutable observation contract; subscribers must defer heavy work.</summary>
    public sealed class CollisionEventDispatcher
    {
        public event CollisionHandler Analyzed;
        public void Publish(in CollisionData data) => Analyzed?.Invoke(in data);
    }
}
