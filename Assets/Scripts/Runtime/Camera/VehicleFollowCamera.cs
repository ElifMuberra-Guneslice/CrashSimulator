using CrashSimulator.Vehicle;
using CrashSimulator.Damage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrashSimulator.CameraSystem
{
    [DefaultExecutionOrder(100)]
    public sealed class VehicleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 3.6f, -7);
        [SerializeField] private float lookHeight = 1;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.16f;
        [SerializeField] private LayerMask obstacleMask = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Vector3 velocity;
        private Vector3 heading = Vector3.forward;
        private VehicleRespawn respawn;
        private VehicleDamage damage;
        private bool inspecting;
        private float orbitYaw, shakeUntil, shakeStrength;
        private bool shakeEnabled = true;
        private int viewIndex;
        private readonly Vector3[] views = { new Vector3(0,3.6f,-7), new Vector3(0,1.2f,1.4f), new Vector3(0,6,-11) };

        public void SetTarget(Transform vehicle)
        {
            Unbind(); target = vehicle; Bind(); Snap();
        }
        public void SetInspect(bool enabled) { inspecting = enabled; orbitYaw = 0; Snap(); }
        public void Orbit(float degrees) { if (inspecting) orbitYaw += degrees; }
        public void SetShakeEnabled(bool enabled) => shakeEnabled = enabled;
        public void CycleView()
        {
            viewIndex = (viewIndex+1)%views.Length;
            offset = views[viewIndex];
            Snap();
        }

        private void Bind()
        {
            if (target == null) return;
            respawn = target.GetComponent<VehicleRespawn>();
            damage = target.GetComponent<VehicleDamage>();
            if (respawn != null) respawn.Respawned += Snap;
            if (damage != null) damage.DamageApplied += OnDamage;
        }
        private void Unbind()
        {
            if (respawn != null) respawn.Respawned -= Snap;
            if (damage != null) damage.DamageApplied -= OnDamage;
            respawn = null; damage = null;
        }
        private void OnDamage(in CollisionData impact)
        {
            if (!shakeEnabled || damage.Quality != null && !damage.Quality.cameraShake) return;
            if (impact.Severity < 15) return;
            shakeStrength = Mathf.Clamp(impact.Severity * .0018f, .015f, .12f);
            shakeUntil = Time.unscaledTime + .28f;
        }

        private void OnEnable()
        {
            Bind();
        }
        private void Start() => Snap();
        private void OnDisable()
        {
            Unbind();
        }

        private Vector3 DesiredPosition(out Vector3 focus)
        {
            Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (forward.sqrMagnitude > 0.01f) heading = forward.normalized;
            focus = target.position + Vector3.up * lookHeight;
            Quaternion turn = Quaternion.LookRotation(heading, Vector3.up);
            if (inspecting) turn *= Quaternion.Euler(0, orbitYaw, 0);
            return target.position + turn * offset;
        }

        private Vector3 AvoidObstacles(Vector3 focus, Vector3 desired)
        {
            Vector3 ray = desired - focus;
            float distance = ray.magnitude;
            if (distance < 0.01f) return desired;
            int count = Physics.SphereCastNonAlloc(focus, 0.25f, ray / distance, hits,
                distance, obstacleMask, QueryTriggerInteraction.Ignore);
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform.IsChildOf(target)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0, hits[i].distance - 0.1f));
            }
            return focus + ray.normalized * allowed;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            if (inspecting)
            {
                var touch = Touchscreen.current;
                var mouse = Mouse.current;
                if (touch != null && touch.primaryTouch.press.isPressed)
                    Orbit(touch.primaryTouch.delta.ReadValue().x * .15f);
                else if (mouse != null && mouse.rightButton.isPressed)
                    Orbit(mouse.delta.ReadValue().x * .2f);
            }
            Vector3 desired = DesiredPosition(out Vector3 focus);
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.position = AvoidObstacles(focus, smoothed);
            if (Time.unscaledTime < shakeUntil)
                transform.position += Random.insideUnitSphere * shakeStrength * ((shakeUntil-Time.unscaledTime)/.28f);
            if ((focus - transform.position).sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        public void Snap()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            Vector3 desired = DesiredPosition(out Vector3 focus);
            transform.position = AvoidObstacles(focus, desired);
            if ((focus - transform.position).sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
