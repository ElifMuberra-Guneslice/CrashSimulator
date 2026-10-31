using System;
using System.IO;
using CrashSimulator.CameraSystem;
using CrashSimulator.Input;
using CrashSimulator.UI;
using CrashSimulator.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CrashSimulator.Editor
{
    /// <summary>Editor-only construction. Players load ordinary serialized assets; no runtime factory.</summary>
    public static class TestTrackBuilder
    {
        private const string ScenePath = "Assets/Scenes/TestTrack.unity";
        private const string RootName = "CrashSimulator Test Track";
        private const string VehiclePath = "Assets/Prefabs/Vehicles/PrototypeCar.prefab";
        private const string BarrierPath = "Assets/Prefabs/Environment/ConcreteBarrier.prefab";

        [MenuItem("Crash Simulator/Prepare TestTrack")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != RootName) continue;
                Debug.Log("TestTrack is already prepared. Existing objects and tuning were preserved.");
                return;
            }

            Material asphalt = Material("Environment/Asphalt", new Color(0.12f, 0.15f, 0.18f), 0);
            Material concrete = Material("Environment/Concrete", new Color(0.53f, 0.55f, 0.54f), 0);
            Material paint = Material("Environment/TrackPaint", new Color(0.94f, 0.74f, 0.22f), 0);
            Material ground = Material("Environment/Ground", new Color(0.27f, 0.31f, 0.28f), 0);
            Material bodyMaterial = Material("Vehicles/BodyOrange", new Color(0.95f, 0.25f, 0.06f), 0.3f);
            Material glass = Material("Vehicles/OpaqueGlass", new Color(0.08f, 0.16f, 0.21f), 0.5f);
            Material rubber = Material("Vehicles/Rubber", new Color(0.035f, 0.04f, 0.05f), 0);
            Material metal = Material("Vehicles/Metal", new Color(0.48f, 0.53f, 0.58f), 0.25f);
            var settings = AssetDatabase.LoadAssetAtPath<VehicleSettings>("Assets/Settings/Vehicles/PrototypeCar.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<VehicleSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Settings/Vehicles/PrototypeCar.asset");
            }
            CreateVehiclePrefab(settings, bodyMaterial, glass, rubber, metal);
            CreateBarrierPrefab(concrete, paint);

            var trackRoot = new GameObject(RootName);
            var environment = Child("Environment", trackRoot.transform);
            Box("Ground", environment, new Vector3(0, -0.25f, 10), new Vector3(60, 0.5f, 110), ground, true);
            Box("Asphalt Lane", environment, new Vector3(0, 0.015f, 10), new Vector3(18, 0.03f, 100), asphalt, false);
            for (int i = 0; i < 15; i++)
                Box($"Center Dash {i:00}", environment, new Vector3(0, 0.035f, -36 + i * 6),
                    new Vector3(0.14f, 0.01f, 2.5f), paint, false);
            foreach (float x in new[] { -8.6f, 8.6f })
                Box("Lane Edge", environment, new Vector3(x, 0.035f, 10), new Vector3(0.12f, 0.01f, 100), paint, false);
            Box("Start Line", environment, new Vector3(0, 0.04f, -28), new Vector3(17, 0.01f, 0.25f), paint, false);
            Box("Left Boundary", environment, new Vector3(-30, 0.5f, 10), new Vector3(0.5f, 1, 110), concrete, true);
            Box("Right Boundary", environment, new Vector3(30, 0.5f, 10), new Vector3(0.5f, 1, 110), concrete, true);
            Box("Far Boundary", environment, new Vector3(0, 0.5f, 65), new Vector3(60, 1, 0.5f), concrete, true);
            Box("Near Boundary", environment, new Vector3(0, 0.5f, -45), new Vector3(60, 1, 0.5f), concrete, true);
            var barrier = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPath), environment);
            barrier.transform.position = new Vector3(0, 0, 22);
            var angled = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPath), environment);
            angled.name = "Angled Concrete Barrier";
            angled.transform.SetPositionAndRotation(new Vector3(17, 0, 38), Quaternion.Euler(0, 30, 0));
            foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            var spawn = Child("Vehicle Spawn", trackRoot.transform);
            spawn.position = new Vector3(-3, 0.15f, -32);
            var car = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePath), trackRoot.transform);
            car.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var respawn = car.GetComponent<VehicleRespawn>();
            respawn.SetSpawnPoint(spawn);
            PrefabUtility.RecordPrefabInstancePropertyModifications(respawn);

            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("TestTrack needs its Main Camera.");
            camera.farClipPlane = 180;
            camera.nearClipPlane = 0.1f;
            camera.fieldOfView = 65;
            var additional = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additional != null) additional.renderPostProcessing = false;
            var follow = camera.GetComponent<VehicleFollowCamera>();
            if (follow == null) follow = camera.gameObject.AddComponent<VehicleFollowCamera>();
            follow.SetTarget(car.transform);
            camera.transform.SetPositionAndRotation(spawn.position + new Vector3(0, 3.6f, -7),
                Quaternion.Euler(20, 0, 0));
            CreateUI(trackRoot.transform, car);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("CRASH_SIMULATOR_BUILD_OK: TestTrack, prefabs, materials and UI saved.");
        }

        private static Material Material(string relativePath, Color color, float smoothness)
        {
            string path = $"Assets/Materials/{relativePath}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) throw new InvalidOperationException("URP Simple Lit shader is unavailable.");
            var material = new Material(shader) { name = Path.GetFileName(relativePath), enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateVehiclePrefab(VehicleSettings settings, Material body, Material glass, Material rubber, Material metal)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePath) != null) return;
            var root = new GameObject("PrototypeCar");
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = settings.mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var input = root.AddComponent<PlayerVehicleInput>();
            var controller = root.AddComponent<VehicleController>();
            root.AddComponent<VehicleCollisionReporter>();
            root.AddComponent<VehicleRespawn>();
            var visuals = Child("Body", root.transform);
            Box("Chassis", visuals, new Vector3(0, 0.72f, 0), new Vector3(1.75f, 0.48f, 3.8f), body, true);
            Box("Cabin", visuals, new Vector3(0, 1.16f, -0.25f), new Vector3(1.5f, 0.5f, 1.8f), glass, true);
            Box("Roof", visuals, new Vector3(0, 1.43f, -0.25f), new Vector3(1.54f, 0.08f, 1.84f), body, false);
            Box("Front Bumper", visuals, new Vector3(0, 0.64f, 1.92f), new Vector3(1.77f, 0.16f, 0.15f), metal, false);
            Box("Rear Bumper", visuals, new Vector3(0, 0.64f, -1.92f), new Vector3(1.77f, 0.16f, 0.15f), metal, false);
            foreach (float x in new[] { -0.58f, 0.58f })
                Box("Headlight", visuals, new Vector3(x, 0.82f, 1.91f), new Vector3(0.35f, 0.14f, 0.04f), metal, false);
            var physics = Child("Wheel Colliders", root.transform);
            var meshes = Child("Wheel Visuals", root.transform);
            var wheels = new VehicleWheel[4];
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                string name = (front ? "Front" : "Rear") + (i % 2 == 0 ? "Left" : "Right");
                Vector3 position = new Vector3(i % 2 == 0 ? -0.9f : 0.9f, 0.53f, front ? 1.25f : -1.25f);
                var wheelTransform = Child(name, physics);
                wheelTransform.localPosition = position;
                var wheel = wheelTransform.gameObject.AddComponent<WheelCollider>();
                wheel.mass = 20;
                wheel.radius = settings.wheelRadius;
                wheel.suspensionDistance = settings.suspensionDistance;
                wheel.forceAppPointDistance = 0.15f;
                wheel.wheelDampingRate = 1;
                wheel.suspensionSpring = new JointSpring
                {
                    spring = settings.spring, damper = settings.damper, targetPosition = settings.suspensionTarget
                };
                var visual = Child(name, meshes);
                visual.localPosition = position - Vector3.up * settings.suspensionDistance * 0.5f;
                var tire = Primitive("Tire", PrimitiveType.Cylinder, visual, Vector3.zero,
                    new Vector3(0.68f, 0.13f, 0.68f), rubber, false);
                tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var hub = Primitive("Hub", PrimitiveType.Cylinder, visual, Vector3.zero,
                    new Vector3(0.35f, 0.135f, 0.35f), metal, false);
                hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
                Box("Hub Spoke", visual, new Vector3(i % 2 == 0 ? -0.142f : 0.142f, 0, 0),
                    new Vector3(0.015f, 0.07f, 0.3f), body, false);
                wheels[i] = new VehicleWheel { collider = wheel, visual = visual, steering = front, driven = true };
            }
            controller.Configure(settings, input, wheels);
            PrefabUtility.SaveAsPrefabAsset(root, VehiclePath);
            Object.DestroyImmediate(root);
        }

        private static void CreateBarrierPrefab(Material concrete, Material paint)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BarrierPath) != null) return;
            var root = new GameObject("ConcreteBarrier");
            Box("Base", root.transform, new Vector3(0, 0.18f, 0), new Vector3(8, 0.36f, 1.25f), concrete, true);
            Box("Wall", root.transform, new Vector3(0, 0.78f, 0), new Vector3(8, 0.84f, 0.7f), concrete, true);
            Box("Safety Stripe", root.transform, new Vector3(0, 0.94f, -0.356f), new Vector3(7.8f, 0.15f, 0.015f), paint, false);
            PrefabUtility.SaveAsPrefabAsset(root, BarrierPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateUI(Transform parent, GameObject car)
        {
            var canvasObject = new GameObject("Driving HUD", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var safe = Rect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaPanel>();
            var header = Rect("Telemetry Background", safe, new Vector2(0, 1), Vector2.one,
                Vector2.zero, new Vector2(0, 156));
            header.pivot = new Vector2(0.5f, 1);
            var headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = new Color(0.035f, 0.055f, 0.08f, 0.85f);
            headerImage.raycastTarget = false;
            var speed = Label("Speed", safe, "0 km/h", 34, TextAnchor.UpperLeft);
            Place(speed.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(230, 50));
            var impact = Label("Impact", safe, "TEST TRACK", 18, TextAnchor.UpperLeft);
            Place(impact.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -76), new Vector2(740, 40));
            var help = Label("Keyboard Help", safe, "W / S: Gaz / Geri    A / D: Direksiyon    SPACE: Fren    R: Sifirla", 17, TextAnchor.UpperLeft);
            Place(help.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -112), new Vector2(740, 36));
            var left = Hold(safe, "SOL", new Vector2(24, 24), false);
            var right = Hold(safe, "SAG", new Vector2(144, 24), false);
            var back = Hold(safe, "GERI", new Vector2(-264, 24), true);
            var brake = Hold(safe, "FREN", new Vector2(-144, 24), true);
            var gas = Hold(safe, "GAZ", new Vector2(-24, 24), true);
            var input = car.GetComponent<PlayerVehicleInput>();
            input.BindControls(gas, back, brake, left, right);
            PrefabUtility.RecordPrefabInstancePropertyModifications(input);
            var resetRect = ButtonBackground("Reset", safe);
            Place(resetRect, Vector2.one, Vector2.one, new Vector2(-24, -20), new Vector2(170, 64));
            Label("Label", resetRect, "SIFIRLA", 22, TextAnchor.MiddleCenter);
            var reset = resetRect.gameObject.AddComponent<Button>();
            reset.targetGraphic = resetRect.GetComponent<Image>();
            reset.navigation = new Navigation { mode = Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(reset.onClick, car.GetComponent<VehicleRespawn>().RequestReset);
            canvasObject.AddComponent<VehicleHud>().Configure(car.GetComponent<VehicleController>(),
                car.GetComponent<VehicleCollisionReporter>(), speed, impact);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(parent, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private static HoldButton Hold(Transform parent, string name, Vector2 position, bool rightSide)
        {
            var rect = ButtonBackground(name, parent);
            var anchor = new Vector2(rightSide ? 1 : 0, 0);
            Place(rect, anchor, anchor, position, new Vector2(104, 104));
            Label("Label", rect, name, 23, TextAnchor.MiddleCenter);
            return rect.gameObject.AddComponent<HoldButton>();
        }
        private static RectTransform ButtonBackground(string name, Transform parent)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.88f);
            return rect;
        }
        private static Text Label(string name, Transform parent, string text, int size, TextAnchor alignment)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
        private static Transform Child(string name, Transform parent)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider)
            => Primitive(name, PrimitiveType.Cube, parent, position, scale, material, collider);
        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
    }
}
