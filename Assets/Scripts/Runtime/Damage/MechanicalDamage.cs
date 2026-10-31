using CrashSimulator.Vehicle;
using UnityEngine;
namespace CrashSimulator.Damage
{
    public struct MechanicalCondition
    {
        public float Engine, Brakes, SteeringBias, Drag;
        public bool EngineFailed;
        public Vector4 WheelDamage;
        public static MechanicalCondition Calculate(float front,float rear,float left,float right,float under,Vector4 wheels)
        {
            float worst=Mathf.Max(Mathf.Max(wheels.x,wheels.y),Mathf.Max(wheels.z,wheels.w));
            bool failed=front>=.95f || under>=.95f;
            return new MechanicalCondition
            {
                Engine=failed?0:Mathf.Clamp01(1-front*.75f-under*.2f),EngineFailed=failed,
                Brakes=Mathf.Clamp(1-front*.3f-rear*.2f-worst*.25f,.25f,1),
                SteeringBias=Mathf.Clamp((right-left)*4+(wheels.y-wheels.x)*3,-6,6),
                Drag=front*.25f+rear*.2f+under*.35f+worst*.2f,WheelDamage=wheels
            };
        }
    }
    [RequireComponent(typeof(VehicleDamage))]
    public sealed class MechanicalDamage : MonoBehaviour, IVehicleMechanicalStatus
    {
        private VehicleDamage damage;
        private DetachablePart[] parts;
        private MechanicalCondition condition;
        private int detachedMask;
        public float EngineMultiplier=>condition.Engine;
        public float BrakeMultiplier=>condition.Brakes;
        public float SteeringBiasDegrees=>condition.SteeringBias;
        public float AdditionalDrag=>condition.Drag;
        public bool EngineFailed=>condition.EngineFailed;
        public MechanicalCondition Condition=>condition;
        public float WheelCondition(int index)=>Mathf.Clamp01(1-condition.WheelDamage[index]*.65f);
        public bool IsWheelDetached(int index)=>(detachedMask&(1<<index))!=0;
        private void Awake()
        {
            damage=GetComponent<VehicleDamage>();parts=GetComponentsInChildren<DetachablePart>(true);Restore();
        }
        private void OnEnable()
        {
            damage.DamageApplied+=Refresh;damage.Restored+=Restore;
            foreach(var part in parts)part.Broken+=OnPartBroken;
        }
        private void OnDisable()
        {
            damage.DamageApplied-=Refresh;damage.Restored-=Restore;
            foreach(var part in parts)if(part!=null)part.Broken-=OnPartBroken;
        }
        private void OnPartBroken(DetachablePart part){if(part.wheelIndex>=0)detachedMask|=1<<part.wheelIndex;}
        private void Refresh(in CollisionData data)
        {
            condition=MechanicalCondition.Calculate(damage.DamageOf(DamageRegion.Front),damage.DamageOf(DamageRegion.Rear),
                damage.DamageOf(DamageRegion.Left),damage.DamageOf(DamageRegion.Right),damage.DamageOf(DamageRegion.Undercarriage),
                new Vector4(damage.DamageOf(DamageRegion.FrontLeftWheel),damage.DamageOf(DamageRegion.FrontRightWheel),
                    damage.DamageOf(DamageRegion.RearLeftWheel),damage.DamageOf(DamageRegion.RearRightWheel)));
        }
        private void Restore(){condition=MechanicalCondition.Calculate(0,0,0,0,0,Vector4.zero);detachedMask=0;}
    }
}
