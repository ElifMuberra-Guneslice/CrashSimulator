#if UNITY_EDITOR
using System.Collections;
using CrashSimulator.Damage;
using CrashSimulator.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace CrashSimulator.Tests
{
    public sealed class PremiumUiPlayModeTests
    {
        private Scene loadedScene;
        private bool originalMuted;

        [UnitySetUp]
        public IEnumerator OpenMainMenu()
        {
            Time.timeScale = 1;
            GameSession.SetMode(GameMode.FreeDrive);
            originalMuted = GameUiSettings.SoundMuted;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            loadedScene = SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");
            SceneManager.SetActiveScene(loadedScene);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator CloseScene()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
            if (GameUiSettings.SoundMuted != originalMuted)
                GameUiSettings.SetSoundMuted(originalMuted);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedScene);
            GameSession.SetMode(GameMode.FreeDrive);
        }

        [UnityTest]
        public IEnumerator MainMenuNavigationAndSoundButtonWork()
        {
            PremiumMenuController menu = null;
            foreach (var root in loadedScene.GetRootGameObjects())
                menu = root.GetComponent<PremiumMenuController>() ?? menu;
            Assert.That(menu, Is.Not.Null);
            var home = menu.transform.Find("Safe Area/Home").gameObject;
            var garage = menu.transform.Find("Safe Area/Garage").gameObject;
            var settings = menu.transform.Find("Safe Area/Settings").gameObject;

            menu.ShowGarage();
            Assert.That(garage.activeSelf, Is.True);
            Assert.That(home.activeSelf, Is.False);
            menu.ShowSettings();
            Assert.That(settings.activeSelf, Is.True);
            Assert.That(garage.activeSelf, Is.False);
            menu.ShowHome();
            Assert.That(home.activeSelf, Is.True);

            var sound = home.transform.Find("Sound Toggle").GetComponent<Button>();
            Assert.That(sound, Is.Not.Null);
            Assert.That(sound.onClick.GetPersistentEventCount(), Is.GreaterThan(0));
            sound.onClick.Invoke();
            yield return null;
            Assert.That(GameUiSettings.SoundMuted, Is.Not.EqualTo(originalMuted));
            var label = sound.GetComponentInChildren<Text>();
            Assert.That(label.text, Is.EqualTo(GameUiSettings.SoundMuted ? "SOUND OFF" : "SOUND ON"));
        }

        [UnityTest]
        public IEnumerator CrashLabSlidersStartAndResetTheExistingScenario()
        {
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedScene);
            GameSession.SetMode(GameMode.CrashLab);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/TestTrack.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            loadedScene = SceneManager.GetSceneByPath("Assets/Scenes/TestTrack.unity");
            SceneManager.SetActiveScene(loadedScene);
            yield return new WaitForSeconds(.8f);

            var scenario = Object.FindFirstObjectByType<SideImpactScenario>();
            var speed = GameObject.Find("Impact Speed").GetComponent<Slider>();
            var angle = GameObject.Find("Impact Angle").GetComponent<Slider>();
            speed.value = 75;
            angle.value = 35;
            Assert.That(scenario.SpeedKph, Is.EqualTo(75).Within(.1f));
            Assert.That(scenario.ImpactAngle, Is.EqualTo(35).Within(.1f));

            GameObject.Find("Start Simulation").GetComponent<Button>().onClick.Invoke();
            Assert.That(scenario.Running, Is.True);
            Assert.That(GameObject.Find("Simulation HUD").activeSelf, Is.True);
            Assert.That(GameObject.Find("Crash Lab Editor").activeSelf, Is.False);

            GameObject.Find("Sim Reset").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForFixedUpdate();
            Assert.That(scenario.Running, Is.False);
            Assert.That(GameObject.Find("Crash Lab Editor").activeSelf, Is.True);
            Assert.That(GameObject.Find("Simulation HUD").activeSelf, Is.False);
        }
    }
}
#endif
