# City Infrastructure Map 3 — imported source

The original Sketchfab glTF source, binary buffer, textures and `license.txt` are now stored in this folder. Preserve the filenames and relative paths: `scene.gltf` references `scene.bin` and the `textures/` directory.

Source: [City Infrastructure Map 3 by PropShop / syedabbas0815](https://sketchfab.com/3d-models/city-infrastructure-map-3-1c6262eb26b94ec0b7565211268fb577)

The included license file identifies CC Attribution 4.0 International (CC BY 4.0). Credit the author and retain the license with redistributed source. The listing gives 244.6k triangles and 172.6k vertices. The source glTF has 20 nodes, 15 meshes, 13 materials, 12 textures, and a ~13 MB geometry buffer. The author describes a road network with multi-lane streets, intersections, an elevated highway/overpass, support pillars, and a flat terrain base ready for additional buildings. It does not include city buildings or props.

The Unity glTFast package is configured in the project manifest. Unity must resolve the package and import this source before it becomes a native Unity model asset. After import, select the imported model and use **Crash Simulator → City → Inspect Selected City Model**. No city scene, road colliders, or spawn point have been created yet.
