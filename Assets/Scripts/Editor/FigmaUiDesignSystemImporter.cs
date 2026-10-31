using CrashSimulator.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrashSimulator.Editor
{
    /// <summary>Applies the inspected Figma Make palette and type roles to the existing native uGUI.</summary>
    public static class FigmaUiDesignSystemImporter
    {
        private const string DisplayBoldPath = "Assets/_CrashSimulator/UI/Fonts/Rajdhani-Bold.ttf";
        private const string DisplayMediumPath = "Assets/_CrashSimulator/UI/Fonts/Rajdhani-Medium.ttf";
        private const string BodyFontPath = "Assets/_CrashSimulator/UI/Fonts/Inter-Variable.ttf";
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/TestTrack.unity"
        };

        [MenuItem("Crash Simulator/Figma UI/Apply inspected design tokens and type")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();
            var theme = AssetDatabase.LoadAssetAtPath<PremiumUiTheme>(PremiumUiFactory.ThemePath);
            if (theme == null)
                throw new System.InvalidOperationException("PremiumTheme.asset was not found. Build the Phase 3.5 UI library first.");

            var oldSetup = EditorSceneManager.GetSceneManagerSetup();
            ConfigureTheme(theme);
            EditorUtility.SetDirty(theme);
            ApplyToPrefabs(theme);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            foreach (string path in ScenePaths)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) ApplyToScene(path, theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (oldSetup != null && oldSetup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(oldSetup);
            else
                EditorSceneManager.OpenScene(ScenePaths[0], OpenSceneMode.Single);
            Debug.Log("Figma CrashSimulator UI tokens, licensed fonts, opaque panels and thin outlines applied to existing uGUI screens.");
        }

        private static void ConfigureTheme(PremiumUiTheme theme)
        {
            theme.background = new Color32(10, 10, 12, 255);
            theme.secondary = new Color32(18, 26, 46, 255);
            theme.surface = new Color32(42, 53, 80, 255);
            theme.primaryText = new Color32(242, 245, 250, 255);
            theme.secondaryText = new Color32(142, 154, 181, 255);
            theme.blue = new Color32(46, 139, 255, 255);
            theme.crashOrange = new Color32(255, 122, 26, 255);
            theme.success = new Color32(46, 204, 113, 255);
            theme.caution = new Color32(255, 176, 32, 255);
            theme.danger = new Color32(255, 77, 77, 255);
            theme.panelOpacity = 1f;
            theme.displayFont = AssetDatabase.LoadAssetAtPath<Font>(DisplayBoldPath);
            theme.displayFontMedium = AssetDatabase.LoadAssetAtPath<Font>(DisplayMediumPath);
            theme.uiFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            if (theme.displayFont == null || theme.displayFontMedium == null || theme.uiFont == null)
                Debug.LogWarning("One or more Figma font files could not be imported as Unity Font assets. Existing fonts will be retained for affected text.");
        }

        private static void ApplyToPrefabs(PremiumUiTheme theme)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PremiumUiFactory.PrefabFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool scale = root.GetComponent<RectTransform>() != null
                        && root.GetComponent<RectTransform>().sizeDelta.x < 250f;
                    ApplyToHierarchy(root, theme, scale);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static void ApplyToScene(string path, PremiumUiTheme theme)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) ApplyToHierarchy(root, theme, true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ApplyToHierarchy(GameObject root, PremiumUiTheme theme, bool allowFirstScale)
        {
            bool scale = false;
            if (allowFirstScale)
            {
                var scalers = root.GetComponentsInChildren<CanvasScaler>(true);
                foreach (var scaler in scalers)
                {
                    if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                        || scaler.referenceResolution.x >= 1900f) continue;
                    scale = true;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = .5f;
                }
                var rootRect = root.GetComponent<RectTransform>();
                if (scalers.Length == 0 && rootRect != null && rootRect.sizeDelta.x < 250f) scale = true;
            }

            if (scale)
                foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) ScaleRect(rect);

            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                var font = text.fontSize >= 32 ? theme.displayFont
                    : text.fontSize >= 22 ? theme.displayFontMedium : theme.uiFont;
                if (font != null) text.font = font;
                if (scale) text.fontSize = Mathf.RoundToInt(text.fontSize * 1.2f);
                text.color = Remap(text.color);
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
                image.color = Remap(image.color);

            foreach (var styled in root.GetComponentsInChildren<PremiumUiElement>(true))
                styled.Apply();
        }

        private static void ScaleRect(RectTransform rect)
        {
            rect.anchoredPosition *= 1.2f;
            rect.sizeDelta *= 1.2f;
            rect.offsetMin *= 1.2f;
            rect.offsetMax *= 1.2f;
        }

        private static Color Remap(Color color)
        {
            if (Near(color, new Color32(11, 16, 24, 255))) return new Color32(10, 10, 12, 255);
            if (Near(color, new Color32(21, 29, 41, 255))) return new Color32(18, 26, 46, 255);
            if (Near(color, new Color32(32, 43, 57, 255))) return new Color32(42, 53, 80, 255);
            if (Near(color, new Color32(244, 247, 251, 255))) return new Color32(242, 245, 250, 255);
            if (Near(color, new Color32(156, 170, 189, 255))) return new Color32(142, 154, 181, 255);
            if (Near(color, new Color32(57, 168, 255, 255))) return new Color32(46, 139, 255, 255);
            if (Near(color, new Color32(255, 118, 59, 255))) return new Color32(255, 122, 26, 255);
            if (Near(color, new Color32(55, 201, 155, 255))) return new Color32(46, 204, 113, 255);
            if (Near(color, new Color32(255, 83, 100, 255))) return new Color32(255, 77, 77, 255);
            return color;
        }

        private static bool Near(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < .005f && Mathf.Abs(a.g - b.g) < .005f && Mathf.Abs(a.b - b.b) < .005f;
    }
}
