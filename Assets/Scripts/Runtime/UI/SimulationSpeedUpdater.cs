using CrashSimulator.Vehicle;
using UnityEngine;
using UnityEngine.UI;
namespace CrashSimulator.UI
{
    public sealed class SimulationSpeedUpdater : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private Text label;
        private float next;
        public void Configure(VehicleController target,Text output){vehicle=target;label=output;}
        private void Update()
        {
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
            if(vehicle==null||label==null)return;
            float value=vehicle.SpeedKph;
            label.text=GameUiSettings.MilesPerHour?(value*.621371f).ToString("000")+" MPH":value.ToString("000")+" KM/H";
        }
    }
}
