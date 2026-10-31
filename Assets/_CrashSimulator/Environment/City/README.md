# City environment

`Source/CityInfrastructureMap3/` is reserved for the selected source model. Keep the original import immutable. Place optimized materials, generated prefabs, collision proxies, and scene-specific instances in sibling folders only after inspecting its hierarchy and license.

Initial inspection checklist after import:

1. Select the model root and run **Crash Simulator → City → Inspect Selected City Model**.
2. Review its renderer bounds, scale, orientation, mesh/triangle counts, material shaders, and existing colliders.
3. Identify road meshes by their actual hierarchy and validate each road surface in Scene view before adding static collision.
4. Add low-complexity building/guardrail colliders only where a player can reach them; omit colliders for distant decoration.
5. Place a verified road-aligned spawn before creating `City_FreeDrive`.

The source listing's triangle count is a warning to profile on representative devices; it is not a measured mobile frame-rate result.
