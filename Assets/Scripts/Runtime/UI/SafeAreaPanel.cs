using UnityEngine;

namespace CrashSimulator.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform panel;
        private Rect previous;
        private Vector2Int resolution;
#if UNITY_EDITOR
        // Explicitly simulated QA input; never included in a player build or saved in a scene.
        public static Rect? SimulatedSafeArea { get; set; }
#endif
        private void Awake() => panel = (RectTransform)transform;
        private void Update()
        {
            Rect safe = Screen.safeArea;
#if UNITY_EDITOR
            if (SimulatedSafeArea.HasValue) safe = SimulatedSafeArea.Value;
#endif
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size.x <= 0 || size.y <= 0 || (safe == previous && size == resolution)) return;
            previous = safe;
            resolution = size;
            panel.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            panel.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            panel.offsetMin = panel.offsetMax = Vector2.zero;
        }
    }
}
