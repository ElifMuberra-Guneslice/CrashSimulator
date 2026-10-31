using System.Collections.Generic;
using CrashSimulator.Damage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CrashSimulator.Vehicle;
namespace CrashSimulator.Editor
{
    public static class Phase2Setup
    {
        public const string CarPath="Assets/Prefabs/Vehicles/PrototypeCar.prefab";
        public const string QualityPath="Assets/Settings/Vehicles/MobileDamage.asset";
        [MenuItem("Crash Simulator/Phase 2/Upgrade Damage Visuals")]
        public static void InstallVisuals()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if(!AssetDatabase.IsValidFolder("Assets/Models/Vehicles")) System.IO.Directory.CreateDirectory("Assets/Models/Vehicles");
            var quality=AssetDatabase.LoadAssetAtPath<DamageQualitySettings>(QualityPath);
            if(quality==null){quality=ScriptableObject.CreateInstance<DamageQualitySettings>();AssetDatabase.CreateAsset(quality,QualityPath);}
            var root=PrefabUtility.LoadPrefabContents(CarPath);
            try
            {
                var damage=GetOrAdd<VehicleDamage>(root); damage.Configure("Prototype",quality);
                GetOrAdd<ImpactSurface>(root).category=ImpactCategory.Vehicle;
                var chassis=root.transform.Find("Body/Chassis");
                string meshPath="Assets/Models/Vehicles/DeformableChassis.asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(mesh==null){mesh=CreateBodyMesh();AssetDatabase.CreateAsset(mesh,meshPath);}
                chassis.GetComponent<MeshFilter>().sharedMesh=mesh;
                chassis.localScale=Vector3.one;
                chassis.GetComponent<BoxCollider>().size=new Vector3(1.75f,.48f,3.8f);
                GetOrAdd<DeformableVehicleMesh>(chassis.gameObject);
                // Optional mesh-swap support on a separate filter; authored damage meshes remain empty.
                GetOrAdd<DamageStateMesh>(root.transform.Find("Body/Cabin").gameObject).region=DamageRegion.Roof;
                PrefabUtility.SaveAsPrefabAsset(root,CarPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            string barrierPath="Assets/Prefabs/Environment/ConcreteBarrier.prefab";
            var barrier=PrefabUtility.LoadPrefabContents(barrierPath);
            try{GetOrAdd<ImpactSurface>(barrier).category=ImpactCategory.Concrete;PrefabUtility.SaveAsPrefabAsset(barrier,barrierPath);}
            finally{PrefabUtility.UnloadPrefabContents(barrier);}
            AssetDatabase.SaveAssets();
            Debug.Log("PHASE2_VISUALS_INSTALLED");
        }
        public static T GetOrAdd<T>(GameObject go) where T:Component
        {var value=go.GetComponent<T>();return value!=null?value:go.AddComponent<T>();}
        [MenuItem("Crash Simulator/Phase 2/Upgrade Detachable Parts")]
        public static void InstallParts()
        {
            var root=PrefabUtility.LoadPrefabContents(CarPath);
            try
            {
                var body=root.transform.Find("Body");
                Part(body.Find("Front Bumper").gameObject,VehiclePartKind.FrontBumper,DamageRegion.Front,12,.5f);
                Part(body.Find("Rear Bumper").gameObject,VehiclePartKind.RearBumper,DamageRegion.Rear,12,.5f);
                var paint=body.Find("Chassis").GetComponent<MeshRenderer>().sharedMaterial;
                NewPart(body,"Hood",new Vector3(0,.99f,1.3f),new Vector3(1.55f,.08f,1),
                    paint,VehiclePartKind.Hood,DamageRegion.Front,18,.6f);
                NewPart(body,"Trunk",new Vector3(0,.99f,-1.4f),new Vector3(1.55f,.08f,.8f),
                    paint,VehiclePartKind.Trunk,DamageRegion.Rear,16,.6f);
                foreach(int side in new[]{-1,1})
                {
                    var region=side<0?DamageRegion.Left:DamageRegion.Right;
                    NewPart(body,side<0?"Left Door":"Right Door",new Vector3(side*.9f,.83f,-.25f),
                        new Vector3(.08f,.46f,1.6f),paint,VehiclePartKind.Door,region,28,.65f);
                    NewPart(body,side<0?"Left Mirror":"Right Mirror",new Vector3(side*1.06f,1.13f,.4f),
                        new Vector3(.22f,.13f,.25f),paint,VehiclePartKind.Mirror,region,2,.4f);
                }
                // Wheels are opt-in: their visual pivots already carry the correct rolling pose.
                var wheelParent=root.transform.Find("Wheel Visuals");
                for(int i=0;i<wheelParent.childCount;i++)
                {
                    var go=wheelParent.GetChild(i).gameObject;
                    var c=GetOrAdd<SphereCollider>(go);c.radius=.34f;c.enabled=false;
                    Part(go,VehiclePartKind.Wheel,(DamageRegion)((int)DamageRegion.FrontLeftWheel+i),20,.95f);
                    go.GetComponent<DetachablePart>().wheelIndex=i;
                    go.GetComponent<DetachablePart>().allowWheelDetachment=false;
                }
                var zones=new List<DamageZone>(DamageZone.DefaultZones());
                for(int i=0;i<4;i++)zones.Add(new DamageZone
                {
                    region=(DamageRegion)((int)DamageRegion.FrontLeftWheel+i),useLocalBounds=true,requireParentFace=false,
                    localBounds=new Bounds(new Vector3(i%2==0?-.9f:.9f,.53f,i<2?1.25f:-1.25f),new Vector3(.8f,1.1f,1.5f)),
                    threshold=8,resistance=1.1f
                });
                var damage=root.GetComponent<VehicleDamage>();
                damage.Configure(damage.Identifier,damage.Quality,zones.ToArray());
                PrefabUtility.SaveAsPrefabAsset(root,CarPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("PHASE2_PARTS_INSTALLED");
        }
        [MenuItem("Crash Simulator/Phase 2/Connect Mechanical Damage")]
        public static void InstallMechanical()
        {
            var root=PrefabUtility.LoadPrefabContents(CarPath);
            try
            {
                var mechanical=GetOrAdd<MechanicalDamage>(root);
                root.GetComponent<VehicleController>().SetMechanicalSource(mechanical);
                PrefabUtility.SaveAsPrefabAsset(root,CarPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("PHASE2_MECHANICAL_INSTALLED");
        }
        private static void NewPart(Transform parent,string name,Vector3 position,Vector3 size,Material material,
            VehiclePartKind kind,DamageRegion region,float mass,float threshold)
        {
            if(parent.Find(name)!=null)return;
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<MeshRenderer>().sharedMaterial=material;
            Part(go,kind,region,mass,threshold);
        }
        private static void Part(GameObject go,VehiclePartKind kind,DamageRegion region,float mass,float threshold)
        {
            if(go.GetComponent<Collider>()==null)go.AddComponent<BoxCollider>();
            foreach(var c in go.GetComponents<Collider>())c.enabled=false;
            var rb=GetOrAdd<Rigidbody>(go);rb.mass=mass;rb.isKinematic=true;rb.detectCollisions=false;
            var part=GetOrAdd<DetachablePart>(go);part.kind=kind;part.region=region;part.detachThreshold=threshold;
            GetOrAdd<ImpactSurface>(go).category=ImpactCategory.Debris;
        }
        public static Mesh CreateBodyMesh()
        {
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var triangles=new List<int>();
            Face(vertices,normals,triangles,Vector3.forward*1.9f,Vector3.right*1.75f,Vector3.up*.48f,12,4);
            Face(vertices,normals,triangles,Vector3.back*1.9f,Vector3.left*1.75f,Vector3.up*.48f,12,4);
            Face(vertices,normals,triangles,Vector3.right*.875f,Vector3.back*3.8f,Vector3.up*.48f,24,4);
            Face(vertices,normals,triangles,Vector3.left*.875f,Vector3.forward*3.8f,Vector3.up*.48f,24,4);
            Face(vertices,normals,triangles,Vector3.up*.24f,Vector3.right*1.75f,Vector3.back*3.8f,12,24);
            Face(vertices,normals,triangles,Vector3.down*.24f,Vector3.right*1.75f,Vector3.forward*3.8f,12,24);
            var mesh=new Mesh{name="Deformable Chassis"}; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds(); return mesh;
        }
        private static void Face(List<Vector3> vertices,List<Vector3> normals,List<int> triangles,Vector3 center,Vector3 u,Vector3 v,int width,int height)
        {
            int start=vertices.Count;Vector3 normal=Vector3.Cross(u,v).normalized;
            for(int y=0;y<=height;y++)for(int x=0;x<=width;x++)
            {vertices.Add(center+u*((float)x/width-.5f)+v*((float)y/height-.5f));normals.Add(normal);}
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int a=start+y*(width+1)+x,b=a+1,c=a+width+1,d=c+1;
                triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);
            }
        }
    }
}
