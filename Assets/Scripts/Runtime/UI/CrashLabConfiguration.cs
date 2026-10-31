using CrashSimulator.Damage;
using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class CrashLabConfiguration : MonoBehaviour
    {
        [SerializeField] private SideImpactScenario scenario;
        [SerializeField] private Slider speed,angle;
        [SerializeField] private Text speedValue,angleValue,obstacleValue,presetValue;
        private bool bound;
        public void Configure(SideImpactScenario test,Slider speedSlider,Slider angleSlider,Text speedLabel,
            Text angleLabel,Text obstacleLabel,Text presetLabel)
        {
            scenario=test;speed=speedSlider;angle=angleSlider;speedValue=speedLabel;
            angleValue=angleLabel;obstacleValue=obstacleLabel;presetValue=presetLabel;
            Bind(false);Bind(true);Sync();
        }
        private void OnEnable() { Bind(true);Sync(); }
        private void OnDisable()=>Bind(false);
        private void Bind(bool add)
        {
            if(bound==add)return;
            bound=add;
            if(speed!=null)
            { if(add)speed.onValueChanged.AddListener(SetSpeed);else speed.onValueChanged.RemoveListener(SetSpeed); }
            if(angle!=null)
            { if(add)angle.onValueChanged.AddListener(SetAngle);else angle.onValueChanged.RemoveListener(SetAngle); }
        }
        public void SetSpeed(float value)
        { if(scenario!=null)scenario.SetSpeedKph(value);if(speedValue!=null)speedValue.text=$"{value:0} KM/H"; }
        public void SetAngle(float value)
        { if(scenario!=null)scenario.SetImpactAngle(value);if(angleValue!=null)angleValue.text=$"{value:0}°"; }
        public void SelectPrototypeVehicle()=>GameSession.SelectVehicle("Prototype");
        public void SelectVehicle()
        { if(scenario!=null)scenario.SetVehicleTarget(true);if(obstacleValue!=null)obstacleValue.text="VEHICLE TARGET  /  PROTOTYPE"; }
        public void SelectBarrier()
        { if(scenario!=null)scenario.SetVehicleTarget(false);if(obstacleValue!=null)obstacleValue.text="CONCRETE BARRIER  /  FIXED"; }
        public void PresetSide90()
        { SelectVehicle();SetSpeed(90);SetAngle(90);SetPreset("SIDE IMPACT  /  90 KM/H"); }
        public void PresetFront60()
        { SelectBarrier();SetSpeed(60);SetAngle(90);SetPreset("FRONTAL  /  60 KM/H"); }
        public void PresetOblique75()
        { SelectBarrier();SetSpeed(75);SetAngle(30);SetPreset("OBLIQUE  /  75 KM/H"); }
        private void SetPreset(string value) { if(presetValue!=null)presetValue.text=value; }
        private void Sync()
        {
            if(scenario==null)return;
            if(speed!=null)speed.SetValueWithoutNotify(scenario.SpeedKph);
            if(angle!=null)angle.SetValueWithoutNotify(scenario.ImpactAngle);
            SetSpeed(scenario.SpeedKph);SetAngle(scenario.ImpactAngle);
            if(scenario.UsesVehicleTarget)SelectVehicle();else SelectBarrier();
            SetPreset("CUSTOM SCENARIO");
        }
    }
}
