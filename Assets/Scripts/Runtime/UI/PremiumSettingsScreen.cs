using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class PremiumSettingsScreen : MonoBehaviour
    {
        [SerializeField] private Slider steering,opacity,master,effects;
        [SerializeField] private Toggle leftHanded,shake,reducedMotion,mph;
        [SerializeField] private Text graphicsLabel;
        [SerializeField] private GameObject lowSelected,mediumSelected,highSelected;
        private bool listening;
        public void Configure(Slider steer,Slider buttons,Slider masterSound,Slider effectsSound,
            Toggle swapped,Toggle cameraShake,Toggle motion,Toggle speedUnit,Text quality,
            GameObject low,GameObject medium,GameObject high)
        {
            steering=steer;opacity=buttons;master=masterSound;effects=effectsSound;
            leftHanded=swapped;shake=cameraShake;reducedMotion=motion;mph=speedUnit;
            graphicsLabel=quality;lowSelected=low;mediumSelected=medium;highSelected=high;
            Bind(false);Sync();Bind(true);
        }
        private void OnEnable()
        {
            Sync(); Bind(true);
            GameUiSettings.Changed+=Sync;
        }
        private void OnDisable() { Bind(false); GameUiSettings.Changed-=Sync; }
        private void Bind(bool add)
        {
            // Inspector-time scene generation rebuilds the Canvas outside Play mode.
            // Slider/Toggle layout rebuilds may invoke UnityEvents there; only persist
            // user preferences during an actual game session.
            if(add && !Application.isPlaying)return;
            if(add==listening)return;
            listening=add;
            if(add)
            {
                if(steering!=null)steering.onValueChanged.AddListener(GameUiSettings.SetSteeringSensitivity);
                if(opacity!=null)opacity.onValueChanged.AddListener(GameUiSettings.SetControlOpacity);
                if(master!=null)master.onValueChanged.AddListener(GameUiSettings.SetMasterVolume);
                if(effects!=null)effects.onValueChanged.AddListener(GameUiSettings.SetEffectsVolume);
                if(leftHanded!=null)leftHanded.onValueChanged.AddListener(GameUiSettings.SetLeftHanded);
                if(shake!=null)shake.onValueChanged.AddListener(GameUiSettings.SetCameraShake);
                if(reducedMotion!=null)reducedMotion.onValueChanged.AddListener(GameUiSettings.SetReducedMotion);
                if(mph!=null)mph.onValueChanged.AddListener(GameUiSettings.SetMilesPerHour);
            }
            else
            {
                if(steering!=null)steering.onValueChanged.RemoveListener(GameUiSettings.SetSteeringSensitivity);
                if(opacity!=null)opacity.onValueChanged.RemoveListener(GameUiSettings.SetControlOpacity);
                if(master!=null)master.onValueChanged.RemoveListener(GameUiSettings.SetMasterVolume);
                if(effects!=null)effects.onValueChanged.RemoveListener(GameUiSettings.SetEffectsVolume);
                if(leftHanded!=null)leftHanded.onValueChanged.RemoveListener(GameUiSettings.SetLeftHanded);
                if(shake!=null)shake.onValueChanged.RemoveListener(GameUiSettings.SetCameraShake);
                if(reducedMotion!=null)reducedMotion.onValueChanged.RemoveListener(GameUiSettings.SetReducedMotion);
                if(mph!=null)mph.onValueChanged.RemoveListener(GameUiSettings.SetMilesPerHour);
            }
        }
        public void SetLow() => GameUiSettings.SetGraphics(0);
        public void SetMedium() => GameUiSettings.SetGraphics(1);
        public void SetHigh() => GameUiSettings.SetGraphics(2);
        public void Sync()
        {
            if(steering!=null)steering.SetValueWithoutNotify(GameUiSettings.SteeringSensitivity);
            if(opacity!=null)opacity.SetValueWithoutNotify(GameUiSettings.ControlOpacity);
            if(master!=null)master.SetValueWithoutNotify(GameUiSettings.MasterVolume);
            if(effects!=null)effects.SetValueWithoutNotify(GameUiSettings.EffectsVolume);
            if(leftHanded!=null)leftHanded.SetIsOnWithoutNotify(GameUiSettings.LeftHanded);
            if(shake!=null)shake.SetIsOnWithoutNotify(GameUiSettings.CameraShake);
            if(reducedMotion!=null)reducedMotion.SetIsOnWithoutNotify(GameUiSettings.ReducedMotion);
            if(mph!=null)mph.SetIsOnWithoutNotify(GameUiSettings.MilesPerHour);
            int quality=GameUiSettings.Graphics;
            if(graphicsLabel!=null)graphicsLabel.text=quality==0?"LOW":quality==1?"MEDIUM":"HIGH";
            if(lowSelected!=null)lowSelected.SetActive(quality==0);
            if(mediumSelected!=null)mediumSelected.SetActive(quality==1);
            if(highSelected!=null)highSelected.SetActive(quality==2);
        }
    }
}
