using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    /// <summary>Each touch is tracked independently; leaving a button releases it.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField] private Color pressedColor = new Color(0.95f, 0.55f, 0.12f, 0.95f);
        private readonly HashSet<int> pointers = new HashSet<int>();
        private Image image;
        private Color idleColor;
        public bool IsHeld => pointers.Count > 0;
        public void SetColors(Color idle, Color pressed)
        {
            idleColor = idle;
            pressedColor = pressed;
            if (image == null) image = GetComponent<Image>();
            Refresh();
        }

        private void Awake()
        {
            image = GetComponent<Image>();
            idleColor = image.color;
        }

        public void OnPointerDown(PointerEventData data)
        {
            pointers.Add(data.pointerId);
            Refresh();
        }
        public void OnPointerUp(PointerEventData data)
        {
            pointers.Remove(data.pointerId);
            Refresh();
        }
        public void OnPointerExit(PointerEventData data) => OnPointerUp(data);
        public void Release()
        {
            pointers.Clear();
            Refresh();
        }
        private void Refresh()
        {
            if (image != null) image.color = IsHeld ? pressedColor : idleColor;
        }
        private void OnDisable() => Release();
        private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        private void OnApplicationPause(bool paused) { if (paused) Release(); }
    }
}
