using CrashSimulator.Damage;
using NUnit.Framework;
using UnityEngine;
namespace CrashSimulator.Tests
{
    public class DeformationTests
    {
        [Test] public void DeformationClonesLocalizesCapsAndRestoresMesh()
        {
            var go=new GameObject("Deform test",typeof(MeshFilter));
            var source=new Mesh();
            source.vertices=new[]{new Vector3(-1,0,0),new Vector3(-1,.1f,0),new Vector3(1,0,0),new Vector3(1,.1f,0)};
            source.triangles=new[]{0,1,2,1,3,2};source.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=source;
            try
            {
                var deform=go.AddComponent<DeformableVehicleMesh>();deform.Initialize();
                var data=new CollisionData{Severity=100};
                data.AddContact(new ImpactContact{Point=new Vector3(-1,0,0),Normal=Vector3.right,LocalPoint=new Vector3(-1,0,0),LocalNormal=Vector3.right,NormalSpeed=25});
                for(int i=0;i<10;i++)deform.Deform(in data);deform.Flush();
                Assert.That(deform.RuntimeMesh,Is.Not.SameAs(source));
                Assert.That(source.vertices[0].x,Is.EqualTo(-1));
                Assert.That(deform.RuntimeMesh.vertices[0].x,Is.GreaterThan(-1));
                Assert.That(deform.RuntimeMesh.vertices[0].x,Is.LessThanOrEqualTo(-.649f));
                Assert.That(deform.RuntimeMesh.vertices[2].x,Is.EqualTo(1));
                deform.Restore();Assert.That(deform.RuntimeMesh.vertices,Is.EqualTo(source.vertices));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(source);}
        }
    }
}
