using System;
using UnityEngine;
namespace CrashSimulator.Damage
{
    public enum VehiclePartKind { FrontBumper, RearBumper, Hood, Trunk, Door, Mirror, Wheel }
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DetachablePart : MonoBehaviour
    {
        public VehiclePartKind kind;
        public DamageRegion region;
        [Range(.1f,1)] public float detachThreshold=.6f;
        [Min(.1f)] public float contactRadius=1.5f;
        [Range(-1,3)] public int wheelIndex=-1;
        public bool allowWheelDetachment;
        private VehicleDamage owner;
        private Rigidbody body, vehicleBody;
        private Transform originalParent;
        private Vector3 localPosition, localScale, vehicleLocalPosition;
        private Quaternion localRotation;
        private Collider[] colliders, ownerColliders;
        private float expires;
        private bool initialized;
        public bool Detached { get; private set; }
        public bool ActiveDebris { get; private set; }
        public event Action<DetachablePart> Broken;
        public Rigidbody Body=>body;
        public void Initialize()
        {
            if(initialized) return;
            owner=GetComponentInParent<VehicleDamage>();
            if(owner==null) return;
            initialized=true;
            body=GetComponent<Rigidbody>(); vehicleBody=owner.GetComponent<Rigidbody>();
            originalParent=transform.parent; localPosition=transform.localPosition;
            localRotation=transform.localRotation; localScale=transform.localScale;
            vehicleLocalPosition=owner.transform.InverseTransformPoint(transform.position);
            colliders=GetComponentsInChildren<Collider>(true);
            ownerColliders=owner.GetComponentsInChildren<Collider>(true);
            body.isKinematic=true; body.detectCollisions=false;
            foreach(var c in colliders)c.enabled=false;
            owner.DamageApplied+=OnDamage;owner.Restored+=Restore;owner.Destroyed+=OwnerDestroyed;
        }
        private void Awake()=>Initialize();
        private void OnDamage(in CollisionData data)
        {
            if(Detached || owner.DamageOf(region)<detachThreshold || (wheelIndex>=0 && !allowWheelDetachment)) return;
            if(owner.Quality!=null && !owner.Quality.detachableParts) return;
            for(int i=0;i<data.ContactCount;i++)
            {
                if((data.GetContact(i).LocalPoint-vehicleLocalPosition).sqrMagnitude>contactRadius*contactRadius) continue;
                Detach(in data);return;
            }
        }
        private void Detach(in CollisionData data)
        {
            Detached=ActiveDebris=true;
            Vector3 inherited=vehicleBody!=null ? vehicleBody.GetPointVelocity(transform.position) : Vector3.zero;
            transform.SetParent(null,true);
            body.isKinematic=false; body.detectCollisions=true;
            body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity=10;
            foreach(var c in colliders)
            {
                c.enabled=true;
                // Ignore the owning car for the entire debris lifetime. No overlap kick at release.
                foreach(var parentCollider in ownerColliders)
                    if(parentCollider!=null && parentCollider!=c) Physics.IgnoreCollision(c,parentCollider,true);
            }
            float kick=Mathf.Min(2,data.Impulse.magnitude/Mathf.Max(100,vehicleBody!=null?vehicleBody.mass:1200)*.1f);
            body.linearVelocity=inherited+data.Direction*kick;
            body.angularVelocity=Vector3.ClampMagnitude((vehicleBody!=null?vehicleBody.angularVelocity:Vector3.zero)
                +Vector3.Cross(transform.position-data.Primary.Point,data.Direction)*Mathf.Min(3,data.Severity*.03f),10);
            expires=Time.time+(owner.Quality!=null?owner.Quality.debrisLifetime:12);
            DebrisBudget.Register(this,owner.Quality!=null?owner.Quality.activeDebris:8);
            Broken?.Invoke(this);
        }
        private void Update(){if(ActiveDebris && Time.time>=expires) Retire();}
        public void Retire()
        {
            if(!ActiveDebris) return;
            ActiveDebris=false;DebrisBudget.Release(this);
            body.linearVelocity=body.angularVelocity=Vector3.zero;
            body.isKinematic=true;body.detectCollisions=false;
            foreach(var c in colliders)c.enabled=false;
            gameObject.SetActive(false);
        }
        public void Restore()
        {
            if(!initialized) return;
            DebrisBudget.Release(this);
            if(!body.isKinematic) body.linearVelocity=body.angularVelocity=Vector3.zero;
            body.isKinematic=true;body.detectCollisions=false;
            foreach(var c in colliders)c.enabled=false;
            transform.SetParent(originalParent,false);
            transform.localPosition=localPosition;transform.localRotation=localRotation;transform.localScale=localScale;
            Detached=ActiveDebris=false;
            gameObject.SetActive(true);
        }
        private void OwnerDestroyed(){if(Detached) Destroy(gameObject);}
        private void OnDestroy()
        {
            DebrisBudget.Release(this);
            if(owner==null)return;
            owner.DamageApplied-=OnDamage;owner.Restored-=Restore;owner.Destroyed-=OwnerDestroyed;
        }
    }
    public static class DebrisBudget
    {
        private static readonly DetachablePart[] active=new DetachablePart[16];
        public static int ActiveCount { get {int count=0;foreach(var p in active)if(p!=null)count++;return count;} }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()=>Array.Clear(active,0,active.Length);
        public static void Register(DetachablePart part,int maximum)
        {
            maximum=Mathf.Clamp(maximum,1,active.Length);
            while(ActiveCount>=maximum)
            {
                for(int i=0;i<active.Length;i++)if(active[i]!=null){active[i].Retire();break;}
            }
            for(int i=0;i<active.Length;i++)if(active[i]==null){active[i]=part;return;}
        }
        public static void Release(DetachablePart part)
        {for(int i=0;i<active.Length;i++)if(active[i]==part)active[i]=null;}
    }
}
