using System;
using CrashSimulator.Input;
using UnityEngine;

namespace CrashSimulator.Vehicle
{
    [Serializable]
    public sealed class VehicleWheel
    {
        public WheelCollider collider;
        public Transform visual;
        public bool steering;
        public bool driven;
    }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour
    {
        [SerializeField] private VehicleSettings settings;
        [SerializeField] private VehicleInputSource input;
        [SerializeField] private VehicleWheel[] wheels = Array.Empty<VehicleWheel>();
        [SerializeField] private MonoBehaviour mechanicalSource;
        private IVehicleMechanicalStatus mechanical;
        private WheelFrictionCurve[] originalSideways;
        private float[] lastWheelConditions;
        private float baseDrag;
        private Rigidbody body;
        private float steerAngle;
        private float resumeAt;

        public Rigidbody Body => body;
        public float SpeedKph => body == null ? 0 : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeed => body == null ? 0 : Vector3.Dot(body.linearVelocity, transform.forward);
        public VehicleWheel[] Wheels => wheels;
        public void SetMechanicalSource(MonoBehaviour source)
        { mechanicalSource=source; mechanical=source as IVehicleMechanicalStatus; }
        public void SetInputSource(VehicleInputSource source)=>input=source;

        public void Configure(VehicleSettings configuration, VehicleInputSource source, VehicleWheel[] wheelSet)
        {
            settings = configuration;
            input = source;
            wheels = wheelSet;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            mechanical=mechanicalSource as IVehicleMechanicalStatus;
            baseDrag=body.linearDamping;
            if (settings == null || wheels.Length != 4)
            {
                Debug.LogError("Vehicle needs settings and four wheel references.", this);
                enabled = false;
                return;
            }
            foreach (var wheel in wheels)
            {
                if (wheel == null || wheel.collider == null)
                {
                    Debug.LogError("Vehicle has a missing WheelCollider.", this);
                    enabled = false;
                    return;
                }
            }

            body.mass = settings.mass;
            body.centerOfMass = settings.centerOfMass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.solverIterations = 8;
            body.solverVelocityIterations = 2;
            originalSideways=new WheelFrictionCurve[wheels.Length];
            lastWheelConditions=new float[wheels.Length];
            for(int i=0;i<wheels.Length;i++)lastWheelConditions[i]=-1;
            for(int i=0;i<wheels.Length;i++)originalSideways[i]=wheels[i].collider.sidewaysFriction;
            foreach (var wheel in wheels)
            {
                wheel.collider.radius = settings.wheelRadius;
                wheel.collider.suspensionDistance = settings.suspensionDistance;
                var spring = wheel.collider.suspensionSpring;
                spring.spring = settings.spring;
                spring.damper = settings.damper;
                spring.targetPosition = settings.suspensionTarget;
                wheel.collider.suspensionSpring = spring;
            }
            // This setting belongs to the whole vehicle, so call it once.
            wheels[0].collider.ConfigureVehicleSubsteps(5, 3, 1);
        }

        private void FixedUpdate()
        {
            VehicleCommand command = input != null && input.isActiveAndEnabled
                ? input.Command : default;
            if (Time.time < resumeAt) command = new VehicleCommand { Brake = 1 };

            float speed = ForwardSpeed;
            body.linearDamping=baseDrag+(mechanical?.AdditionalDrag ?? 0);
            float throttle = Mathf.Clamp(command.Throttle, -1, 1);
            float braking = Mathf.Clamp01(command.Brake);
            // An opposite direction command brakes first, then reverses near standstill.
            if (throttle * speed < -0.5f)
            {
                braking = Mathf.Max(braking, Mathf.Abs(throttle));
                throttle = 0;
            }
            float limit = throttle >= 0 ? settings.forwardSpeedKph : settings.reverseSpeedKph;
            float torqueScale = 1 - Mathf.InverseLerp(limit * 0.85f, limit, Mathf.Abs(speed) * 3.6f);
            if (braking > 0.01f) throttle = 0;
            float maxAngle = Mathf.Lerp(settings.lowSpeedSteerAngle, settings.highSpeedSteerAngle,
                Mathf.InverseLerp(0, settings.forwardSpeedKph, Mathf.Abs(speed) * 3.6f));
            steerAngle = Mathf.MoveTowards(steerAngle,
                Mathf.Clamp(Mathf.Clamp(command.Steering, -1, 1) * maxAngle+(mechanical?.SteeringBiasDegrees ?? 0),
                    -settings.lowSpeedSteerAngle,settings.lowSpeedSteerAngle), settings.steeringRate * Time.fixedDeltaTime);

            for(int i=0;i<wheels.Length;i++)
            {
                var wheel=wheels[i];
                bool detached=mechanical!=null && mechanical.IsWheelDetached(i);
                if(wheel.collider.enabled==detached)wheel.collider.enabled=!detached;
                if(detached)continue;
                float condition=mechanical?.WheelCondition(i) ?? 1;
                wheel.collider.motorTorque = wheel.driven ? throttle * settings.motorTorque * torqueScale
                    *(mechanical?.EngineMultiplier ?? 1)*condition : 0;
                float wheelBrake = !wheel.steering ? Mathf.Max(braking,Mathf.Clamp01(command.Handbrake)) : braking;
                wheel.collider.brakeTorque = wheelBrake * settings.brakeTorque*(mechanical?.BrakeMultiplier ?? 1)*condition;
                wheel.collider.steerAngle = wheel.steering ? steerAngle : 0;
                if(Mathf.Abs(lastWheelConditions[i]-condition)>.001f)
                {
                    var friction=originalSideways[i];friction.stiffness*=Mathf.Lerp(.45f,1,condition);
                    wheel.collider.sidewaysFriction=friction;lastWheelConditions[i]=condition;
                }
            }
        }

        private void LateUpdate()
        {
            for(int i=0;i<wheels.Length;i++)
            {
                var wheel=wheels[i];
                if(mechanical!=null && mechanical.IsWheelDetached(i))continue;
                if (wheel.visual == null) continue;
                wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
                wheel.visual.SetPositionAndRotation(position, rotation);
            }
        }

        public void ResetMotion()
        {
            if (body == null || settings == null) return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            steerAngle = 0;
            resumeAt = Time.time + 0.3f;
            if (input != null) input.ClearInput();
            foreach (var wheel in wheels)
            {
                wheel.collider.motorTorque = 0;
                wheel.collider.brakeTorque = settings.brakeTorque;
                wheel.collider.steerAngle = 0;
                // Recreate the native wheel state to clear stored rotation/suspension velocity.
                wheel.collider.enabled = false;
                wheel.collider.enabled = true;
            }
        }

        private void OnDisable()
        {
            foreach (var wheel in wheels)
            {
                if (wheel?.collider == null) continue;
                wheel.collider.motorTorque = 0;
                wheel.collider.brakeTorque = settings != null ? settings.brakeTorque : 0;
            }
        }
    }
}
