using System;
using CrashSimulator.Vehicle;
using UnityEngine;
namespace CrashSimulator.Damage
{
    [RequireComponent(typeof(VehicleCollisionReporter))]
    public sealed class VehicleDamage : MonoBehaviour
    {
        [SerializeField] private string vehicleIdentifier = "Prototype";
        [SerializeField] private DamageQualitySettings quality;
        [SerializeField] private DamageZone[] zones = DamageZone.DefaultZones();
        private readonly CollisionData[] pending = new CollisionData[8];
        private readonly float[] regionWeights = new float[6];
        private int head, count;
        private VehicleCollisionReporter reporter;
        private VehicleRespawn respawn;
        public string Identifier => vehicleIdentifier;
        public DamageQualitySettings Quality => quality;
        public int ZoneCount => zones.Length;
        public int AppliedImpactCount { get; private set; }
        public int DroppedEvents { get; private set; }
        public CollisionData LastApplied { get; private set; }
        public event CollisionHandler DamageApplied;
        public event Action Restored;
        public event Action Destroyed;
        public DamageZone GetZone(int index) => zones[index];
        public float DamageOf(DamageRegion region)
        {
            float result=0;
            for(int i=0;i<zones.Length;i++) if(zones[i].region==region) result=Mathf.Max(result,zones[i].Normalized);
            return result;
        }
        public void Configure(string id, DamageQualitySettings settings, DamageZone[] configuration = null)
        { vehicleIdentifier=id; quality=settings; if(configuration!=null) zones=configuration; }
        private void Awake()
        {
            reporter=GetComponent<VehicleCollisionReporter>();
            respawn=GetComponent<VehicleRespawn>();
        }
        private void OnEnable()
        {
            reporter.Events.Analyzed += Enqueue;
            if(respawn!=null) respawn.Respawned += Restore;
        }
        private void OnDisable()
        {
            reporter.Events.Analyzed -= Enqueue;
            if(respawn!=null) respawn.Respawned -= Restore;
            head=count=0;
        }
        private void Enqueue(in CollisionData data)
        {
            if(count==pending.Length) { DroppedEvents++; return; }
            pending[(head+count)%pending.Length]=data; count++;
        }
        private void LateUpdate()
        {
            // At most two bounded visual/mechanical updates per rendered frame.
            for(int i=0;i<2 && count>0;i++)
            {
                var data=pending[head]; head=(head+1)%pending.Length; count--;
                Apply(in data);
            }
        }
        public void Apply(in CollisionData data)
        {
            if(data.Severity<=0 || data.ContactCount<=0) return;
            Array.Clear(regionWeights,0,regionWeights.Length);
            float total=0;
            for(int c=0;c<data.ContactCount;c++)
            {
                var contact=data.GetContact(c);
                int face=(int)ImpactAnalyzer.RegionAt(contact.LocalPoint,contact.LocalNormal);
                regionWeights[face]=Mathf.Max(regionWeights[face],Mathf.Max(.01f,contact.NormalSpeed));
            }
            for(int i=0;i<6;i++) total+=regionWeights[i];
            for(int z=0;z<zones.Length;z++)
            {
                float matched=0;
                for(int c=0;c<data.ContactCount;c++)
                {
                    var contact=data.GetContact(c);
                    if(zones[z].Contains(in contact))
                        matched=Mathf.Max(matched,regionWeights[(int)ImpactAnalyzer.RegionAt(contact.LocalPoint,contact.LocalNormal)]);
                }
                if(matched>0) zones[z].Add(data.Severity*matched/Mathf.Max(.01f,total));
            }
            AppliedImpactCount++;
            LastApplied=data;
            DamageApplied?.Invoke(in data);
        }
        public void Restore()
        {
            head=count=0; AppliedImpactCount=DroppedEvents=0; LastApplied=default;
            foreach(var zone in zones) zone.Restore();
            Restored?.Invoke();
        }
        private void OnDestroy()=>Destroyed?.Invoke();
    }
}
