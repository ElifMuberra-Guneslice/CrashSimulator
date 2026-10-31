using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class SimulationHudClock : MonoBehaviour
    {
        [SerializeField] private Text elapsed;
        private float start;
        private float nextUpdate;
        public void Configure(Text label) { elapsed=label;start=Time.time; }
        private void OnEnable() { start=Time.time; }
        private void Update()
        {
            if(elapsed==null||Time.unscaledTime<nextUpdate)return;
            nextUpdate=Time.unscaledTime+.1f;
            float seconds=Mathf.Max(0,Time.time-start);
            elapsed.text=$"{Mathf.FloorToInt(seconds/60):00}:{seconds%60:00.0}";
        }
    }
}
