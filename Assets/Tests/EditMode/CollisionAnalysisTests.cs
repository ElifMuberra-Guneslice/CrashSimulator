using CrashSimulator.Damage;
using NUnit.Framework;
using UnityEngine;
namespace CrashSimulator.Tests
{
    public class CollisionAnalysisTests
    {
        [Test] public void MinorContactsDoNotDamage() => Assert.That(CollisionSeverityCalculator.Calculate(1, 5000, 1200), Is.Zero);
        [Test] public void SeverityScalesWithSpeedAndMass()
        {
            Assert.That(CollisionSeverityCalculator.Calculate(20, 10000, 1200), Is.GreaterThan(CollisionSeverityCalculator.Calculate(4, 1000, 1200)));
            Assert.That(CollisionSeverityCalculator.Calculate(10, 10000, 2400), Is.LessThan(CollisionSeverityCalculator.Calculate(10, 10000, 1200)));
            Assert.That(CollisionSeverityCalculator.Calculate(100, 1000000, 1200), Is.EqualTo(100));
        }
        [TestCase(-1, 1, 0, 1, 0, 0, DamageRegion.Left)]
        [TestCase(1, 1, 0, -1, 0, 0, DamageRegion.Right)]
        [TestCase(0, 1, 2, 0, 0, -1, DamageRegion.Front)]
        [TestCase(0, 1, -2, 0, 0, 1, DamageRegion.Rear)]
        [TestCase(0, 1.5f, 0, 0, -1, 0, DamageRegion.Roof)]
        [TestCase(0, 0.2f, 0, 0, 1, 0, DamageRegion.Undercarriage)]
        public void ClassifiesReceivingFace(float x,float y,float z,float nx,float ny,float nz,DamageRegion expected)
            => Assert.That(ImpactAnalyzer.RegionAt(new Vector3(x,y,z),new Vector3(nx,ny,nz)),Is.EqualTo(expected));
        [Test] public void GateRejectsDuplicatesButAllowsOtherObjectsAndLaterImpacts()
        {
            var gate=new CollisionDeduplicator();
            Assert.That(gate.Accept(1,1,.3f),Is.True);
            Assert.That(gate.Accept(1,1.1,.3f),Is.False);
            Assert.That(gate.Accept(2,1.1,.3f),Is.True);
            Assert.That(gate.Accept(1,1.5,.3f),Is.True);
            gate.Clear(); Assert.That(gate.Accept(1,1.5,.3f),Is.True);
        }
        [Test] public void ContactSnapshotIsBoundedAndSelectsStrongest()
        {
            var data=new CollisionData();
            for(int i=0;i<12;i++) data.AddContact(new ImpactContact {NormalSpeed=i,LocalPoint=Vector3.right*i});
            Assert.That(data.ContactCount,Is.EqualTo(8));
            Assert.That(data.NormalSpeed,Is.EqualTo(7));
            var copy=data; data=default;
            Assert.That(copy.GetContact(7).LocalPoint.x,Is.EqualTo(7));
        }
    }
}
