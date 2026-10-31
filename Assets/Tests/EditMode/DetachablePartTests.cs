using CrashSimulator.Damage;
using NUnit.Framework;
using UnityEngine;
namespace CrashSimulator.Tests
{
    public class DetachablePartTests
    {
        [Test] public void PartsInheritVelocityRespectBudgetAndRestore()
        {
            var root=new GameObject("Parts test",typeof(Rigidbody));
            var quality=ScriptableObject.CreateInstance<DamageQualitySettings>();quality.activeDebris=1;
            root.GetComponent<Rigidbody>().mass=1200;
            var damage=root.AddComponent<VehicleDamage>();damage.Configure("Test",quality);
            var parts=new DetachablePart[2];
            try
            {
                for(int i=0;i<2;i++)
                {
                    var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.SetParent(root.transform);
                    go.transform.localPosition=new Vector3(i*.2f,.7f,1.9f);
                    go.AddComponent<Rigidbody>().mass=12;
                    parts[i]=go.AddComponent<DetachablePart>();parts[i].region=DamageRegion.Front;parts[i].detachThreshold=.5f;parts[i].Initialize();
                }
                var data=new CollisionData{Severity=100,Impulse=Vector3.back*12000,Direction=Vector3.back};
                data.AddContact(new ImpactContact{Point=new Vector3(0,.7f,1.9f),LocalPoint=new Vector3(0,.7f,1.9f),Normal=Vector3.back,LocalNormal=Vector3.back,NormalSpeed=25});
                root.GetComponent<Rigidbody>().linearVelocity=Vector3.forward*20;
                damage.Apply(in data);
                Assert.That(parts[0].Detached && parts[1].Detached,Is.True);
                Assert.That(DebrisBudget.ActiveCount,Is.EqualTo(1));
                Assert.That(parts[1].Body.linearVelocity.z,Is.InRange(18,20));
                Assert.That(parts[1].Body.isKinematic,Is.False);
                damage.Restore();
                Assert.That(DebrisBudget.ActiveCount,Is.Zero);
                foreach(var part in parts)
                {
                    Assert.That(part.transform.parent,Is.EqualTo(root.transform));
                    Assert.That(part.Detached,Is.False);Assert.That(part.gameObject.activeSelf,Is.True);
                    Assert.That(part.GetComponent<Collider>().enabled,Is.False);
                }
            }
            finally
            {
                foreach(var part in parts)if(part!=null && part.transform.parent==null)Object.DestroyImmediate(part.gameObject);
                Object.DestroyImmediate(root);Object.DestroyImmediate(quality);
            }
        }
    }
}
