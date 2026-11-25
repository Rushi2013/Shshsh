# Asset credits

## City model — `Assets/Resources/City/city.glb`

- Title: **extreme car driving simulator - city (pre-2021)**
- Author: **amogusstrikesback2** (https://sketchfab.com/amogusstrikesback2)
- Source: https://sketchfab.com/3d-models/extreme-car-driving-simulator-city-pre-2021-9ba763ff456a42d0b4da4d58334e8983
- Licence: **CC BY 4.0** (http://creativecommons.org/licenses/by/4.0/)
- File supplied by the user with a `.txt` extension; renamed to `.glb` for Unity/glTFast.

## Primary car model — `Assets/Resources/Vehicles/BMW330i/bmw330i.glb`

- BMW 330i-inspired sedan GLB supplied by the user for this project.
- It is an **inspired-by design**, not an official BMW asset or official BMW recording.
- Licence documentation was not supplied with the file; please confirm redistribution rights before commercial release.

## Additional unused/reference car asset — `Assets/Resources/Vehicles/BMW/`

- **2005 BMW M3 GTR E46** model and materials by **BxB Studio** ("MVC Getting Started" package),
  copied from https://github.com/realTimexo/Crimson-Streets (path:
  `Assets/BxB Studio/MVC Getting Started/Models/Vehicles/Cars/BMW M3 GTR E46`).
- Only the `... - Empty.prefab` (visual-only hierarchy), FBX sub-meshes, materials and textures are used.
- The upstream repository does not include an explicit licence file for this asset. Please verify the
  licence with the original author before commercial distribution and replace the model if needed.

## Tooling / runtime

- glTFast (`com.unity.cloud.gltfast`) — Unity Technologies and glTFast authors, Apache-2.0.
- Game is compiled by Unity; APK automation uses `game-ci/unity-builder`.

## Audio and sky

- All engine, turbo, tyre, horn, impact and loading audio is generated procedurally at runtime by `VehicleAudio.cs`. No third-party recordings or BMW audio were imported.
- The skybox uses Unity's built-in `Skybox/Procedural` shader. No external HDRI/skybox pack was downloaded.

The game code in `Assets/Scripts` and `Assets/Editor` was written specifically for this repository.
