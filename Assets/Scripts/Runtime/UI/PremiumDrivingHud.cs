using CrashSimulator.Damage;
using CrashSimulator.Input;
using CrashSimulator.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class PremiumDrivingHud : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private VehicleDamage damage;
        [SerializeField] private VehicleCollisionReporter collisions;
        [SerializeField] private Text speed,unit,direction,mode,impact;
        [SerializeField] private Image damageFill;
        [SerializeField] private CanvasGroup steeringGroup,pedalGroup;
        [SerializeField] private HoldButton gas,reverse,brake,left,right,handbrake;
        [SerializeField] private PlayerVehicleInput playerInput;
        private float refreshAt;
        public void Configure(VehicleController car,PlayerVehicleInput input,Text speedValue,Text speedUnit,
            Text driveDirection,Text modeLabel,Text impactLabel,Image fill,CanvasGroup steeringControls,
            CanvasGroup pedals,HoldButton accelerator,HoldButton back,HoldButton stop,HoldButton steerLeft,
            HoldButton steerRight,HoldButton park)
        {
            vehicle=car;playerInput=input;damage=car.GetComponent<VehicleDamage>();collisions=car.GetComponent<VehicleCollisionReporter>();
            speed=speedValue;unit=speedUnit;direction=driveDirection;mode=modeLabel;impact=impactLabel;damageFill=fill;
            steeringGroup=steeringControls;pedalGroup=pedals;gas=accelerator;reverse=back;brake=stop;
            left=steerLeft;right=steerRight;handbrake=park;
            BindInput();
            ApplySettings();
        }
        private void Awake() => BindInput();
        private void BindInput()
        {
            if(playerInput==null && vehicle!=null)
                playerInput=vehicle.GetComponent<PlayerVehicleInput>();
            if(playerInput==null || gas==null)return;
            playerInput.BindControls(gas,reverse,brake,left,right);
            playerInput.BindHandbrake(handbrake);
        }
        private void OnEnable() { GameUiSettings.Changed+=ApplySettings;ApplySettings(); }
        private void OnDisable() => GameUiSettings.Changed-=ApplySettings;
        private void ApplySettings()
        {
            if(steeringGroup!=null)steeringGroup.alpha=GameUiSettings.ControlOpacity;
            if(pedalGroup!=null)pedalGroup.alpha=GameUiSettings.ControlOpacity;
            bool leftHand=GameUiSettings.LeftHanded;
            Anchor(steeringGroup,leftHand?1:0,leftHand?new Vector2(-38,36):new Vector2(38,36));
            Anchor(pedalGroup,leftHand?0:1,leftHand?new Vector2(38,36):new Vector2(-38,36));
        }
        private static void Anchor(CanvasGroup group,int side,Vector2 position)
        {
            if(group==null)return;
            var rect=(RectTransform)group.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(side,0);rect.pivot=new Vector2(side,0);
            rect.anchoredPosition=position;
        }
        private void Update()
        {
            if(Time.unscaledTime<refreshAt)return;
            refreshAt=Time.unscaledTime+.1f;
            if(vehicle==null)return;
            bool mph=GameUiSettings.MilesPerHour;
            if(speed!=null)speed.text=(mph?vehicle.SpeedKph*.621371f:vehicle.SpeedKph).ToString("000");
            if(unit!=null)unit.text=mph?"MPH":"KM/H";
            if(direction!=null)direction.text=vehicle.ForwardSpeed<-.5f?"R":"D";
            if(mode!=null)mode.text=GameSession.SelectedMode==GameMode.FreeDrive?"FREE DRIVE":"CRASH LAB";
            if(damageFill!=null && damage!=null)
            {
                float worst=0;
                for(int i=0;i<damage.ZoneCount;i++)worst=Mathf.Max(worst,damage.GetZone(i).Normalized);
                damageFill.fillAmount=worst;
                damageFill.color=worst>.7f?new Color32(255,83,100,255):worst>.25f?new Color32(255,118,59,255):new Color32(55,201,155,255);
            }
            if(impact!=null && collisions!=null)
                impact.text=collisions.ImpactCount==0?"NO IMPACTS":"IMPACT  /  "+collisions.LastImpact.RelativeSpeedKph.ToString("0")+(mph?" MPH":" KM/H");
        }
    }
}
