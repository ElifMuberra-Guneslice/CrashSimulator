# City asset and Figma UI integration audit

Audit date: 2026-10-26

## Existing project

- Unity editor/project version: 6000.3.26f1; URP 17.3.0.
- Existing playable scene: `Assets/Scenes/TestTrack.unity`; menu: `Assets/Scenes/MainMenu.unity`; sample scene retained. Build settings currently register MainMenu, TestTrack, and SampleScene.
- Existing driving: `VehicleController`, `PlayerVehicleInput`, `VehicleRespawn`, `VehicleFollowCamera`, collision/damage reporting, scenario setup, and the Phase 3.5 uGUI screens are already integrated in TestTrack. Reuse these systems; do not introduce a second vehicle controller.
- UI framework: uGUI (`UnityEngine.UI`) and the Input System UI module. TextMeshPro is not in the current package manifest, so keep the established uGUI stack. The Figma typefaces are imported as Unity `Font` assets.
- Imported assets contain a geometric prototype car, concrete barrier, and test-track materials. No FBX, OBJ, glTF, GLB, Blender city source, or City Infrastructure Map 3 file was present in the audited `Assets` tree.
- URP Mobile and PC pipeline assets already exist. Keep those settings until city import and device profiling justify specific changes.

## Imported source models

The city model source is now in `Assets/_CrashSimulator/Environment/City/Source/CityInfrastructureMap3/`. Its `scene.gltf`, `scene.bin`, `textures/`, and original `license.txt` were copied intact. The [Sketchfab listing](https://sketchfab.com/3d-models/city-infrastructure-map-3-1c6262eb26b94ec0b7565211268fb577) identifies CC BY 4.0 and credits PropShop™ / syedabbas0815; retain attribution. The source describes multi-lane streets, intersections, elevated highway, support pillars, and a flat base, but no buildings/props. It is about 35 MB on disk; the listing reports 244.6k triangles / 172.6k vertices, which still needs Unity/mobile profiling.

The three vehicle sources are in `Assets/Models/Vehicles/`: Lucid Air Sapphire (~46 MB), Low-Poly Sedan (~456 KB), plus their original textures, buffers and license files. The Lucid glTF is particularly large (1,033 nodes, 474 meshes, 97 materials, 175 textures); it requires LOD/mesh and texture optimization before mobile use. Its included `license.txt` says Sketchfab Standard, whose general terms disallow redistribution of the asset as a standalone file and require the applicable license rights to be held. The sedan is CC BY 4.0 by scailman and requires attribution. The source metadata is retained in each folder.

The user asked not to use the Ford F-150 model. Its project copy was removed; the downloaded source remains untouched in Downloads. It is not included in the game. The supplied Ford license is CC BY-NC-SA 4.0, which prohibits commercial use.

Unity glTFast `6.20.0` is declared in `Packages/manifest.json` to import glTF into native Unity model assets. Registry metadata lists it for Unity 6000.0 and newer. The Editor still needs to resolve and import this package; the model sources are not yet verified usable prefabs. `City_FreeDrive` has not been created: road orientation, scale, colliders, and a valid spawn still need verification. Free Drive remains on TestTrack.

## Figma Make audit

The provided Figma Make project was accessible through the Figma connector and the already-open browser tab. The workspace instructions/preview specified 1920×1080 landscape, 64 px horizontal and 48 px vertical safe-area guides; dark flat surfaces without glass/blur; a cinematic garage/industrial facility; and editable controls with 48 px minimum touch targets (larger driving controls). Palette: base `#0A0A0C`, panel `#121A2E`, border `#2A3550`, text `#F2F5FA` / `#8E9AB5`, blue `#2E8BFF`, orange `#FF7A1A`, success `#2ECC71`, caution `#FFB020`, critical `#FF4D4D`. Display/UI font roles are Rajdhani and Inter.

The Make workspace described these screens:

1. `01_MainMenu`: dark cinematic garage/car image, wordmark and navigation.
2. `02_Garage`: vehicle selection, preview and game-rating bars.
3. `05_CrashLab_Setup`: vehicle/obstacle selection, placement view, speed, angle, presets and simulation start.
4. `06_CrashLab_SimHUD`: minimal simulation controls/timeline.
5. `07_DamageReport`: impact summary, localized damage map, parts/mechanical damage, retry/edit/compare/menu actions.
6. `08_RunComparison`: two or three actual runs side by side.
7. `09_Settings`: categories and graphics/audio/controls/units.

The Make brief does not define a separate Free Drive HUD frame, so the existing functional TestTrack HUD is retained as the base. Its visual system now uses the flat Figma palette and licensed Rajdhani/Inter font roles without copying the Vite/React app or turning a whole screen into a bitmap. MainMenu uses the real prototype car on a 3D turntable; no licensed photo asset from the Make project was added. Garage exposes only the real prototype data and does not invent stats for unimplemented vehicle variants. The UI Canvas reference size is 1920×1080, landscape orientation is configured, and the menu exposes working sound and settings actions.

The current runtime has no real run-comparison history storage, free-drive city scene, or replay recorder. Those actions remain unsupported until connected to actual gameplay data; the design's comparison layout must not display fabricated measurements.

## Current integration steps

- `Assets/_CrashSimulator/` provides the environment/UI staging structure without moving serialized legacy assets.
- `Assets/Scripts/Editor/FigmaUiDesignSystemImporter.cs` applies exact palette, Rajdhani/Inter roles, opaque panel fills, and thin outlines to existing UI prefabs/scenes.
- `Assets/Scripts/Editor/CityModelAudit.cs` provides a non-destructive imported-model inspection menu.
- `Assets/Tests/PlayMode/PremiumUiPlayModeTests.cs` covers menu navigation/sound and Crash Lab speed, angle, start, and reset interactions; these new tests still need to run in the open Unity Editor.
- `Packages/manifest.json` now includes registry version Unity glTFast 6.20.0. `.gitattributes` sends the three included glTF `.bin` buffers and the existing PNG textures to Git LFS. Package resolution and source-model import remain to be confirmed in the Editor.
- City scene creation and collider assignment remain gated on imported-model hierarchy inspection.

## Required follow-up

1. Let Unity resolve `com.unity.cloud.gltfast` and import the city and vehicle glTF files. Verify no importer errors.
2. Run the City Model Audit for the city and inspect vehicle bounds/axes/mesh hierarchy. Build simple physics colliders and vehicle prefabs; never put detailed render meshes on WheelColliders or chassis physics.
3. Optimize models/textures for mobile (LOD, mesh/texture memory, material count) before adding them to scenes. Profile on representative devices.
4. Create `City_FreeDrive` only after city-road collider coverage and a safe spawn are validated; then route Free Drive into that scene.
5. Retain each asset's attribution/license. Keep the Lucid asset in accordance with its Sketchfab Standard license; the F-150 is excluded at the user's request. The sedan requires CC BY attribution.
6. Validate Figma layouts at 2400×1080, 2532×1170, 1280×720 and 1024×768. Visual/touch-device checks have not been performed. PlayMode validation was attempted in batch mode but Unity rejected the run because it reported another project instance.
7. Add comparison UI only when run snapshots/history are available, and bind Compare/Replay controls to implemented features rather than placeholders.

## Font source/license

Rajdhani and Inter files were retrieved from the official Google Fonts GitHub repository, with the corresponding SIL Open Font License 1.1 files stored beside them in `Assets/_CrashSimulator/UI/Fonts/`. No runtime UI package was added.
