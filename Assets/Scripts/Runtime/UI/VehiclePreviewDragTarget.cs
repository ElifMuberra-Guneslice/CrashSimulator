using UnityEngine;
using UnityEngine.EventSystems;
namespace CrashSimulator.UI
{
    public sealed class VehiclePreviewDragTarget : MonoBehaviour, IDragHandler
    {
        [SerializeField] private VehiclePreviewTurntable turntable;
        public void Configure(VehiclePreviewTurntable target) => turntable=target;
        public void OnDrag(PointerEventData data)
        { if(turntable!=null)turntable.Rotate(-data.delta.x*.4f); }
    }
}
