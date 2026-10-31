using CrashSimulator.CameraSystem;
using CrashSimulator.Damage;
using CrashSimulator.Input;
using CrashSimulator.Vehicle;
using UnityEngine;

namespace CrashSimulator.UI
{
    public sealed class PremiumTrackController : MonoBehaviour
    {
        [SerializeField] private GameObject freeDrive,crashLab,simulation,pause,analysis,settings;
        [SerializeField] private SideImpactScenario scenario;
        [SerializeField] private VehicleRespawn respawn;
        [SerializeField] private VehicleFollowCamera cameraRig;
        [SerializeField] private PlayerVehicleInput playerInput;
        [SerializeField] private DamageAnalysisScreen damageScreen;
        private float resumeScale=1;
        private bool simulationActive,analysisOpen,settingsOpen;
        private bool analysisFromPause;
        public void Configure(GameObject drive,GameObject lab,GameObject sim,GameObject pauseScreen,
            GameObject analysisScreen,GameObject settingsScreen,SideImpactScenario test,VehicleRespawn reset,
            VehicleFollowCamera follow,PlayerVehicleInput input,DamageAnalysisScreen report)
        {
            freeDrive=drive;crashLab=lab;simulation=sim;pause=pauseScreen;analysis=analysisScreen;
            settings=settingsScreen;scenario=test;respawn=reset;cameraRig=follow;playerInput=input;damageScreen=report;
        }
        private void Awake()
        {
            Time.timeScale=1;AudioListener.pause=false;GameUiSettings.Apply();
            GameUiSettings.ApplyCamera(cameraRig);
            bool labMode=GameSession.SelectedMode==GameMode.CrashLab;
            if(playerInput!=null)playerInput.SetInputSuspended(labMode);
            if(freeDrive!=null)freeDrive.SetActive(!labMode);
            if(crashLab!=null)crashLab.SetActive(labMode);
            if(simulation!=null)simulation.SetActive(false);
            if(pause!=null)pause.SetActive(false);
            if(analysis!=null)analysis.SetActive(false);
            if(settings!=null)settings.SetActive(false);
            simulationActive=false;analysisOpen=settingsOpen=false;
        }
        public void StartSimulation()
        {
            if(scenario==null)return;
            simulationActive=true;
            if(crashLab!=null)crashLab.SetActive(false);
            if(freeDrive!=null)freeDrive.SetActive(false);
            if(simulation!=null)simulation.SetActive(true);
            scenario.Begin();
        }
        public void ResetSimulation()
        {
            Time.timeScale=1;AudioListener.pause=false;
            if(GameSession.SelectedMode==GameMode.CrashLab && scenario!=null)scenario.ResetScenario();
            else if(respawn!=null)respawn.RequestReset();
            simulationActive=false;analysisOpen=settingsOpen=false;
            if(pause!=null)pause.SetActive(false);
            if(analysis!=null)analysis.SetActive(false);
            if(settings!=null)settings.SetActive(false);
            if(simulation!=null)simulation.SetActive(false);
            if(crashLab!=null)crashLab.SetActive(GameSession.SelectedMode==GameMode.CrashLab);
            if(freeDrive!=null)freeDrive.SetActive(GameSession.SelectedMode==GameMode.FreeDrive);
        }
        public void Pause()
        {
            if(Time.timeScale<=0)return;
            resumeScale=Time.timeScale;
            Time.timeScale=0;AudioListener.pause=true;
            if(pause!=null)pause.SetActive(true);
        }
        public void Resume()
        {
            if(pause!=null)pause.SetActive(false);
            AudioListener.pause=false;Time.timeScale=resumeScale<=0?1:resumeScale;
        }
        public void ToggleSlowMotion()
        {
            if(Time.timeScale<=0)return;
            Time.timeScale=Time.timeScale<.8f?1:.35f;
            resumeScale=Time.timeScale;
        }
        public void CycleCamera() { if(cameraRig!=null)cameraRig.CycleView(); }
        public void InspectA() { if(scenario!=null)scenario.InspectStriker(); }
        public void InspectB() { if(scenario!=null)scenario.InspectTarget(); }
        public void OpenAnalysis()
        {
            if(analysis==null)return;
            analysisFromPause=pause!=null && pause.activeSelf;
            if(!analysisFromPause && Time.timeScale>0)
            { resumeScale=Time.timeScale;Time.timeScale=0;AudioListener.pause=true; }
            if(pause!=null)pause.SetActive(false);
            analysisOpen=true;
            if(freeDrive!=null)freeDrive.SetActive(false);
            if(crashLab!=null)crashLab.SetActive(false);
            if(simulation!=null)simulation.SetActive(false);
            analysis.SetActive(true);damageScreen?.Show();
        }
        public void CloseAnalysis()
        {
            if(analysis!=null)analysis.SetActive(false);
            analysisOpen=false;RestoreActiveScreen();
            if(analysisFromPause)
            { if(pause!=null)pause.SetActive(true); }
            else
            { AudioListener.pause=false;Time.timeScale=resumeScale<=0?1:resumeScale; }
            analysisFromPause=false;
        }
        public void OpenSettings()
        {
            if(pause!=null)pause.SetActive(false);
            if(settings!=null)settings.SetActive(true);
            settingsOpen=true;
        }
        public void CloseSettings()
        {
            if(settings!=null)settings.SetActive(false);
            settingsOpen=false;
            if(Time.timeScale<=0 && pause!=null)pause.SetActive(true);
            else RestoreActiveScreen();
        }
        private void RestoreActiveScreen()
        {
            if(pause!=null && pause.activeSelf)return;
            if(analysisOpen||settingsOpen)return;
            if(simulationActive) { if(simulation!=null)simulation.SetActive(true); }
            else if(GameSession.SelectedMode==GameMode.CrashLab)
            { if(crashLab!=null)crashLab.SetActive(true); }
            else if(freeDrive!=null)freeDrive.SetActive(true);
        }
        public void ReturnToMenu()
        {
            Time.timeScale=1;AudioListener.pause=false;GameSession.ReturnToMenu();
        }
        public void ToggleCompactLabPanel(GameObject panel)
        { if(panel!=null)panel.SetActive(!panel.activeSelf); }
    }
}
