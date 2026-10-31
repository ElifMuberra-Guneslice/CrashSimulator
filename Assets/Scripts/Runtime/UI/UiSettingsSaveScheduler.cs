using UnityEngine;
namespace CrashSimulator.UI
{
    internal sealed class UiSettingsSaveScheduler : MonoBehaviour
    {
        private static UiSettingsSaveScheduler instance;
        private static float saveAt;
        private static bool dirty;

        internal static void Schedule()
        {
            dirty = true;
            saveAt = Time.unscaledTime + .8f;
            if (instance != null) return;
            var host = new GameObject("UI Settings Save Scheduler");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<UiSettingsSaveScheduler>();
        }

        private void Update()
        {
            if (dirty && Time.unscaledTime >= saveAt) Flush();
        }

        private void Flush()
        {
            if (!dirty) return;
            dirty = false;
            GameUiSettings.Flush();
        }

        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();
    }
}
