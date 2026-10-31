using UnityEngine;
namespace CrashSimulator.Damage
{
    [RequireComponent(typeof(MeshFilter))]
    public sealed class DamageStateMesh : MonoBehaviour
    {
        public DamageRegion region;
        public Mesh undamaged, lightDamage, mediumDamage, severeDamage;
        private VehicleDamage owner;
        private MeshFilter filter;
        private void Awake()
        {
            owner=GetComponentInParent<VehicleDamage>(); filter=GetComponent<MeshFilter>();
            if(undamaged==null) undamaged=filter.sharedMesh;
        }
        private void OnEnable(){if(owner!=null){owner.DamageApplied+=Refresh;owner.Restored+=Restore;}}
        private void OnDisable(){if(owner!=null){owner.DamageApplied-=Refresh;owner.Restored-=Restore;}}
        private void Refresh(in CollisionData data)
        {
            float value=owner.DamageOf(region);
            Mesh selected=value>=.7f ? severeDamage : value>=.35f ? mediumDamage : value>0 ? lightDamage : undamaged;
            // Missing authored states safely fall back; never invent/download a replacement asset.
            filter.sharedMesh=selected!=null ? selected : undamaged;
        }
        private void Restore(){if(filter!=null) filter.sharedMesh=undamaged;}
    }
}
