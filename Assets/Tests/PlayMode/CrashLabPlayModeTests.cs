#if UNITY_EDITOR
using System.Collections;
using CrashSimulator.Damage;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CrashSimulator.Tests
{
    public sealed class CrashLabPlayModeTests
    {
        private Scene scene;
        private SideImpactScenario scenario;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/TestTrack.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath("Assets/Scenes/TestTrack.unity");
            SceneManager.SetActiveScene(scene);
            scenario = Object.FindFirstObjectByType<SideImpactScenario>();
            Assert.That(scenario,Is.Not.Null);
            yield return new WaitForSeconds(.7f);
        }
        [UnityTearDown]
        public IEnumerator Close()
        {
            if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest]
        public IEnumerator NinetyKphSideImpactMovesBothDynamicVehiclesAndReportsDamage()
        {
            var a=scenario.Striker; var b=scenario.Target;
            Assert.That(a.Body.isKinematic,Is.False);
            Assert.That(b.Body.isKinematic,Is.False);
            scenario.Begin();
            yield return new WaitForSeconds(2.5f);
            var reportA=a.GetComponent<DamageReport>();var reportB=b.GetComponent<DamageReport>();
            Assert.That(reportA.TotalImpacts,Is.GreaterThan(0));
            Assert.That(reportB.TotalImpacts,Is.GreaterThan(0));
            var region=reportB.Snapshot().MainRegion;
            Assert.That(region==DamageRegion.Left || region==DamageRegion.Right,Is.True,region.ToString());
            Assert.That(reportA.Snapshot().LastImpulse,Is.GreaterThan(0));
            Assert.That(reportB.Snapshot().LastImpactSpeedKph,Is.GreaterThan(40));
            Assert.That(Vector3.Distance(b.transform.position,new Vector3(0,.15f,-8)),Is.GreaterThan(.3f));
            Assert.That(Quaternion.Angle(b.transform.rotation,Quaternion.Euler(0,90,0)),Is.GreaterThan(1));
        }
        [UnityTest]
        public IEnumerator ResetClearsDamageAndMeshAfterSideImpact()
        {
            scenario.Begin();
            yield return new WaitForSeconds(2.5f);
            var a=scenario.Striker;var b=scenario.Target;
            var deformation=a.GetComponentInChildren<DeformableVehicleMesh>();
            Assert.That(a.GetComponent<DamageReport>().TotalImpacts,Is.GreaterThan(0));
            Assert.That(deformation.RuntimeMesh,Is.Not.Null);
            scenario.ResetScenario();
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(a.GetComponent<DamageReport>().TotalImpacts,Is.Zero);
            Assert.That(b.GetComponent<DamageReport>().TotalImpacts,Is.Zero);
            Assert.That(a.GetComponent<VehicleDamage>().AppliedImpactCount,Is.Zero);
            Assert.That(b.GetComponent<VehicleDamage>().AppliedImpactCount,Is.Zero);
            Assert.That(a.GetComponent<MechanicalDamage>().EngineMultiplier,Is.EqualTo(1));
            Assert.That(b.GetComponent<MechanicalDamage>().EngineMultiplier,Is.EqualTo(1));
            Assert.That(deformation.LastChangedVertices,Is.Zero);
            Assert.That(DebrisBudget.ActiveCount,Is.Zero);
        }
        [UnityTest]
        public IEnumerator FasterBarrierImpactCausesMoreFrontDamageThanSlowImpact()
        {
            var car=scenario.Striker;
            car.SetInputSource(null);
            var body=car.Body; var report=car.GetComponent<DamageReport>();
            body.position=new Vector3(-3,.15f,15);
            body.rotation=Quaternion.identity;
            body.linearVelocity=Vector3.forward*5;
            yield return new WaitForSeconds(1.5f);
            Assert.That(report.TotalImpacts,Is.GreaterThan(0));
            float slow=report.Snapshot().LastSeverity;
            Assert.That(report.Snapshot().MainRegion,Is.EqualTo(DamageRegion.Front));
            car.GetComponent<CrashSimulator.Vehicle.VehicleRespawn>().RequestReset();
            yield return new WaitForFixedUpdate();
            body.position=new Vector3(-3,.15f,10);
            body.rotation=Quaternion.identity;
            body.linearVelocity=Vector3.forward*20;
            yield return new WaitForSeconds(1.3f);
            Assert.That(report.TotalImpacts,Is.GreaterThan(0));
            Assert.That(report.Snapshot().LastSeverity,Is.GreaterThan(slow));
            Assert.That(car.GetComponent<VehicleDamage>().DamageOf(DamageRegion.Front),Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator OrdinarySettlingDoesNotRecordCrashAndRepeatedResetsStayStable()
        {
            var car=scenario.Striker;
            var report=car.GetComponent<DamageReport>();
            Assert.That(report.TotalImpacts,Is.Zero);
            for(int i=0;i<3;i++)
            {
                car.GetComponent<CrashSimulator.Vehicle.VehicleRespawn>().RequestReset();
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(report.TotalImpacts,Is.Zero);
                Assert.That(car.Body.linearVelocity.magnitude,Is.LessThan(1));
                Assert.That(DebrisBudget.ActiveCount,Is.Zero);
            }
        }
        [UnityTest]
        public IEnumerator SideCrashDetachesLocalPartAndReportMatchesReporter()
        {
            scenario.Begin();
            yield return new WaitForSeconds(2.5f);
            var target=scenario.Target;
            var report=target.GetComponent<DamageReport>();
            Assert.That(report.TotalImpacts,Is.EqualTo(target.GetComponent<CrashSimulator.Vehicle.VehicleCollisionReporter>().ImpactCount));
            Assert.That(report.Snapshot().DetachedParts,Is.GreaterThan(0));
            Assert.That(target.GetComponent<VehicleDamage>().DamageOf(DamageRegion.Left)
                +target.GetComponent<VehicleDamage>().DamageOf(DamageRegion.Right),Is.GreaterThan(0));
        }
        [UnityTest]
        public IEnumerator RearBarrierImpactAndSecondImpactRemainDistinct()
        {
            var car=scenario.Striker;
            car.SetInputSource(null);
            var body=car.Body; var report=car.GetComponent<DamageReport>();
            body.position=new Vector3(-3,.15f,28);
            body.rotation=Quaternion.identity;
            body.linearVelocity=Vector3.back*12;
            yield return new WaitForSeconds(1.2f);
            Assert.That(report.TotalImpacts,Is.GreaterThan(0));
            Assert.That(report.Snapshot().MainRegion,Is.EqualTo(DamageRegion.Rear));
            int first=report.TotalImpacts;
            body.position=new Vector3(-3,.15f,14);
            body.rotation=Quaternion.identity;
            yield return new WaitForFixedUpdate();
            body.linearVelocity=Vector3.forward*12;
            yield return new WaitForSeconds(1.3f);
            Assert.That(report.TotalImpacts,Is.GreaterThan(first));
            Assert.That(car.GetComponent<VehicleDamage>().DamageOf(DamageRegion.Front),Is.GreaterThan(0));
        }
    }
}
#endif
