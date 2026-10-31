using System.Collections;
using CrashSimulator.Vehicle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrashSimulator.UI
{




    public sealed class PremiumMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject home,garage,settings,loading;
        [SerializeField] private Text garageSpecification,selectionStatus;
        [SerializeField] private Text soundToggleLabel;
        [SerializeField] private VehicleSettings prototype;
        private bool busy;
        public void Configure(GameObject homeScreen,GameObject garageScreen,GameObject settingsScreen,
            GameObject loadingScreen,Text specification,Text selection,VehicleSettings vehicle)
        {
            home=homeScreen;garage=garageScreen;settings=settingsScreen;loading=loadingScreen;
            garageSpecification=specification;selectionStatus=selection;prototype=vehicle;
        }
        public void ConfigureSoundLabel(Text label) => soundToggleLabel=label;
        private void OnEnable() => GameUiSettings.Changed+=RefreshSoundLabel;
        private void OnDisable() => GameUiSettings.Changed-=RefreshSoundLabel;
        private void Awake()
        {
            GameUiSettings.Apply();
            Time.timeScale=1;
        }
        private void Start()
        {
            ShowHome();
            if(garageSpecification!=null && prototype!=null)
                garageSpecification.text=$"PROTOTYPE VEHICLE\n\nCATEGORY  Test car\nMASS  {prototype.mass:0} kg\nPOWER  Not available\nTOP SPEED  {prototype.forwardSpeedKph:0} km/h\nSTEERING  {prototype.lowSpeedSteerAngle:0}°\nDAMAGE RESISTANCE  Configurable by region";
            if(selectionStatus!=null)selectionStatus.text="SELECTED  /  PROTOTYPE";
            RefreshSoundLabel();
        }
        public void ShowHome() => Show(home);
        public void ShowGarage() => Show(garage);
        public void ShowSettings() => Show(settings);
        public void SelectPrototype()
        {
            GameSession.SelectVehicle("Prototype");
            if(selectionStatus!=null)selectionStatus.text="SELECTED  /  PROTOTYPE";
        }
        public void ToggleSound() => GameUiSettings.SetSoundMuted(!GameUiSettings.SoundMuted);
        private void RefreshSoundLabel()
        { if(soundToggleLabel!=null)soundToggleLabel.text=GameUiSettings.SoundMuted?"SOUND OFF":"SOUND ON"; }
        public void StartFreeDrive() { if(!busy)StartCoroutine(Load(GameMode.FreeDrive)); }
        public void StartCrashLab() { if(!busy)StartCoroutine(Load(GameMode.CrashLab)); }
        private IEnumerator Load(GameMode mode)
        {
            busy=true;
            Show(loading);
            yield return GameSession.LoadTrack(mode);
            busy=false;
        }
        private void Show(GameObject selected)
        {
            if(home!=null)home.SetActive(selected==home);
            if(garage!=null)garage.SetActive(selected==garage);
            if(settings!=null)settings.SetActive(selected==settings);
            if(loading!=null)loading.SetActive(selected==loading);
        }
    }
}
