# CrashSimulator asset workspace

This is the new home for city-specific content. Existing `Assets/Scenes`, vehicle prefabs, scripts, and project settings remain in their established locations so serialized references are not broken.

## Layout

```text
_CrashSimulator/
  Environment/City/Source/CityInfrastructureMap3/  original download, untouched
  Environment/{Buildings,Roads,Props,Vegetation,Materials,Textures,Prefabs,Lighting}/
  Scenes/                                         city-only scene documentation/assets
  Scripts/{Environment,Gameplay,Vehicles,UI,Camera,Systems}/
  UI/Fonts/                                       licensed Figma typefaces
```

No city model was present in the existing project's `Assets` tree during the audit. Do not create or ship `City_FreeDrive` until the selected source model is placed, inspected, and validated with road collision and a safe spawn.

See `Environment/City/Source/CityInfrastructureMap3/README.md` for the exact import path and the source attribution record.
