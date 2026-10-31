using CrashSimulator.Damage;
using NUnit.Framework;
using UnityEngine;
namespace CrashSimulator.Tests
{
    public class MechanicalDamageTests
    {
        [Test] public void RearDamageDoesNotArbitrarilyBreakEngine()
        {var c=MechanicalCondition.Calculate(0,1,0,0,0,Vector4.zero);Assert.That(c.Engine,Is.EqualTo(1));Assert.That(c.EngineFailed,Is.False);Assert.That(c.Brakes,Is.LessThan(1));}
        [Test] public void FrontDamageCanStopEngine()
        {var c=MechanicalCondition.Calculate(1,0,0,0,0,Vector4.zero);Assert.That(c.Engine,Is.Zero);Assert.That(c.EngineFailed,Is.True);}
        [Test] public void SideAndWheelDamageChangeAlignmentWithCorrectSign()
        {
            var left=MechanicalCondition.Calculate(0,0,.8f,0,0,new Vector4(.5f,0,0,0));
            var right=MechanicalCondition.Calculate(0,0,0,.8f,0,new Vector4(0,.5f,0,0));
            Assert.That(left.SteeringBias,Is.LessThan(0));Assert.That(right.SteeringBias,Is.GreaterThan(0));
            Assert.That(left.Engine,Is.EqualTo(1));Assert.That(left.Drag,Is.GreaterThan(0));
            Assert.That(left.Brakes,Is.LessThan(1));
        }
    }
}
