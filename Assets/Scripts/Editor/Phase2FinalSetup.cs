using CrashSimulator.CameraSystem;
using CrashSimulator.Damage;
using CrashSimulator.Input;
using CrashSimulator.UI;
using CrashSimulator.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrashSimulator.Editor
{
    public static class Phase2FinalSetup
    {
        private const string ScenePath = "Assets/Scenes/TestTrack.unity";

        [MenuItem("Crash Simulator/Phase 2/Finish Crash Lab")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Phase2Setup.InstallParts();
            Phase2Setup.InstallMechanical();
            UpgradePrefab();
            UpgradeScene();
            AssetDatabase.SaveAssets();
            Debug.Log("PHASE2_CRASH_LAB_INSTALLED");
        }

        private static void UpgradePrefab()
        {
            var car = PrefabUtility.LoadPrefabContents(Phase2Setup.CarPath);
            try
            {
                Phase2Setup.GetOrAdd<DamageReport>(car);
                Phase2Setup.GetOrAdd<ImpactEffects>(car);
                Phase2Setup.GetOrAdd<ImpactAudio>(car);
                var cabin = car.transform.Find("Body/Cabin");
                if (cabin != null) Phase2Setup.GetOrAdd<ImpactSurface>(cabin.gameObject).category = ImpactCategory.Glass;
                PrefabUtility.SaveAsPrefabAsset(car, Phase2Setup.CarPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(car); }
        }

        private static void UpgradeScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var track = GameObject.Find("CrashSimulator Test Track");
            if (track == null) throw new System.InvalidOperationException("Open the existing TestTrack scene first.");
            var cars = track.GetComponentsInChildren<VehicleController>();
            if (cars.Length == 0) throw new System.InvalidOperationException("TestTrack has no Phase 1 car.");
            var striker = cars[0];
            var target = GameObject.Find("Target Vehicle");
            if (target == null)
            {
                target = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(Phase2Setup.CarPath), track.transform);
                target.name = "Target Vehicle";
                target.transform.SetPositionAndRotation(new Vector3(0,.15f,-8), Quaternion.Euler(0,90,0));
            }
            var targetController = target.GetComponent<VehicleController>();
            targetController.SetInputSource(null);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetController);
            var targetDamage = target.GetComponent<VehicleDamage>();
            targetDamage.Configure("Target Vehicle", targetDamage.Quality);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetDamage);
            var targetSpawn = GameObject.Find("Target Vehicle Spawn");
            if (targetSpawn == null)
            {
                targetSpawn = new GameObject("Target Vehicle Spawn");
                targetSpawn.transform.SetParent(track.transform, false);
                targetSpawn.transform.SetPositionAndRotation(target.transform.position,target.transform.rotation);
            }
            var targetRespawn = target.GetComponent<VehicleRespawn>();
            targetRespawn.SetSpawnPoint(targetSpawn.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetRespawn);
            var camera = Object.FindFirstObjectByType<VehicleFollowCamera>();
            var scenarioObject = GameObject.Find("Side Impact Scenario");
            if (scenarioObject == null)
            {
                scenarioObject = new GameObject("Side Impact Scenario");
                scenarioObject.transform.SetParent(track.transform,false);
            }
            var scenario = Phase2Setup.GetOrAdd<SideImpactScenario>(scenarioObject);
            scenario.Configure(striker,targetController,striker.GetComponent<PlayerVehicleInput>(),camera);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) throw new System.InvalidOperationException("TestTrack has no driving HUD.");
            var safe = canvas.transform.Find("Safe Area");
            if (safe == null) throw new System.InvalidOperationException("TestTrack has no safe-area panel.");
            var lab = safe.Find("Crash Lab");
            if (lab == null) BuildUi(safe, scenario, striker, targetController);
            else if (lab.Find("Inspect Striker") == null)
                Button(lab,"Inspect Striker","ARACI INCELE",new Vector2(-240,-430),new Vector2(190,52),scenario.InspectStriker);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildUi(Transform safe, SideImpactScenario scenario,
            VehicleController striker, VehicleController target)
        {
            var group = new GameObject("Crash Lab", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(safe,false);
            group.anchorMin=Vector2.zero; group.anchorMax=Vector2.one; group.offsetMin=group.offsetMax=Vector2.zero;
            Button(group,"Start 90 km/h","90 km/h YAN TEST",new Vector2(-22,-100),new Vector2(210,56),scenario.Begin);
            Button(group,"Reset Test","TESTI SIFIRLA",new Vector2(-240,-100),new Vector2(190,56),scenario.ResetScenario);
            Button(group,"Inspect Target","HEDEFI INCELE",new Vector2(-438,-100),new Vector2(180,56),scenario.InspectTarget);
            var panel = new GameObject("Damage Report",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(group,false);
            panel.anchorMin=panel.anchorMax=new Vector2(1,1); panel.pivot=new Vector2(1,1);
            panel.anchoredPosition=new Vector2(-22,-170); panel.sizeDelta=new Vector2(595,245);
            panel.GetComponent<Image>().color=new Color(.025f,.04f,.055f,.9f);
            var text = new GameObject("Report Text",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(panel,false);
            text.rectTransform.anchorMin=Vector2.zero; text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=new Vector2(16,12); text.rectTransform.offsetMax=new Vector2(-12,-12);
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=16; text.color=Color.white; text.alignment=TextAnchor.UpperLeft;
            text.raycastTarget=false;
            var hud=Phase2Setup.GetOrAdd<CrashReportHud>(group.gameObject);
            hud.Configure(striker.GetComponent<DamageReport>(),target.GetComponent<DamageReport>(),text,panel.gameObject);
            Button(group,"Report Toggle","HASAR RAPORU",new Vector2(-22,-430),new Vector2(210,52),hud.Toggle);
            Button(group,"Inspect Striker","ARACI INCELE",new Vector2(-240,-430),new Vector2(190,52),scenario.InspectStriker);
            panel.gameObject.SetActive(false);
        }

        private static void Button(Transform parent,string name,string caption,Vector2 position,Vector2 size,
            UnityEngine.Events.UnityAction action)
        {
            var rect=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(1,1);
            rect.anchoredPosition=position;rect.sizeDelta=size;
            var image=rect.GetComponent<Image>();image.color=new Color(.06f,.09f,.13f,.9f);
            var button=rect.GetComponent<Button>();button.targetGraphic=image;
            button.navigation=new Navigation{mode=Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(button.onClick,action);
            var label=new GameObject("Label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(rect,false);label.rectTransform.anchorMin=Vector2.zero;
            label.rectTransform.anchorMax=Vector2.one; label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=17;
            label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            label.text=caption;
        }
    }
}
