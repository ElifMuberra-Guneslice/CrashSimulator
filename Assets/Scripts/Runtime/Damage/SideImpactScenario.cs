using CrashSimulator.CameraSystem;
using CrashSimulator.Input;
using CrashSimulator.Vehicle;
using UnityEngine;

namespace CrashSimulator.Damage
{
    /// <summary>Repeatable Rigidbody launch; the subsequent collision is solved entirely by PhysX.</summary>
    public sealed class SideImpactScenario : MonoBehaviour
    {
        [SerializeField] private VehicleController striker;
        [SerializeField] private VehicleController target;
        [SerializeField] private PlayerVehicleInput playerInput;
        [SerializeField] private VehicleFollowCamera followCamera;
        [SerializeField] private Vector3 strikerStart = new Vector3(-.65f, .15f, -23);
        [SerializeField] private Vector3 targetStart = new Vector3(0, .15f, -8);
        [SerializeField] private float speedKph = 90;
        [SerializeField,Range(0,180)] private float impactAngle = 90;
        [SerializeField] private bool vehicleTarget = true;
        private int stage;
        private float launchAt;
        public bool Running => stage != 0;
        public VehicleController Striker => striker;
        public VehicleController Target => target;
        public float SpeedKph => speedKph;
        public float ImpactAngle => impactAngle;
        public bool UsesVehicleTarget => vehicleTarget;

        public void Configure(VehicleController moving, VehicleController stationary, PlayerVehicleInput input, VehicleFollowCamera camera)
        {
            striker = moving; target = stationary; playerInput = input; followCamera = camera;
            strikerStart = new Vector3(-.65f,.15f,-23);
            targetStart = new Vector3(0,.15f,-8);
            speedKph = 90;
            impactAngle = 90;
            vehicleTarget = true;
        }
        public void SetSpeedKph(float value) => speedKph=Mathf.Clamp(value,10,120);
        public void SetImpactAngle(float value) => impactAngle=Mathf.Clamp(value,0,180);
        public void SetVehicleTarget(bool value) => vehicleTarget=value;

        public void Begin()
        {
            if (striker == null || target == null || striker.Body == null || target.Body == null) return;
            if (striker.Body.isKinematic || target.Body.isKinematic)
            { Debug.LogError("Both crash test vehicles must have dynamic Rigidbodies.", this); return; }
            striker.SetInputSource(null);
            target.SetInputSource(null);
            if(!target.gameObject.activeSelf)target.gameObject.SetActive(true);
            Quaternion strikerRotation=Quaternion.identity;
            Vector3 launchPoint=strikerStart;
            Vector3 targetPoint=targetStart;
            Quaternion targetRotation=Quaternion.Euler(0,impactAngle,0);
            if(!vehicleTarget)
            {
                float approachAngle=Mathf.Clamp((impactAngle-90)*.25f,-22.5f,22.5f);
                launchPoint=new Vector3(-Mathf.Tan(approachAngle*Mathf.Deg2Rad)*14,.15f,8);
                strikerRotation=Quaternion.Euler(0,approachAngle,0);
                targetPoint=new Vector3(12,.15f,-8);
                targetRotation=Quaternion.Euler(0,90,0);
            }
            striker.GetComponent<VehicleRespawn>().ResetAt(launchPoint,strikerRotation);
            target.GetComponent<VehicleRespawn>().ResetAt(targetPoint,targetRotation);
            stage = 1;
            launchAt = Time.fixedTime + .5f;
            if (followCamera != null) { followCamera.SetTarget(striker.transform); followCamera.Snap(); }
        }

        private void FixedUpdate()
        {
            if (stage != 1 || Time.fixedTime < launchAt) return;
            // 90 km/h = 25 m/s. This initial condition does not override the contact response.
            striker.Body.linearVelocity = striker.transform.forward * (speedKph / 3.6f);
            striker.Body.angularVelocity = Vector3.zero;
            target.Body.linearVelocity = Vector3.zero;
            target.Body.angularVelocity = Vector3.zero;
            stage = 2;
        }

        public void ResetScenario()
        {
            if (striker == null || target == null) return;
            stage = 0;
            striker.GetComponent<VehicleRespawn>().RequestReset();
            target.GetComponent<VehicleRespawn>().RequestReset();
            striker.SetInputSource(playerInput);
            target.SetInputSource(null);
            if (followCamera != null) { followCamera.SetInspect(false); followCamera.SetTarget(striker.transform); }
        }

        public void InspectTarget()
        {
            if (target != null && followCamera != null)
            { followCamera.SetTarget(target.transform); followCamera.SetInspect(true); }
        }

        public void InspectStriker()
        {
            if (striker != null && followCamera != null)
            { followCamera.SetTarget(striker.transform); followCamera.SetInspect(true); }
        }
    }
}
