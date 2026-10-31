using CrashSimulator.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrashSimulator.Input
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerVehicleInput : VehicleInputSource
    {
        [SerializeField] private HoldButton accelerator;
        [SerializeField] private HoldButton reverse;
        [SerializeField] private HoldButton brake;
        [SerializeField] private HoldButton left;
        [SerializeField] private HoldButton right;
        [SerializeField] private HoldButton handbrake;

        private VehicleCommand command;
        private bool resetRequested;
        private bool suspended;
        public override VehicleCommand Command => command;

        public void BindControls(HoldButton gas, HoldButton back, HoldButton stop,
            HoldButton turnLeft, HoldButton turnRight)
        {
            accelerator = gas;
            reverse = back;
            brake = stop;
            left = turnLeft;
            right = turnRight;
        }
        public void BindHandbrake(HoldButton hold) => handbrake = hold;
        public void SetInputSuspended(bool value)
        { suspended=value;if(value)ClearInput(); }

        private void Update()
        {
            if (suspended) return;
            float throttle = Held(accelerator) - Held(reverse);
            float steering = (Held(right) - Held(left)) * GameUiSettings.SteeringSensitivity;
            float braking = Held(brake);
            float parking = Held(handbrake);

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                throttle += (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
                steering += ((keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0)) * GameUiSettings.SteeringSensitivity;
                braking = Mathf.Max(braking, keyboard.spaceKey.isPressed ? 1 : 0);
                parking = Mathf.Max(parking, keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? 1 : 0);
                if (keyboard.rKey.wasPressedThisFrame) RequestReset();
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                throttle += gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();
                steering += gamepad.leftStick.x.ReadValue() * GameUiSettings.SteeringSensitivity;
                braking = Mathf.Max(braking, gamepad.buttonSouth.isPressed ? 1 : 0);
                parking = Mathf.Max(parking, gamepad.buttonEast.isPressed ? 1 : 0);
                if (gamepad.selectButton.wasPressedThisFrame) RequestReset();
            }

            command = new VehicleCommand
            {
                Throttle = Mathf.Clamp(throttle, -1, 1),
                Steering = Mathf.Clamp(steering, -1, 1),
                Brake = Mathf.Clamp01(braking),
                Handbrake = Mathf.Clamp01(parking)
            };
        }

        public void RequestReset() => resetRequested = true;

        public bool ConsumeResetRequest()
        {
            bool requested = resetRequested;
            resetRequested = false;
            return requested;
        }

        public override void ClearInput()
        {
            command = default;
            resetRequested = false;
            if (accelerator != null) accelerator.Release();
            if (reverse != null) reverse.Release();
            if (brake != null) brake.Release();
            if (left != null) left.Release();
            if (right != null) right.Release();
            if (handbrake != null) handbrake.Release();
        }

        private static float Held(HoldButton button) => button != null && button.IsHeld ? 1 : 0;
        private void OnDisable() => ClearInput();
        private void OnApplicationFocus(bool focused)
        {
            suspended = !focused;
            if (!focused) ClearInput();
        }
        private void OnApplicationPause(bool paused)
        {
            suspended = paused;
            if (paused) ClearInput();
        }
    }
}
