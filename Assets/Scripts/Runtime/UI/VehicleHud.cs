using CrashSimulator.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class VehicleHud : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private VehicleCollisionReporter collisions;
        [SerializeField] private Text speedText;
        [SerializeField] private Text impactText;
        private float nextUpdate;
        private int displayedCount = -1;

        public void Configure(VehicleController controller, VehicleCollisionReporter reporter,
            Text speed, Text impact)
        {
            vehicle = controller;
            collisions = reporter;
            speedText = speed;
            impactText = impact;
        }
        private void Update()
        {
            // Refresh text at 5 Hz instead of rebuilding the UI every frame.
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + 0.2f;
            if (vehicle != null && speedText != null)
                speedText.text = $"{vehicle.SpeedKph:0} km/h";
            if (collisions == null || impactText == null || displayedCount == collisions.ImpactCount) return;
            displayedCount = collisions.ImpactCount;
            impactText.text = displayedCount == 0 ? "TEST TRACK  /  CARPISMA BEKLENIYOR"
                : $"CARPISMA {displayedCount}  |  {collisions.LastImpact.RelativeSpeedKph:0} km/h  |  {collisions.LastImpact.Impulse:0} N s";
        }
    }
}
