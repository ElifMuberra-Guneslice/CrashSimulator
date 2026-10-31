using System.IO;
using System.Reflection;
using CrashSimulator.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrashSimulator.Editor
{
    /// <summary>Captures the actual Game View during Play mode for visual review.</summary>
    public static class MainMenuGameViewCapture
    {
        private const string RelativePath = "Documentation/UI/UnityScreenshots/MainMenu_1920x1080.png";
        private enum NavigationTestStage { None, AwaitFreeDrive, AwaitMenu, AwaitCrashLab }
        private static NavigationTestStage navigationStage;
        private static int waitFrames;

        [MenuItem("Crash Simulator/Validation/Capture Main Menu Game View")]
        private static void Capture()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("Enter Play mode and select the 1920x1080 Game View before capturing.");
                return;
            }

            var absolutePath = Path.GetFullPath(RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            ScreenCapture.CaptureScreenshot(absolutePath);
            Debug.Log($"Main Menu Game View screenshot queued: {absolutePath}");
        }

        [MenuItem("Crash Simulator/Validation/Capture Main Menu Game View", true)]
        private static bool ValidateCapture() => EditorApplication.isPlaying;

        [MenuItem("Crash Simulator/Validation/Run Main Menu Navigation Smoke Test")]
        private static void RunNavigationSmokeTest()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != GameSession.MenuScene)
            {
                Debug.LogError("Run the navigation smoke test in Play mode with MainMenu active.");
                return;
            }

            var menu = Object.FindFirstObjectByType<PremiumMenuController>(FindObjectsInactive.Include);
            if (menu == null) { Debug.LogError("PremiumMenuController is missing."); return; }

            bool screensPass = InvokeScreenButton(menu, "Garage", "garage")
                               && InvokeScreenButton(menu, "Settings Navigation", "settings");
            menu.ShowHome();

            var soundButton = FindButton("Sound");
            bool initialMuted = GameUiSettings.SoundMuted;
            soundButton?.onClick.Invoke();
            bool toggled = soundButton != null && GameUiSettings.SoundMuted != initialMuted;
            soundButton?.onClick.Invoke();
            bool restored = GameUiSettings.SoundMuted == initialMuted;

            if (!screensPass || !toggled || !restored)
            {
                Debug.LogError($"Main Menu screen/sound test FAILED. Screens={screensPass}, SoundToggle={toggled}, Restored={restored}.");
                return;
            }

            Debug.Log("Main Menu Garage, Settings and Sound UnityEvent tests PASSED.");
            var freeDrive = FindButton("Free Drive");
            if (freeDrive == null) { Debug.LogError("Free Drive button is missing."); return; }
            navigationStage = NavigationTestStage.AwaitFreeDrive;
            waitFrames = 0;
            EditorApplication.update += PollNavigationTest;
            freeDrive.onClick.Invoke();
        }

        private static bool InvokeScreenButton(PremiumMenuController menu, string buttonName, string fieldName)
        {
            var button = FindButton(buttonName);
            var field = typeof(PremiumMenuController).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (button == null || field == null) return false;
            button.onClick.Invoke();
            var selected = field.GetValue(menu) as GameObject;
            var home = typeof(PremiumMenuController).GetField("home",
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(menu) as GameObject;
            return selected != null && selected.activeSelf && home != null && !home.activeSelf;
        }

        private static Button FindButton(string objectName)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                    if (button.gameObject.name == objectName) return button;
            return null;
        }

        private static void PollNavigationTest()
        {
            var scene = SceneManager.GetActiveScene().name;
            if (navigationStage == NavigationTestStage.AwaitFreeDrive && scene == GameSession.TrackScene)
            {
                if (GameSession.SelectedMode != GameMode.FreeDrive)
                { FailNavigationTest("Free Drive did not select FreeDrive mode."); return; }
                Debug.Log("Free Drive button loaded TestTrack and selected FreeDrive: PASSED.");
                navigationStage = NavigationTestStage.AwaitMenu;
                waitFrames = 0;
                SceneManager.LoadScene(GameSession.MenuScene);
                return;
            }

            if (navigationStage == NavigationTestStage.AwaitMenu && scene == GameSession.MenuScene)
            {
                if (++waitFrames < 2) return;
                var crashLab = FindButton("Crash Lab");
                if (crashLab == null) { FailNavigationTest("Crash Lab button is missing after returning to MainMenu."); return; }
                navigationStage = NavigationTestStage.AwaitCrashLab;
                waitFrames = 0;
                crashLab.onClick.Invoke();
                return;
            }

            if (navigationStage == NavigationTestStage.AwaitCrashLab && scene == GameSession.TrackScene)
            {
                if (GameSession.SelectedMode != GameMode.CrashLab)
                { FailNavigationTest("Crash Lab did not select CrashLab mode."); return; }
                Debug.Log("Crash Lab button loaded TestTrack and selected CrashLab: PASSED.");
                navigationStage = NavigationTestStage.None;
                EditorApplication.update -= PollNavigationTest;
            }
        }

        private static void FailNavigationTest(string message)
        {
            navigationStage = NavigationTestStage.None;
            EditorApplication.update -= PollNavigationTest;
            Debug.LogError($"Main Menu navigation smoke test FAILED: {message}");
        }
    }
}
