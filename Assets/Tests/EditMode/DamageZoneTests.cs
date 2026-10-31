using CrashSimulator.Damage;
using NUnit.Framework;
using UnityEngine;
namespace CrashSimulator.Tests
{
    public class DamageZoneTests
    {
        [Test] public void LeftContactDoesNotMatchRightZone()
        {
            var c=new ImpactContact {LocalPoint=new Vector3(-.9f,.7f,0),LocalNormal=Vector3.right};
            Assert.That(new DamageZone{region=DamageRegion.Left}.Contains(in c),Is.True);
            Assert.That(new DamageZone{region=DamageRegion.Right}.Contains(in c),Is.False);
        }
        [Test] public void ZoneThresholdResistanceProgressionAndRestore()
        {
            var zone=new DamageZone{threshold=5,resistance=2,maximumDamage=30};
            zone.Add(4); Assert.That(zone.Current,Is.Zero);
            zone.Add(25); Assert.That(zone.Current,Is.EqualTo(10));
            zone.Add(100); Assert.That(zone.Current,Is.EqualTo(30));
            Assert.That(zone.VisualState,Is.EqualTo(DamageVisualState.Severe));
            zone.Restore(); Assert.That(zone.VisualState,Is.EqualTo(DamageVisualState.Undamaged));
        }
        [Test] public void OptionalDoorZoneRequiresLocalBounds()
        {
            var zone=new DamageZone{region=DamageRegion.LeftDoor,parentRegion=DamageRegion.Left,
                useLocalBounds=true,localBounds=new Bounds(new Vector3(-.9f,1,0),new Vector3(.5f,1,1))};
            var near=new ImpactContact{LocalPoint=new Vector3(-.9f,1,0),LocalNormal=Vector3.right};
            var far=near; far.LocalPoint.z=2;
            Assert.That(zone.Contains(in near),Is.True); Assert.That(zone.Contains(in far),Is.False);
        }
        [Test] public void DuplicateContactsDoNotMultiplyRegionalDamage()
        {
            var go=new GameObject("Zone test");
            try
            {
                var damage=go.AddComponent<VehicleDamage>();
                var c=new ImpactContact{LocalPoint=new Vector3(-.9f,.7f,0),LocalNormal=Vector3.right,NormalSpeed=10};
                var data=new CollisionData{Severity=40};
                for(int i=0;i<8;i++) data.AddContact(c);
                damage.Apply(in data);
                Assert.That(damage.DamageOf(DamageRegion.Left),Is.EqualTo(.36f).Within(.001f));
                Assert.That(damage.DamageOf(DamageRegion.Right),Is.Zero);
                damage.Restore(); Assert.That(damage.DamageOf(DamageRegion.Left),Is.Zero);
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}
