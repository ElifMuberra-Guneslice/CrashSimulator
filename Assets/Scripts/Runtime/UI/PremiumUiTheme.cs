using UnityEngine;

namespace CrashSimulator.UI
{
    [CreateAssetMenu(menuName = "Crash Simulator/UI Theme")]
    public sealed class PremiumUiTheme : ScriptableObject
    {
        public Color background = new Color32(10,10,12,255);
        public Color secondary = new Color32(18,26,46,255);
        public Color surface = new Color32(42,53,80,255);
        public Color primaryText = new Color32(242,245,250,255);
        public Color secondaryText = new Color32(142,154,181,255);
        public Color blue = new Color32(46,139,255,255);
        public Color crashOrange = new Color32(255,122,26,255);
        public Color success = new Color32(46,204,113,255);
        public Color caution = new Color32(255,176,32,255);
        public Color danger = new Color32(255,77,77,255);
        public Font displayFont;
        public Font displayFontMedium;
        public Font uiFont;
        [Range(.85f,1f)] public float panelOpacity = 1f;
        [Range(.2f,.8f)] public float controlOpacity = .62f;
    }
}
