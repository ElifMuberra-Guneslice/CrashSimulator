using UnityEngine;

namespace CrashSimulator.Input
{
    public struct VehicleCommand
    {
        public float Throttle;
        public float Steering;
        public float Brake;
        public float Handbrake;
    }

    /// <summary>Player input and future Crash Lab controllers share this contract.</summary>
    public abstract class VehicleInputSource : MonoBehaviour
    {
        public abstract VehicleCommand Command { get; }
        public virtual void ClearInput() { }
    }
}
