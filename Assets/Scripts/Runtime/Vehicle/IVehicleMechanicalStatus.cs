namespace CrashSimulator.Vehicle
{
    /// <summary>Optional adapter contract; the driving controller does not depend on a damage implementation.</summary>
    public interface IVehicleMechanicalStatus
    {
        float EngineMultiplier { get; }
        float BrakeMultiplier { get; }
        float SteeringBiasDegrees { get; }
        float AdditionalDrag { get; }
        bool EngineFailed { get; }
        float WheelCondition(int index);
        bool IsWheelDetached(int index);
    }
}
