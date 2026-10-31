using System.Collections.Generic;
using UnityEngine;
namespace CrashSimulator.Damage
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(MeshFilter))]
    public sealed class DeformableVehicleMesh : MonoBehaviour
    {
        [SerializeField, Range(.1f,1.2f)] private float radius = .85f;
        [SerializeField, Range(0,.6f)] private float strength = .3f;
        [SerializeField, Range(.01f,.5f)] private float maximumDisplacement = .35f;
        private const float CellSize=.5f;
        private VehicleDamage damage;
        private MeshFilter filter;
        private Mesh originalMesh, mesh;
        private Vector3[] original, vertices;
        private int[] visited;
        private int stamp;
        private Dictionary<Vector3Int,int[]> cells;
        private bool dirty;
        public int LastChangedVertices { get; private set; }
        public Mesh RuntimeMesh => mesh;
        public void Initialize()
        {
            if(mesh!=null) return;
            damage=GetComponentInParent<VehicleDamage>();
            filter=GetComponent<MeshFilter>(); originalMesh=filter.sharedMesh;
            int limit=damage!=null && damage.Quality!=null ? damage.Quality.maximumMeshVertices : 2048;
            if(damage!=null && damage.Quality!=null && !damage.Quality.meshDeformation) return;
            if(originalMesh==null || !originalMesh.isReadable || originalMesh.vertexCount>limit) return;
            // One instance and one spatial index at startup, never per impact.
            mesh=Instantiate(originalMesh); mesh.name=originalMesh.name+" (Damage Instance)"; mesh.MarkDynamic();
            original=originalMesh.vertices; vertices=(Vector3[])original.Clone(); filter.sharedMesh=mesh;
            visited=new int[original.Length];
            var building=new Dictionary<Vector3Int,List<int>>();
            for(int i=0;i<original.Length;i++)
            {
                var key=Cell(original[i]);
                if(!building.TryGetValue(key,out var bucket)) building.Add(key,bucket=new List<int>());
                bucket.Add(i);
            }
            cells=new Dictionary<Vector3Int,int[]>(building.Count);
            foreach(var pair in building) cells.Add(pair.Key,pair.Value.ToArray());
            var bounds=mesh.bounds; bounds.Expand(maximumDisplacement*2); mesh.bounds=bounds;
        }
        private static Vector3Int Cell(Vector3 point) => Vector3Int.FloorToInt(point/CellSize);
        private void Awake() => Initialize();
        private void OnEnable()
        {
            if(damage==null) damage=GetComponentInParent<VehicleDamage>();
            if(damage==null) return;
            damage.DamageApplied+=Deform; damage.Restored+=Restore;
        }
        private void OnDisable()
        {
            if(damage==null) return;
            damage.DamageApplied-=Deform; damage.Restored-=Restore;
        }
        public void Deform(in CollisionData data)
        {
            if(mesh==null || (damage!=null && damage.Quality!=null && !damage.Quality.meshDeformation)) return;
            int budget=damage!=null && damage.Quality!=null ? damage.Quality.verticesPerImpact : 256;
            LastChangedVertices=0;
            if(stamp==int.MaxValue){System.Array.Clear(visited,0,visited.Length);stamp=0;}
            stamp++;
            // A vertex is changed at most once per impact even when contact patches overlap.
            for(int c=0;c<data.ContactCount && LastChangedVertices<budget;c++)
            {
                var contact=data.GetContact(c);
                var region=ImpactAnalyzer.RegionAt(contact.LocalPoint,contact.LocalNormal);
                if(damage!=null && damage.DamageOf(region)<=0) continue;
                Vector3 point=transform.InverseTransformPoint(contact.Point);
                Vector3 direction=transform.InverseTransformDirection(contact.Normal).normalized;
                float r=Mathf.Clamp(radius,.1f,1.2f);
                var min=Cell(point-Vector3.one*r); var max=Cell(point+Vector3.one*r);
                for(int x=min.x;x<=max.x && LastChangedVertices<budget;x++)
                for(int y=min.y;y<=max.y && LastChangedVertices<budget;y++)
                for(int z=min.z;z<=max.z && LastChangedVertices<budget;z++)
                {
                    if(!cells.TryGetValue(new Vector3Int(x,y,z),out var indices)) continue;
                    for(int n=0;n<indices.Length && LastChangedVertices<budget;n++)
                    {
                        int i=indices[n];
                        if(visited[i]==stamp) continue;
                        float distance=(original[i]-point).magnitude;
                        if(distance>=r) continue;
                        if(damage!=null)
                        {
                            var local=damage.transform.InverseTransformPoint(transform.TransformPoint(original[i]));
                            if((region==DamageRegion.Left && local.x>0)||(region==DamageRegion.Right && local.x<0)
                                ||(region==DamageRegion.Front && local.z<0)||(region==DamageRegion.Rear && local.z>0)) continue;
                        }
                        float falloff=1-distance/r;
                        Vector3 delta=direction*(strength*data.Severity*.01f*falloff*falloff);
                        vertices[i]=original[i]+Vector3.ClampMagnitude(vertices[i]+delta-original[i],maximumDisplacement);
                        visited[i]=stamp;
                        LastChangedVertices++; dirty=true;
                    }
                }
            }
        }
        public void Flush()
        {
            if(!dirty || mesh==null) return;
            // A bounded mesh upload once per rendered frame; colliders never reference this mesh.
            mesh.SetVertices(vertices);
            mesh.RecalculateNormals();
            dirty=false;
        }
        private void LateUpdate()=>Flush();
        public void Restore()
        {
            if(mesh==null) return;
            System.Array.Copy(original,vertices,original.Length);
            dirty=true; LastChangedVertices=0; Flush();
        }
        private void OnDestroy()
        {
            if(mesh==null) return;
            if(filter!=null) filter.sharedMesh=originalMesh;
            if(Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
