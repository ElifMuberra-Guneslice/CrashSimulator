using CrashSimulator.CameraSystem;
using CrashSimulator.Damage;
using CrashSimulator.Input;
using CrashSimulator.UI;
using CrashSimulator.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrashSimulator.Editor
{
    public static class PremiumTrackUiBuilder
    {
        [MenuItem("Crash Simulator/Phase 3.5/Build Driving and Crash Lab UI")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            PremiumUiFactory.EnsureLibrary();
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/TestTrack.unity",OpenSceneMode.Single);
            var track=GameObject.Find("CrashSimulator Test Track");
            if(track==null)throw new System.InvalidOperationException("TestTrack base scene was not found.");
            if(track.transform.Find("Premium UI")!=null)
            { PremiumMenuBuilder.RegisterBuildScenes();Debug.Log("PHASE35_TRACK_UI_ALREADY_BUILT");return; }
            var vehicles=track.GetComponentsInChildren<VehicleController>(true);
            VehicleController striker=null,target=null;
            foreach(var vehicle in vehicles)
                if(vehicle.gameObject.name=="Target Vehicle")target=vehicle;else if(striker==null)striker=vehicle;
            if(striker==null||target==null)throw new System.InvalidOperationException("TestTrack needs both Phase 2 vehicles.");
            var playerInput=striker.GetComponent<PlayerVehicleInput>();
            var respawn=striker.GetComponent<VehicleRespawn>();
            var targetSpawn=GameObject.Find("Target Vehicle Spawn");
            if(targetSpawn!=null)
            {
                targetSpawn.transform.SetPositionAndRotation(new Vector3(12,.15f,-8),Quaternion.Euler(0,90,0));
                target.GetComponent<VehicleRespawn>().SetSpawnPoint(targetSpawn.transform);
                target.transform.SetPositionAndRotation(targetSpawn.transform.position,targetSpawn.transform.rotation);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target.GetComponent<VehicleRespawn>());
            }
            var scenario=Object.FindFirstObjectByType<SideImpactScenario>();
            var camera=Object.FindFirstObjectByType<VehicleFollowCamera>();
            var theme=PremiumUiFactory.Theme;
            foreach(var oldCanvas in track.GetComponentsInChildren<Canvas>(true))oldCanvas.gameObject.SetActive(false);
            var canvas=CreateCanvas(track.transform);
            var safe=PremiumUiFactory.Rect("Safe Area",canvas.transform);
            PremiumUiFactory.Stretch(safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaPanel>();

            var free=Screen(safe,"Free Drive HUD");
            var lab=Screen(safe,"Crash Lab Editor");
            var sim=Screen(safe,"Simulation HUD");
            var pause=Screen(safe,"Pause Modal");
            var analysis=Screen(safe,"Damage Analysis");
            var settings=Screen(safe,"Gameplay Settings");
            var trackController=canvas.AddComponent<PremiumTrackController>();
            BuildFreeDrive(free,theme,striker,playerInput,trackController);
            BuildCrashLab(lab,theme,striker,target,scenario,trackController);
            BuildSimulation(sim,theme,trackController,scenario);
            BuildPause(pause,theme,trackController);
            var analysisView=BuildAnalysis(analysis,theme,striker.GetComponent<DamageReport>(),
                target.GetComponent<DamageReport>(),trackController);
            BuildSettings(settings,theme,trackController);
            trackController.Configure(free.gameObject,lab.gameObject,sim.gameObject,pause.gameObject,
                analysis.gameObject,settings.gameObject,scenario,respawn,camera,playerInput,analysisView);
            free.gameObject.SetActive(true);lab.gameObject.SetActive(false);sim.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);analysis.gameObject.SetActive(false);settings.gameObject.SetActive(false);
            if(Object.FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            PremiumMenuBuilder.RegisterBuildScenes();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();Debug.Log("PHASE35_TRACK_UI_BUILT");
        }

        private static GameObject CreateCanvas(Transform parent)
        {
            var canvas=new GameObject("Premium UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(parent,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            return canvas;
        }
        private static RectTransform Screen(Transform parent,string name)
        { var result=PremiumUiFactory.Rect(name,parent);return PremiumUiFactory.Stretch(result,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero); }
        private static Text TextAt(Transform parent,string name,string value,int size,TextAnchor alignment,Color color,
            Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 dimensions)
        {
            var text=PremiumUiFactory.Label(parent,name,value,size,alignment,color);
            PremiumUiFactory.Place(text.rectTransform,anchor,pivot,position,dimensions);return text;
        }
        private static RectTransform Panel(Transform parent,string name,PremiumUiTheme theme,Vector2 anchor,
            Vector2 pivot,Vector2 position,Vector2 dimensions)
        {
            var panel=PremiumUiFactory.Panel(parent,name,PremiumUiKind.GlassPanel,anchor,pivot,position,dimensions);
            return panel;
        }
        private static Text Button(Transform parent,string name,string caption,PremiumUiKind kind,
            Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var button=PremiumUiFactory.Button(parent,name,caption,kind,anchor,pivot,position,size,action);
            return button.GetComponentInChildren<Text>();
        }
        private static CanvasGroup Group(Transform parent,string name,Vector2 anchor,Vector2 pivot,
            Vector2 position,Vector2 size)
        {
            var rect=PremiumUiFactory.Rect(name,parent);
            PremiumUiFactory.Place(rect,anchor,pivot,position,size);
            return rect.gameObject.AddComponent<CanvasGroup>();
        }
        private static HoldButton Hold(Transform parent,string name,string caption,PremiumUiTheme theme,
            Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            var text=Button(parent,name,caption,PremiumUiKind.SecondaryButton,anchor,pivot,position,size,null);
            text.fontSize=caption.Length>5?18:26;
            var hold=text.GetComponentInParent<Button>().gameObject.AddComponent<HoldButton>();
            hold.SetColors(new Color(theme.surface.r,theme.surface.g,theme.surface.b,GameUiSettings.ControlOpacity),
                new Color(theme.blue.r,theme.blue.g,theme.blue.b,.96f));
            return hold;
        }
        private static void BuildFreeDrive(RectTransform root,PremiumUiTheme theme,VehicleController vehicle,
            PlayerVehicleInput player,PremiumTrackController controller)
        {
            var header=Panel(root,"Telemetry Bar",theme,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-24),new Vector2(760,94));
            var mode=TextAt(header,"Mode","FREE DRIVE",18,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(26,0),new Vector2(165,52));
            var speed=TextAt(header,"Speed","000",54,TextAnchor.MiddleRight,theme.primaryText,
                new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(205,0),new Vector2(152,68));
            var unit=TextAt(header,"Speed Unit","KM/H",17,TextAnchor.MiddleLeft,theme.secondaryText,
                new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(365,-13),new Vector2(92,28));
            var gear=TextAt(header,"Direction","D",26,TextAnchor.MiddleCenter,theme.primaryText,
                new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-246,0),new Vector2(54,50));
            var status=TextAt(header,"Impact Status","NO IMPACTS",14,TextAnchor.MiddleLeft,theme.secondaryText,
                new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-170,0),new Vector2(146,36));
            var fillRect=PremiumUiFactory.Rect("Damage Fill",header);
            PremiumUiFactory.Place(fillRect,new Vector2(1,.5f),new Vector2(0,.5f),new Vector2(-21,0),new Vector2(7,45));
            var fill=fillRect.gameObject.AddComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Vertical;
            fill.color=theme.success;

            var top=Group(root,"Top Actions",new Vector2(1,1),new Vector2(1,1),new Vector2(-28,-28),new Vector2(400,64));
            Button(top.transform,"Analysis","DAMAGE",PremiumUiKind.SecondaryButton,new Vector2(0,1),new Vector2(0,1),Vector2.zero,
                new Vector2(112,60),controller.OpenAnalysis);
            Button(top.transform,"Camera","CAMERA",PremiumUiKind.SecondaryButton,new Vector2(0,1),new Vector2(0,1),new Vector2(122,0),
                new Vector2(112,60),controller.CycleCamera);
            Button(top.transform,"Reset","RESET",PremiumUiKind.SecondaryButton,new Vector2(0,1),new Vector2(0,1),new Vector2(244,0),
                new Vector2(112,60),controller.ResetSimulation);
            Button(top.transform,"Pause","PAUSE",PremiumUiKind.PrimaryButton,new Vector2(0,1),new Vector2(0,1),new Vector2(366,0),
                new Vector2(112,60),controller.Pause);

            var steering=Group(root,"Steering Controls",Vector2.zero,Vector2.zero,new Vector2(38,36),new Vector2(238,118));
            var left=Hold(steering.transform,"SOL","◀",theme,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(108,108));
            var right=Hold(steering.transform,"SAG","▶",theme,Vector2.zero,Vector2.zero,new Vector2(122,0),new Vector2(108,108));
            var pedals=Group(root,"Pedal Controls",Vector2.one,Vector2.one,new Vector2(-38,36),new Vector2(390,178));
            var reverse=Hold(pedals.transform,"GERI","REVERSE",theme,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(112,104));
            var brake=Hold(pedals.transform,"FREN","BRAKE",theme,Vector2.zero,Vector2.zero,new Vector2(124,0),new Vector2(112,104));
            var gas=Hold(pedals.transform,"GAZ","THROTTLE",theme,Vector2.zero,Vector2.zero,new Vector2(248,0),new Vector2(112,104));
            var park=Hold(pedals.transform,"HANDBRAKE","PARK",theme,new Vector2(1,1),new Vector2(1,1),new Vector2(0,-112),new Vector2(112,54));
            var hud=root.gameObject.AddComponent<PremiumDrivingHud>();
            hud.Configure(vehicle,player,speed,unit,gear,mode,status,fill,steering,pedals,gas,reverse,brake,left,right,park);
        }
        private static void BuildCrashLab(RectTransform root,PremiumUiTheme theme,VehicleController striker,
            VehicleController target,SideImpactScenario scenario,PremiumTrackController controller)
        {
            var title=Panel(root,"Lab Header",theme,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-22),new Vector2(620,78));
            TextAt(title,"Lab Title","CRASH LAB  /  SCENARIO EDITOR",24,TextAnchor.MiddleCenter,theme.primaryText,
                Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(610,72));
            var left=Panel(root,"Vehicle and Presets",theme,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(22,-25),new Vector2(292,620));
            var labUi=left.gameObject.AddComponent<CrashLabConfiguration>();
            TextAt(left,"Vehicle Heading","VEHICLE",19,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-24),new Vector2(240,38));
            Button(left,"Vehicle Card","PROTOTYPE  /  TEST VEHICLE\n1200 KG   •   SELECTED",PremiumUiKind.VehicleCard,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-72),new Vector2(248,82),labUi.SelectPrototypeVehicle);
            TextAt(left,"Target Heading","OBSTACLE",19,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-172),new Vector2(240,36));
            var vehicleTarget=Button(left,"Vehicle Target","TARGET  /  VEHICLE",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-222),new Vector2(248,58),labUi.SelectVehicle);
            var barrierTarget=Button(left,"Barrier Target","TARGET  /  BARRIER",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-286),new Vector2(248,58),labUi.SelectBarrier);
            TextAt(left,"Preset Heading","SCENARIO PRESETS",18,TextAnchor.MiddleLeft,theme.secondaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-370),new Vector2(248,34));
            Button(left,"Preset Side 90","SIDE IMPACT  /  90",PremiumUiKind.ScenarioCard,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-414),new Vector2(248,54),labUi.PresetSide90);
            Button(left,"Preset Frontal 60","FRONTAL  /  60",PremiumUiKind.ScenarioCard,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-475),new Vector2(248,54),labUi.PresetFront60);
            Button(left,"Preset Oblique 75","OBLIQUE  /  75",PremiumUiKind.ScenarioCard,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-536),new Vector2(248,54),labUi.PresetOblique75);

            var right=Panel(root,"Impact Parameters",theme,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-22,-25),new Vector2(320,620));
            TextAt(right,"Parameters Heading","IMPACT SETUP",21,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(24,-24),new Vector2(270,42));
            TextAt(right,"Obstacle Value","TARGET VEHICLE  /  PROTOTYPE",16,TextAnchor.MiddleLeft,theme.secondaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(24,-86),new Vector2(275,46));
            TextAt(right,"Vehicle Mass","VEHICLE MASS  /  "+striker.GetComponent<Rigidbody>().mass.ToString("0")+" KG",16,
                TextAnchor.MiddleLeft,theme.secondaryText,new Vector2(0,1),new Vector2(0,1),new Vector2(24,-136),new Vector2(275,36));
            TextAt(right,"Speed Label","IMPACT SPEED",17,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(24,-205),new Vector2(270,34));
            var speedValue=TextAt(right,"Speed Value","90 KM/H",18,TextAnchor.MiddleRight,theme.blue,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-24,-205),new Vector2(110,34));
            var speed=PremiumUiFactory.Slider(right,"Impact Speed",new Vector2(0,1),new Vector2(0,1),
                new Vector2(24,-250),new Vector2(270,50),10,120,scenario.SpeedKph);
            TextAt(right,"Angle Label","IMPACT ANGLE",17,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(24,-334),new Vector2(270,34));
            var angleValue=TextAt(right,"Angle Value","90°",18,TextAnchor.MiddleRight,theme.blue,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-24,-334),new Vector2(90,34));
            var angle=PremiumUiFactory.Slider(right,"Impact Angle",new Vector2(0,1),new Vector2(0,1),
                new Vector2(24,-379),new Vector2(270,50),0,180,scenario.ImpactAngle);
            var presetValue=TextAt(right,"Preset Status","CUSTOM SCENARIO",14,TextAnchor.MiddleLeft,theme.secondaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(24,-466),new Vector2(260,38));
            TextAt(right,"Mass Note","Power and resistance data are not authored yet.",14,TextAnchor.UpperLeft,theme.secondaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(24,-512),new Vector2(265,58));
            labUi.Configure(scenario,speed,angle,speedValue,angleValue,
                right.Find("Obstacle Value").GetComponent<Text>(),presetValue);
            var start=Button(root,"Start Simulation","START SIMULATION  ▶",PremiumUiKind.PrimaryButton,
                new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,30),new Vector2(360,74),controller.StartSimulation);
            start.GetComponentInParent<PremiumUiElement>().SetAccent(theme.crashOrange);
            Button(root,"Lab Reset","RESET TEST",PremiumUiKind.SecondaryButton,
                new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(-218,34),new Vector2(180,66),controller.ResetSimulation);
            Button(root,"Lab Results","ANALYSIS",PremiumUiKind.SecondaryButton,
                new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(218,34),new Vector2(180,66),controller.OpenAnalysis);
            Button(root,"Lab Pause","Ⅱ",PremiumUiKind.IconButton,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-24,-20),new Vector2(68,64),controller.Pause);
            TextAt(root,"Lab Target Name","PROTOTYPE  /  CONCRETE BARRIER",15,TextAnchor.MiddleCenter,theme.secondaryText,
                new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,116),new Vector2(510,34));
            var responsive=root.gameObject.AddComponent<ResponsiveCrashLabPanels>();
            var vehicleTab=Button(root,"Vehicle Panel Tab","VEHICLE",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(22,-108),new Vector2(150,58),responsive.ToggleVehiclePanel);
            var setupTab=Button(root,"Setup Panel Tab","SETUP",PremiumUiKind.SecondaryButton,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-22,-108),new Vector2(150,58),responsive.ToggleSetupPanel);
            responsive.Configure(left.gameObject,right.gameObject,vehicleTab.gameObject,setupTab.gameObject);
        }

        private static void BuildSimulation(RectTransform root,PremiumUiTheme theme,PremiumTrackController controller,
            SideImpactScenario scenario)
        {
            var header=Panel(root,"Simulation Bar",theme,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-24),new Vector2(900,90));
            TextAt(header,"Simulation Mode","SIMULATION",16,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(22,0),new Vector2(170,38));
            var speed=TextAt(header,"Simulation Speed","90 KM/H",36,TextAnchor.MiddleCenter,theme.primaryText,
                new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-100,0),new Vector2(200,54));
            var time=TextAt(header,"Simulation Time","00:00.0",22,TextAnchor.MiddleCenter,theme.secondaryText,
                new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(130,0),new Vector2(160,46));
            var speedHud=root.gameObject.AddComponent<SimulationHudClock>();speedHud.Configure(time);
            var actions=Group(root,"Simulation Actions",new Vector2(1,1),new Vector2(1,1),new Vector2(-24,-120),new Vector2(560,66));
            Button(actions.transform,"Sim Camera","CAMERA",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(112,60),controller.CycleCamera);
            Button(actions.transform,"Sim Slow Motion","SLOW MOTION",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(120,0),new Vector2(140,60),controller.ToggleSlowMotion);
            Button(actions.transform,"Sim Analysis","ANALYSIS",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(268,0),new Vector2(112,60),controller.OpenAnalysis);
            Button(actions.transform,"Sim Reset","RESET",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(388,0),new Vector2(102,60),controller.ResetSimulation);
            Button(root,"Sim Pause","PAUSE",PremiumUiKind.PrimaryButton,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-24,-24),new Vector2(138,62),controller.Pause);
            var replay=Button(root,"Replay Unavailable","REPLAY  /  NOT AVAILABLE",PremiumUiKind.SimulationTimeline,
                new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,38),new Vector2(400,50),null);
            var replayButton=replay.GetComponentInParent<Button>();replayButton.interactable=false;
            replay.color=theme.secondaryText;
            var speedometer=TextAt(root,"Large Speed","",22,TextAnchor.MiddleCenter,theme.primaryText,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-230,-24),new Vector2(110,90));
            var updater=root.gameObject.AddComponent<SimulationSpeedUpdater>();updater.Configure(scenario.Striker,speedometer);
        }
        private static GameObject BuildPause(RectTransform root,PremiumUiTheme theme,PremiumTrackController controller)
        {
            var shade=PremiumUiFactory.Rect("Pause Shade",root);
            PremiumUiFactory.Stretch(shade,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.72f);
            var panel=Panel(root,"Pause Panel",theme,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(570,625));
            TextAt(panel,"Pause Heading","PAUSED",36,TextAnchor.MiddleCenter,theme.primaryText,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-35),new Vector2(480,60));
            Button(panel,"Resume","RESUME  ▶",PremiumUiKind.PrimaryButton,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-125),new Vector2(420,68),controller.Resume);
            Button(panel,"Pause Settings","SETTINGS",PremiumUiKind.SecondaryButton,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-214),new Vector2(420,64),controller.OpenSettings);
            Button(panel,"Pause Analysis","DAMAGE ANALYSIS",PremiumUiKind.SecondaryButton,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-298),new Vector2(420,64),controller.OpenAnalysis);
            Button(panel,"Pause Reset","RESET TEST",PremiumUiKind.SecondaryButton,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-382),new Vector2(420,64),controller.ResetSimulation);
            Button(panel,"Main Menu","MAIN MENU",PremiumUiKind.SecondaryButton,
                new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-466),new Vector2(420,64),controller.ReturnToMenu);
            return panel.gameObject;
        }
        private static DamageAnalysisScreen BuildAnalysis(RectTransform root,PremiumUiTheme theme,
            DamageReport a,DamageReport b,PremiumTrackController controller)
        {
            var shade=PremiumUiFactory.Rect("Analysis Shade",root);
            PremiumUiFactory.Stretch(shade,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.78f);
            var panel=Panel(root,"Analysis Panel",theme,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1100,720));
            var title=TextAt(panel,"Analysis Title","DAMAGE ANALYSIS",34,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(32,-30),new Vector2(700,55));
            Button(panel,"Inspect Vehicle A","VEHICLE A",PremiumUiKind.SecondaryButton,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-230,-28),new Vector2(122,54),controller.InspectA);
            Button(panel,"Inspect Vehicle B","VEHICLE B",PremiumUiKind.SecondaryButton,
                new Vector2(1,1),new Vector2(1,1),new Vector2(-96,-28),new Vector2(122,54),controller.InspectB);
            var report=panel.gameObject.AddComponent<DamageAnalysisScreen>();
            var vehicleA=Button(panel,"Select Report A","VEHICLE A",PremiumUiKind.TabNavigation,
                new Vector2(0,1),new Vector2(0,1),new Vector2(32,-98),new Vector2(150,52),report.SelectVehicleA);
            var vehicleB=Button(panel,"Select Report B","VEHICLE B",PremiumUiKind.TabNavigation,
                new Vector2(0,1),new Vector2(0,1),new Vector2(192,-98),new Vector2(150,52),report.SelectVehicleB);
            Button(panel,"Basic Data","BASIC",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(365,-98),new Vector2(130,52),report.ShowBasic);
            Button(panel,"Advanced Data","ADVANCED",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(505,-98),new Vector2(160,52),report.ShowAdvanced);
            var basic=Panel(panel,"Basic Analysis",theme,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(32,-5),new Vector2(360,455));
            var summary=TextAt(basic,"Basic Summary","NO COLLISION DATA",23,TextAnchor.UpperLeft,theme.primaryText,
                Vector2.zero,Vector2.zero,new Vector2(22,425),new Vector2(318,410));
            var advanced=Panel(panel,"Advanced Analysis",theme,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-32,-5),new Vector2(640,455));
            var details=TextAt(advanced,"Technical Details","IMPULSE  —",19,TextAnchor.UpperLeft,theme.primaryText,
                Vector2.zero,Vector2.zero,new Vector2(18,426),new Vector2(600,250));
            var history=TextAt(advanced,"Impact History","NO IMPACT EVENTS",17,TextAnchor.UpperLeft,theme.secondaryText,
                Vector2.zero,Vector2.zero,new Vector2(18,170),new Vector2(600,220));
            Button(panel,"Retry Crash","RETRY TEST",PremiumUiKind.PrimaryButton,
                Vector2.zero,Vector2.zero,new Vector2(32,30),new Vector2(210,62),controller.ResetSimulation);
            Button(panel,"Close Analysis","CLOSE",PremiumUiKind.SecondaryButton,
                Vector2.one,Vector2.one,new Vector2(-32,-32),new Vector2(190,58),controller.CloseAnalysis);
            report.Configure(a,b,title,summary,details,history,basic.gameObject,advanced.gameObject);
            advanced.gameObject.SetActive(false);
            return report;
        }
        private static void BuildSettings(RectTransform root,PremiumUiTheme theme,PremiumTrackController controller)
        {
            var shade=PremiumUiFactory.Rect("Settings Shade",root);
            PremiumUiFactory.Stretch(shade,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.78f);
            var panel=Panel(root,"Settings Panel",theme,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(920,765));
            TextAt(panel,"Settings Title","SETTINGS",39,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(42,-28),new Vector2(780,66));
            TextAt(panel,"Graphics Heading","GRAPHICS",20,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(42,-110),new Vector2(410,38));
            var settings=panel.gameObject.AddComponent<PremiumSettingsScreen>();
            var low=PremiumUiFactory.Button(panel,"Low Quality","LOW",PremiumUiKind.SecondaryButton,new Vector2(0,1),new Vector2(0,1),new Vector2(42,-155),new Vector2(116,54),settings.SetLow);
            var mid=PremiumUiFactory.Button(panel,"Medium Quality","MED",PremiumUiKind.SecondaryButton,new Vector2(0,1),new Vector2(0,1),new Vector2(168,-155),new Vector2(116,54),settings.SetMedium);
            var high=PremiumUiFactory.Button(panel,"High Quality","HIGH",PremiumUiKind.SecondaryButton,new Vector2(0,1),new Vector2(0,1),new Vector2(294,-155),new Vector2(116,54),settings.SetHigh);
            var lowMarker=Marker(low.transform,theme.blue);var midMarker=Marker(mid.transform,theme.blue);var highMarker=Marker(high.transform,theme.blue);
            var quality=TextAt(panel,"Quality Value","MEDIUM",18,TextAnchor.MiddleLeft,theme.secondaryText,new Vector2(0,1),new Vector2(0,1),new Vector2(42,-214),new Vector2(390,34));
            var steering=SettingSlider(panel,theme,"Steering Sensitivity",new Vector2(42,-262),.5f,1.5f,1);
            var opacity=SettingSlider(panel,theme,"Button Opacity",new Vector2(42,-354),.4f,1,.7f);
            var handed=SettingToggle(panel,theme,"Swapped Control Layout",new Vector2(42,-454));
            TextAt(panel,"Audio Heading","AUDIO",20,TextAnchor.MiddleLeft,theme.blue,new Vector2(0,1),new Vector2(0,1),new Vector2(480,-110),new Vector2(385,38));
            var master=SettingSlider(panel,theme,"Master Volume",new Vector2(480,-165),0,1,1);
            var effects=SettingSlider(panel,theme,"Effects Volume",new Vector2(480,-257),0,1,1);
            TextAt(panel,"Gameplay Heading","GAMEPLAY",20,TextAnchor.MiddleLeft,theme.blue,new Vector2(0,1),new Vector2(0,1),new Vector2(480,-360),new Vector2(385,38));
            var shake=SettingToggle(panel,theme,"Camera Shake",new Vector2(480,-414));
            var motion=SettingToggle(panel,theme,"Reduced UI Motion",new Vector2(480,-490));
            var mph=SettingToggle(panel,theme,"Speed in mph",new Vector2(480,-566));
            Button(panel,"Close Settings","←  BACK",PremiumUiKind.SecondaryButton,Vector2.zero,Vector2.zero,new Vector2(42,35),new Vector2(195,62),controller.CloseSettings);
            settings.Configure(steering,opacity,master,effects,handed,shake,motion,mph,quality,lowMarker,midMarker,highMarker);
            lowMarker.SetActive(false);midMarker.SetActive(true);highMarker.SetActive(false);
        }
        private static GameObject Marker(Transform parent,Color color)
        {
            var rect=PremiumUiFactory.Rect("Selected Marker",parent);
            PremiumUiFactory.Place(rect,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,3),new Vector2(74,4));
            rect.gameObject.AddComponent<Image>().color=color;return rect.gameObject;
        }
        private static Slider SettingSlider(Transform parent,PremiumUiTheme theme,string label,Vector2 top,float min,float max,float value)
        {
            TextAt(parent,label+" Label",label,19,TextAnchor.MiddleLeft,theme.primaryText,new Vector2(0,1),new Vector2(0,1),top,new Vector2(405,32));
            return PremiumUiFactory.Slider(parent,label,new Vector2(0,1),new Vector2(0,1),top+new Vector2(0,-38),new Vector2(385,42),min,max,value);
        }
        private static Toggle SettingToggle(Transform parent,PremiumUiTheme theme,string label,Vector2 top)
        {
            TextAt(parent,label+" Label",label,19,TextAnchor.MiddleLeft,theme.primaryText,new Vector2(0,1),new Vector2(0,1),top,new Vector2(310,50));
            return PremiumUiFactory.Toggle(parent,label,new Vector2(0,1),new Vector2(0,1),top+new Vector2(320,0),false);
        }
    }


}
