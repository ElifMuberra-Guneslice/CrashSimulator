using System;
using UnityEngine;
namespace CrashSimulator.Damage
{
    [Serializable]
    public sealed class DamageZone
    {
        public DamageRegion region;
        public DamageRegion parentRegion;
        public bool useLocalBounds;
        public bool requireParentFace = true;
        public Bounds localBounds;
        [Min(0)] public float threshold = 4;
        [Min(1)] public float maximumDamage = 100;
        [Min(0.1f)] public float resistance = 1;
        [NonSerialized] private float current;
        public float Current => current;
        public float Normalized => Mathf.Clamp01(current / Mathf.Max(1, maximumDamage));
        public DamageVisualState VisualState => Normalized >= .7f ? DamageVisualState.Severe
            : Normalized >= .35f ? DamageVisualState.Medium : current > 0 ? DamageVisualState.Light : DamageVisualState.Undamaged;
        public bool Contains(in ImpactContact contact)
        {
            DamageRegion face = ImpactAnalyzer.RegionAt(contact.LocalPoint, contact.LocalNormal);
            if (!useLocalBounds) return face == region;
            return (!requireParentFace || face == parentRegion) && localBounds.Contains(contact.LocalPoint);
        }
        public float Add(float severity)
        {
            float before = current;
            current = Mathf.Min(Mathf.Max(1, maximumDamage), current + Mathf.Max(0, severity - threshold) / Mathf.Max(.1f, resistance));
            return current - before;
        }
        public void Restore() => current = 0;
        public static DamageZone[] DefaultZones()
        {
            var zones = new DamageZone[6];
            for (int i=0; i<zones.Length; i++) zones[i] = new DamageZone { region=(DamageRegion)i, parentRegion=(DamageRegion)i };
            return zones;
        }
    }
}
