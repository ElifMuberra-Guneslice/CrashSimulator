#if UNITY_EDITOR
using System.Collections;
using CrashSimulator.CameraSystem;
using CrashSimulator.Damage;
using CrashSimulator.Input;
using CrashSimulator.UI;
using CrashSimulator.Vehicle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrashSimulator.Tests
{
    public sealed class TestVehicleInput : VehicleInputSource
    {
        public VehicleCommand Value;
        public override VehicleCommand Command => Value;
        public override void ClearInput() => Value = default;
    }

    public sealed class VehiclePlayModeTests
    {
        private VehicleController vehicle;
        private TestVehicleInput input;
        private Vector3 spawn;
        private Scene testScene;

        [UnitySetUp]
        public IEnumerator LoadTrack()
        {
            Time.timeScale = 1;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/TestTrack.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            testScene = SceneManager.GetSceneByPath("Assets/Scenes/TestTrack.unity");
            SceneManager.SetActiveScene(testScene);
            vehicle = Object.FindFirstObjectByType<SideImpactScenario>()?.Striker;
            Assert.That(vehicle, Is.Not.Null);
            spawn = vehicle.transform.position;
            yield return new WaitForSeconds(1.2f);
        }

        [UnityTearDown]
        public IEnumerator UnloadTrack()
        {
            Time.timeScale = 1;
            if (testScene.IsValid() && testScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(testScene);
        }

        private void UseTestInput()
        {
            input = vehicle.gameObject.AddComponent<TestVehicleInput>();
            var serialized = new UnityEditor.SerializedObject(vehicle);
            serialized.FindProperty("input").objectReferenceValue = input;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [UnityTest]
        public IEnumerator SuspensionSettlesAndFourWheelsAreGrounded()
        {
            Assert.That(vehicle.GetComponentsInChildren<WheelCollider>().Length, Is.EqualTo(4));
            foreach (var wheel in vehicle.GetComponentsInChildren<WheelCollider>())
                Assert.That(wheel.isGrounded, Is.True, wheel.name);
            Assert.That(vehicle.SpeedKph, Is.LessThan(1));
            Assert.That(vehicle.transform.up.y, Is.GreaterThan(0.98f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AcceleratorMovesForwardAndBrakeStops()
        {
            UseTestInput();
            input.Value = new VehicleCommand { Throttle = 1 };
            yield return new WaitForSeconds(2);
            Assert.That(vehicle.ForwardSpeed, Is.GreaterThan(3));
            Assert.That(vehicle.transform.position.z, Is.GreaterThan(spawn.z + 3));
            input.Value = new VehicleCommand { Brake = 1 };
            yield return new WaitForSeconds(2);
            Assert.That(vehicle.SpeedKph, Is.LessThan(2));
            Assert.That(vehicle.GetComponent<DamageReport>().TotalImpacts, Is.Zero);
        }

        [UnityTest]
        public IEnumerator OppositeThrottleBrakesBeforeReversing()
        {
            UseTestInput();
            input.Value = new VehicleCommand { Throttle = 1 };
            yield return new WaitForSeconds(1.5f);
            input.Value = new VehicleCommand { Throttle = -1 };
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            foreach (var wheel in vehicle.GetComponentsInChildren<WheelCollider>())
            {
                Assert.That(wheel.motorTorque, Is.Zero);
                Assert.That(wheel.brakeTorque, Is.GreaterThan(0));
            }
            yield return new WaitForSeconds(2);
            Assert.That(vehicle.ForwardSpeed, Is.LessThan(-1));
        }

        [UnityTest]
        public IEnumerator SteeringChangesVehicleHeading()
        {
            UseTestInput();
            input.Value = new VehicleCommand { Throttle = 0.6f, Steering = 0.5f };
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Angle(Vector3.forward, vehicle.transform.forward), Is.GreaterThan(10));
            Assert.That(vehicle.transform.up.y, Is.GreaterThan(0.7f));
        }

        [UnityTest]
        public IEnumerator BarrierImpactIsReported()
        {
            vehicle.Body.position = new Vector3(-3, 0.05f, 17);
            vehicle.Body.linearVelocity = Vector3.forward * 12;
            var reporter = vehicle.GetComponent<VehicleCollisionReporter>();
            reporter.ClearHistory();
            // The first contact arrives after the reporter's reset cooldown.
            vehicle.Body.position = new Vector3(-3, 0.05f, 14);
            yield return new WaitForSeconds(1.2f);
            Assert.That(reporter.ImpactCount, Is.GreaterThan(0));
            Assert.That(reporter.LastImpact.Impulse, Is.GreaterThan(0));
            Assert.That(vehicle.transform.position.z, Is.LessThan(22));
        }

        [UnityTest]
        public IEnumerator ResetRestoresPoseVelocityWheelsAndCamera()
        {
            vehicle.Body.position = new Vector3(10, 2, 10);
            vehicle.Body.rotation = Quaternion.Euler(0, 110, 90);
            vehicle.Body.linearVelocity = Vector3.one * 8;
            vehicle.Body.angularVelocity = Vector3.one * 4;
            vehicle.GetComponent<VehicleRespawn>().RequestReset();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(Vector3.Distance(vehicle.transform.position, spawn), Is.LessThan(0.2f));
            Assert.That(vehicle.Body.linearVelocity.magnitude, Is.LessThan(1));
            Assert.That(vehicle.Body.angularVelocity.magnitude, Is.LessThan(0.1f));
            Assert.That(vehicle.transform.up.y, Is.GreaterThan(0.99f));
            var camera = Object.FindFirstObjectByType<VehicleFollowCamera>();
            Assert.That(Vector3.Distance(camera.transform.position, spawn), Is.LessThan(9));
            foreach (var wheel in vehicle.GetComponentsInChildren<WheelCollider>())
            {
                Assert.That(wheel.motorTorque, Is.Zero);
                Assert.That(wheel.steerAngle, Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator FallingBelowTrackAutomaticallyResets()
        {
            vehicle.Body.position = new Vector3(0, -12, 0);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(Vector3.Distance(vehicle.transform.position, spawn), Is.LessThan(0.2f));
        }

        [UnityTest]
        public IEnumerator TouchButtonsCombineAndReleaseWithoutSticking()
        {
            var player = vehicle.GetComponent<PlayerVehicleInput>();
            var gas = GameObject.Find("GAZ").GetComponent<HoldButton>();
            var right = GameObject.Find("SAG").GetComponent<HoldButton>();
            var touch1 = new PointerEventData(EventSystem.current) { pointerId = 11 };
            var touch2 = new PointerEventData(EventSystem.current) { pointerId = 12 };
            gas.OnPointerDown(touch1);
            right.OnPointerDown(touch2);
            yield return null;
            Assert.That(player.Command.Throttle, Is.EqualTo(1));
            Assert.That(player.Command.Steering, Is.EqualTo(1));
            gas.OnPointerExit(touch1);
            yield return null;
            Assert.That(player.Command.Throttle, Is.Zero);
            Assert.That(player.Command.Steering, Is.EqualTo(1));
            player.enabled = false;
            Assert.That(gas.IsHeld || right.IsHeld, Is.False);
            Assert.That(player.Command.Steering, Is.Zero);
        }
    }
}
#endif
