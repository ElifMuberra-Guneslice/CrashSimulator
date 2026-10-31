using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrashSimulator.UI
{
    public enum GameMode { FreeDrive, CrashLab }
    public static class GameSession
    {
        public const string MenuScene = "MainMenu";
        public const string TrackScene = "TestTrack";
        public static GameMode SelectedMode { get; private set; } = GameMode.FreeDrive;
        public static string SelectedVehicleId { get; private set; } = "Prototype";
        public static void SelectVehicle(string id)
        { if (id == "Prototype") SelectedVehicleId = id; }
        public static void SetMode(GameMode mode) => SelectedMode = mode;
        public static void ReturnToMenu()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(MenuScene);
        }
        public static IEnumerator LoadTrack(GameMode mode)
        {
            SelectedMode = mode;
            Time.timeScale = 1;
            var load = SceneManager.LoadSceneAsync(TrackScene);
            if (load == null) yield break;
            while (!load.isDone) yield return null;
        }
    }
}
