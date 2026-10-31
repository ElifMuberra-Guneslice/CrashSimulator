using System;
using CrashSimulator.Input;
using UnityEngine;

namespace CrashSimulator.Vehicle
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(VehicleController))]
    public sealed class VehicleRespawn : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float minimumHeight = -8;
        private VehicleController vehicle;
        private PlayerVehicleInput playerInput;
        private VehicleCollisionReporter collisions;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool requested;
        private bool useRequestedPose;
        private Vector3 requestedPosition;
        private Quaternion requestedRotation;
        public event Action Respawned;

        public void SetSpawnPoint(Transform point) => spawnPoint = point;
        public void RequestReset() => requested = true;
        public void ResetAt(Vector3 position, Quaternion rotation)
        {
            requestedPosition = position;
            requestedRotation = rotation;
            useRequestedPose = requested = true;
        }

        private void Awake()
        {
            vehicle = GetComponent<VehicleController>();
            playerInput = GetComponent<PlayerVehicleInput>();
            collisions = GetComponent<VehicleCollisionReporter>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void FixedUpdate()
        {
            bool inputReset = playerInput != null && playerInput.ConsumeResetRequest();
            if (requested || inputReset || transform.position.y < minimumHeight)
                ResetVehicle();
        }

        private void ResetVehicle()
        {
            requested = false;
            var body = vehicle.Body;
            if (body == null) return;
            Vector3 position = useRequestedPose ? requestedPosition : spawnPoint != null ? spawnPoint.position : initialPosition;
            Quaternion rotation = useRequestedPose ? requestedRotation : spawnPoint != null ? spawnPoint.rotation : initialRotation;
            useRequestedPose = false;
            // Disable interpolation briefly so the camera never follows an old physics pose.
            var interpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            vehicle.ResetMotion();
            body.interpolation = interpolation;
            body.WakeUp();
            if (collisions != null) collisions.ClearHistory();
            Respawned?.Invoke();
        }
    }
}
