using System.Text;
using CrashSimulator.Damage;
using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class CrashReportHud : MonoBehaviour
    {
        [SerializeField] private DamageReport strikerReport, targetReport;
        [SerializeField] private Text label;
        [SerializeField] private GameObject panel;
        private readonly StringBuilder buffer = new StringBuilder(512);
        private float nextRefresh;
        public void Configure(DamageReport striker, DamageReport target, Text text, GameObject container)
        { strikerReport = striker; targetReport = target; label = text; panel = container; }
        public void Toggle() { if (panel != null) panel.SetActive(!panel.activeSelf); }
        private void Update()
        {
            if (panel == null || !panel.activeSelf || label == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            buffer.Clear();
            Append(strikerReport);
            Append(targetReport);
            label.text = buffer.ToString();
        }
        private void Append(DamageReport report)
        {
            if (report == null) return;
            var data = report.Snapshot();
            buffer.Append(data.VehicleId).Append(" | impacts ").Append(data.ImpactCount).Append('\n');
            buffer.Append("Speed ").Append(data.LastImpactSpeedKph.ToString("0"))
                .Append(" km/h | impulse ").Append(data.LastImpulse.ToString("0"))
                .Append(" N s | gameplay severity ").Append(data.LastSeverity.ToString("0"))
                .Append('\n');
            buffer.Append("Region ").Append(data.MainRegion).Append(" | direction ")
                .Append(data.LastDirection.ToString("F1")).Append(" | detached ")
                .Append(data.DetachedParts).Append('\n');
            buffer.Append("Engine ").Append((data.Mechanical.Engine*100).ToString("0"))
                .Append("% | brakes ").Append((data.Mechanical.Brakes*100).ToString("0"))
                .Append("% | steer ").Append(data.Mechanical.SteeringBias.ToString("0.0"))
                .Append(" deg | failed ").Append(data.Mechanical.EngineFailed ? "YES" : "NO")
                .Append("\n\n");
        }
    }
}
