using UnityEngine;
using UnityEngine.EventSystems;
namespace CrashSimulator.UI
{
    public sealed class VehiclePreviewTurntable : MonoBehaviour
    {
        [SerializeField] private float idleDegreesPerSecond = 5;
        public void Rotate(float degrees) => transform.Rotate(Vector3.up,degrees,Space.World);
        private void Update()
        {
            if(!GameUiSettings.ReducedMotion)Rotate(idleDegreesPerSecond*Time.unscaledDeltaTime);
        }
    }
}
