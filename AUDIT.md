# City Drive — overhaul audit (2026-11-24)

## Phase 1 — project audit

- **Unity:** 2022.3.62f3, Built-in Render Pipeline (no URP/HDRP package).
- **Packages:** `com.unity.cloud.gltfast` 6.9.0, `com.unity.ugui`, core audio/physics/particles/modules. No Input System package; legacy `Input` is used, enabled by `ProjectSetup`.
- **Android:** ARM64 + IL2CPP, min SDK 24, landscape; APK built by `ProjectSetup.BuildAndroid` in GameCI.
- **Scene:** `Assets/Scenes/CityDrive.unity` is generated and saved by `ProjectSetup.EnsureScene()`; runtime systems are added by `GameBootstrap`.
- **Assets:** user-supplied `city.glb` (roads, pavements, terrain, ramps) and `bmw330i.glb`. An older M3 GTR asset exists but is unused by gameplay.
- **Before this overhaul:** one controller existed, but it had no per-wheel angular velocity (fake wheel spin integration), no burnout, no tyre smoke/skid marks, no audio system, no real asynchronous loading, fake loading timers, a manually created duplicate interior steering wheel, 4 camera modes (including bonnet/top) instead of the requested 3, and a speedometer that was only text.
- **GLB inspection:** the 330i GLB has 81 nodes / 41 meshes / 16 materials, wheel nodes `polySurface1-4`, generic interior meshes, and **no steering-wheel or dashboard node**. The manual wheel added in the previous build was therefore removed; no primitive replacement is created.
- **Backups:** previous branch tip saved as Git tag `pre-full-overhaul-backup`.

## Phase 2 — asset research

| Source | Check | Decision |
| --- | --- | --- |
| Vehicle Essentials (Unity Asset Store, ID 194951) | Free, Standard Unity EULA, v1.0, original Unity 2019.4.20, 161 WAV sounds | Suitable license, but requires a signed-in Unity Account download; not imported. |
| i6 German Free Engine Sound Pack (ID 106037) | Free, Standard Unity EULA, v2.0.0, compatible with Built-in/URP/HDRP, Unity 2020.3.12, 18 WAV loops | Good i6 reference; not downloaded (account required). |
| Skybox Series Free (ID 103633) | Free, Standard EULA, v4.3, 16 HDRIs, 274.9 MB | Realistic but heavy for mobile and account-gated; not imported. |
| SuperRev Starter / Cars Audio Pack Vol. 1 | Paid / requires approval | Not purchased, not imported. |
| Vehicle SFX Free (itch.io) | Page could not be fetched from this sandbox | Not imported; license unverified. |
| TheDeveloper10/Unity-Speedometer, DreddDev/UnityUISpeedometer | GitHub API reports no license for either repository | No code copied; only the general digital+needle concept is used in our own implementation. |

Because external packs could not be downloaded or license-verified end-to-end here, engine/turbo/tyre/loading audio is generated procedurally at runtime and the sky uses Unity's built-in procedural skybox shader. No copyrighted BMW recordings were used.

## Implemented systems and files

- `ArcadeCarController.cs` — rigidbody physics, 4 raycast wheels, spring/damper, per-wheel angular velocity, slip-based tyre forces in a friction circle, Ackermann steering, gearbox, torque curve, FWD/RWD/AWD, load transfer, anti-roll, drag/downforce, real wheelspin and burnout.
- `WheelTelemetry.cs` — read-only per-wheel contact/slip data shared with effects and audio.
- `VehicleVisuals.cs` — wheel mesh sync; syncs only an original interior wheel if the model has one.
- `VehicleEffects.cs` — pooled skid marks and slip-driven smoke.
- `VehicleAudio.cs` — procedural engine/load/turbo/tyre/squeal/wind layers, one-shots, interior muffling.
- `VehicleLights.cs` — headlights and brake-responsive taillights.
- `ChaseCamera.cs` — close chase, far chase, interior; one camera, smooth mode blend.
- `MobileControls.cs` — angular wheel HUD, separate pedals, drift, live Rigidbody speedometer + needle, safe area.
- `LoadingScreen.cs` — real progress input, animated bar, tips, procedural loading audio, no timer-based progress.
- `HomeScreen.cs` — fixed-centre frame-independent showroom rotation, real staged loading, studio lighting.
- `CityBuilder.cs` — drivable surface colliders, building boxes, decoration skip list.
- `GameBootstrap.cs` — async asset loading with real progress, PBR/glass/emissive material upgrades, sky/sun/fog/reflection probe, system wiring.
- `ProjectSetup.cs` — Android/quality settings and procedural skybox asset for CI builds.

## Cloud-sky PDF pass (no APK built)

The supplied `Realistic_Clouds_Unity_A_to_Z_Guide(1).pdf` was read in full. Its key rules were applied:

- Confirmed Built-in Render Pipeline; no URP/HDRP cloud shader was imported.
- Kept the imported AllSky/daytime skybox if present; added an editor-side material scanner instead of guessing a material name. `CityDrive → Reapply Sky and Environment` re-runs detection after importing the asset pack.
- Built-in fallback: `Assets/Shaders/RealisticSky.shader` (atmospheric gradient, soft sun, restrained high cloud veil).
- Separate true 3D cloud layer: `Assets/Scripts/CloudSky.cs` + `Assets/Shaders/SoftCloud.shader`, with high/mid/near groups, slow independent drift, soft noise edges, no shadows/tessellation and combined meshes for low draw calls.
- Environment lighting/reflections from skybox, neutral exposure/rotation, subtle distant fog, warm directional sun, soft shadows at mobile-friendly resolution/distance.
- No vehicle physics, controls, audio or gameplay UI changed in this pass.
- Backup tag before this pass: `pre-cloud-overhaul-backup`.

The AllSky asset pack itself is not present in the sandbox workspace. The code auto-detects it once imported into the Unity project; no password was requested or used.

## Realistic tyre smoke pass (no APK built)

The supplied `Realistic_Tire_Smoke_Drift_Burnout_Unity_Guide(1).pdf` was read in full. Applied:

- Removed the old simple radial-texture smoke emitter entirely.
- New `Assets/Shaders/TireSmoke.shader`: soft alpha, per-particle colour, built-in-pipeline compatible and soft-particle depth fade against road/buildings. Camera depth texture is enabled in `GameBootstrap`.
- `VehicleEffects.cs`: one pooled emitter per wheel at the real contact point, soft noise texture, low-strength noise module, slow upward drift, trailing velocity, expanding size, alpha fade, 120 particles/wheel cap, no shadows.
- Emission only from measured wheel contact/slip; normal grip, traction-limited acceleration and normal braking produce no smoke. Burnout smoke comes from driven spinning wheels; drift smoke from lateral slip; intensity is smoothed and fades when grip returns.
- Skid marks remain pooled per-wheel strips spawned from real contact paths, darker for burnout.
- `VehicleAudio.cs`: separate physics-driven wheelspin and brake-squeal layers added beside drift squeal and general tyre scrub; no button-only triggering.
- No external paid smoke VFX was imported: the pack was not supplied, and the project is Built-in while several recommended packs are URP-only.

Backup tag before this pass: `pre-smoke-overhaul-backup`.

## Verification limits

No Unity Editor, Android SDK or device is available in this sandbox. Verification consists of static checks plus Unity Android builds in GitHub Actions. Runtime feel and frame rate must be confirmed on the user's device.
