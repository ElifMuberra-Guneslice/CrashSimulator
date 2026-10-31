# CrashSimulator Phase 3.5 UI/UX

## Audit

The project uses uGUI (`UnityEngine.UI`) and the Input System UI module. The TestTrack scene already contained the Phase 2 gameplay HUD and crash report; the new HUD is built on the same Canvas/EventSystem approach. No TextMeshPro package was present, so this implementation uses Unity's built-in runtime font and does not add a UI framework or font package. The original vehicle controller, collision/damage scripts, and test-track physics remain in place.

## Design system

`Assets/Settings/UI/PremiumTheme.asset` stores the palette: background `#0B1018`, secondary background `#151D29`, surface `#202B39`, primary text `#F4F7FB`, secondary text `#9CAABD`, blue `#39A8FF`, crash orange `#FF763B`, success `#37C99B`, and danger `#FF5364`. Reusable controls share the `PremiumUiElement` style and short press feedback. The reusable template assets are in `Assets/Prefabs/UI/Premium/`.

Canvas scaling uses a 1600×900 reference with `Scale With Screen Size`; scene canvases include a safe-area component. TestTrack's compact Crash Lab panels collapse for narrower aspect ratios. Settings changes are stored in PlayerPrefs; writes are debounced while a slider is being dragged.

## Screens and prefab structure

- `Assets/Scenes/MainMenu.unity`: cinematic studio stage, animated Prototype vehicle, Home, Garage, Settings, and loading panels. Garage values come from `PrototypeCar.asset`; unavailable specs are explicitly marked.
- `Assets/Scenes/TestTrack.unity` → `CrashSimulator Test Track/Premium UI`: Free Drive HUD, Crash Lab editor, Simulation HUD, pause, damage analysis, and gameplay settings.
- `Assets/Prefabs/UI/Premium/`: PrimaryButton, SecondaryButton, IconButton, GlassPanel, StatCard, VehicleCard, ScenarioCard, ParameterSlider, ToggleControl, Speedometer, DamageIndicator, ConfirmationDialog, ToastNotification, LoadingIndicator, TabNavigation, ModalPanel, SimulationTimeline.
- `Assets/Scripts/Editor/PremiumMenuBuilder.cs` and `PremiumTrackUiBuilder.cs` are deterministic scene authoring tools under **Crash Simulator → Phase 3.5**. The TestTrack builder retains the EventSystem and disables the old visual Canvas.

## Interactions implemented

- Main menu: Free Drive and Crash Lab load TestTrack with the selected mode; Garage, prototype selection, Settings, and back navigation work.
- Free Drive: simultaneous hold steering/pedal/handbrake controls bind to the existing player input, and camera, reset, pause, and damage analysis are connected to existing systems. Steering sensitivity, control opacity/layout, camera shake, speed unit, and audio volume use saved settings.
- Crash Lab: prototype vehicle and barrier/vehicle target selection, scenario presets, speed and angle controls, scenario start/reset, vehicle inspection camera, camera cycling, and pause/settings are wired to the Phase 2 scenario and camera.
- Simulation HUD: speed/time, camera, pause/resume, slow motion, and reset. Replay playback is shown as unavailable because a recorder/player is not implemented.
- Damage analysis: basic and advanced views use the existing `DamageReport`; values that are absent display an unavailable state. Vehicle A/B selection and retry are wired.
- Controls use uGUI Button/Slider/Toggle and have shared visual feedback. The interface currently does not provide live portrait orientation changes; the project currently allows auto-rotation.

## Files created and changed

New runtime files: `Assets/Scripts/Runtime/UI/PremiumUiTheme.cs`, `GameUiSettings.cs`, `GameSession.cs`, `PremiumUiElement.cs`, `PremiumMenuController.cs`, `PremiumSettingsScreen.cs`, `PremiumDrivingHud.cs`, `DamageAnalysisScreen.cs`, `CrashLabConfiguration.cs`, `PremiumTrackController.cs`, `SimulationHudClock.cs`, and `ResponsiveCrashLabPanels.cs`.

New editor files: `Assets/Scripts/Editor/PremiumUiFactory.cs`, `PremiumMenuBuilder.cs`, and `PremiumTrackUiBuilder.cs`.

New authored assets: `Assets/Scenes/MainMenu.unity`, `Assets/Settings/UI/PremiumTheme.asset`, and the 17 prefabs under `Assets/Prefabs/UI/Premium/`.

Modified files: `Assets/Scenes/TestTrack.unity`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/Scripts/Runtime/Damage/SideImpactScenario.cs`, `Assets/Scripts/Runtime/Input/PlayerVehicleInput.cs`, `Assets/Scripts/Runtime/Input/VehicleInputSource.cs`, `Assets/Scripts/Runtime/Vehicle/VehicleController.cs`, `Assets/Scripts/Runtime/Camera/VehicleFollowCamera.cs`, `Assets/Scripts/Runtime/Damage/ImpactAudio.cs`, and `Assets/Scripts/Runtime/UI/HoldButton.cs`.

## Testing and manual verification

Editor authoring completed in Unity batch mode for MainMenu and TestTrack. Final Unity test results: EditMode 19/19 passed; PlayMode 14/14 passed. The PlayMode suite includes the simultaneous gas/steering touch-hold regression and 90 km/h side-impact, reset, report, and impact behavior checks. The initial PlayMode run exposed a missing runtime binding for the new HUD controls; binding on HUD startup fixed it and the focused touch test plus full suite then passed. These are existing physics/interaction tests, not a comprehensive screen-by-screen UI automation suite. No Android/iOS device or touch-device testing was performed in this environment.

In Unity, open `MainMenu` and press Play. Exercise Garage/Settings/back, then Free Drive: hold left/right with throttle, pause/resume, switch camera, inspect damage, and reset. Exit/reopen and choose Crash Lab: vary speed/angle and scenario target, start, pause, reset, then inspect the crash report. Check 16:9, 19.5:9, tablet, and notched safe areas in Game view and on target devices. Confirm input hits both controls simultaneously and verify graphics/audio preferences persist after restart.

## Known limitations

- The prototype uses placeholder geometry and built-in font, not final art direction assets or licensed typefaces.
- Main menu preview is a lit turntable stage, not a fully modeled garage; the camera has subtle vehicle rotation but no parallax environment.
- Only the existing prototype vehicle is selectable. Specifications intentionally identify unavailable values rather than estimating them.
- Replay recording is not implemented; unsupported replay actions are disabled.
- The UI has been authored for landscape-oriented play, but project orientation remains Auto Rotation to preserve current settings. Verify/rescope this before store submission.
- Layout and visual hierarchy need device-level review on narrow phone aspect ratios, tablets, touch targets, and safe-area cutouts.
- Unity batch tests validate code paths, not visual quality, actual mobile frame rate, or physics feel on device.
- Unity batch logs report one or more `The referenced script (Unknown) on this Behaviour is missing!` warnings while loading TestTrack. Tests pass, but the missing references were not traced to a specific scene object in this UI pass and should be inspected in the Unity Inspector before release.

## Next phase

Run the complete UI checklist on representative iPhone and Android aspect ratios, then replace the prototype turntable/vehicle surfaces with original licensed-free art, add an actual scenario library and persistable scenario data, and implement replay controls only alongside a real replay recorder.
