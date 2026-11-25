# City Drive — Unity 3D Open-World Sedan Driving

Ek **real Unity 3D** project hai (HTML/JS nahi). Ismein aapki supplied city GLB aur BMW 330i-inspired sedan GLB, physics-driven car controller, circular touch steering, burnout/drift effects, layered car audio aur mobile HUD hai.

## Latest APK — city-drive-26

**Fast raw download (repo-hosted):** https://raw.githubusercontent.com/Rushi2013/Shshsh/arena/411eaf4f-shshsh/apk/CityDrive.apk

**Release download:** https://github.com/Rushi2013/Shshsh/releases/download/city-drive-26/CityDrive.apk

**Verified CI build:** https://github.com/Rushi2013/Shshsh/actions/runs/45563875786 ✅

SHA-256: `4d93511172e875606ac9c7d4e954725da7700217b1aac11d931cb34796a2e2b9`

Phone par APK download karke install karein (unknown apps allow karna pad sakta hai).

## Kya kya hai

- **Physics:** Rigidbody + 4 raycast wheels, spring/damper suspension, per-wheel angular velocity, slip-based tyre forces, friction circle, anti-roll bars, weight transfer, aero drag/downforce, automatic 5-speed gearbox with torque curve, configurable FWD/RWD/AWD.
- **Steering:** true angular circular wheel (clockwise = right, counter-clockwise = left), 200° configurable range, single-touch rim drag, smooth self-centring, Ackermann front steering; rear wheels steer nahi karte. Original interior wheel mile to usse sync kiya jata hai (is GLB me alag steering mesh nahi hai, isliye duplicate primitive nahi banaya gaya).
- **Pedals:** alag accelerator aur brake, hold-to-drive, smooth press/release animation. Dono dabane par brake physics priority leti hai aur driven wheels ko burnout ke liye spin hone deti hai.
- **Drift/burnout:** real lateral/longitudinal slip se; pooled skid marks aur contact-point smoke sirf tab jab tyres actually slide karein. Handbrake rear grip kam karta hai.
- **Audio:** procedurally generated RPM-driven engine/load/turbo layers, wheelspin, squeal, wind, gear shift, horn, start/stop, impact aur loading ambience. Koi copyrighted BMW recording nahi.
- **Speedometer:** purana PNG hata diya gaya; naya runtime circular gauge + needle + digital km/h sirf actual player Rigidbody velocity se. Input se fake nahi.
- **Cameras:** 3 modes — close chase, far chase, driver-seat interior. Smooth blend, configurable offsets/FOV.
- **Loading:** real `ResourceRequest`/setup-stage progress, smooth bar, tips aur loading audio. Timer se fake percentage nahi.
- **Showroom:** fixed-centre, frame-rate-independent 360° car rotation aur studio lighting.
- **Graphics:** realistic layered 3D clouds + procedural sky, single warm sun (shadow strength 0.88, 2 cascades, 85 m distance), skybox ambient/reflections, subtle fog, real car/building shadows + subtle contact patch, PBR/glass/emissive tuning, headlights aur brake-responsive taillights.
- **City:** roads, pavements, ramps aur terrain par proper colliders; decorative meshes ko colliders nahi milte.

## Controls

| Touch | Keyboard | Kaam |
| --- | --- | --- |
| Circular wheel (angle drag) | A / D ya arrow keys | Steering |
| Gas (right) | W / up arrow | Accelerator |
| Brake (right) | S / down arrow | Brake / reverse |
| Gas + Brake together | W + S | Burnout (drivetrain ke hisaab se) |
| Drift | Space | Rear handbrake |
| CAM | C | Camera cycle |
| RESET | R | Road spawn par wapas |
| — | H | Horn |

## Build

Unity **2022.3.62f3**, Built-in Render Pipeline, Android ARM64 + IL2CPP. Scene aur player settings `Assets/Editor/ProjectSetup.cs` se auto banate hain. CI command:

```
gh workflow run build-apk.yml --ref arena/411eaf4f-shshsh
```

Fast raw link ke liye `host_only=true` ke saath dobara run karein. Build secrets `UNITY_EMAIL`/`UNITY_PASSWORD` already GitHub Actions me set hain. Details: [UNITY-LICENSE-GUIDE.md](UNITY-LICENSE-GUIDE.md).

## Project map

```
Assets/Scripts/ArcadeCarController.cs   physics, gearbox, tyres, burnout
Assets/Scripts/WheelTelemetry.cs        per-wheel contact/slip data
Assets/Scripts/VehicleVisuals.cs        wheel + original interior wheel sync
Assets/Scripts/VehicleEffects.cs        pooled skid marks + smoke
Assets/Scripts/VehicleAudio.cs          procedural layered audio
Assets/Scripts/VehicleLights.cs         headlights + brake lights
Assets/Scripts/MobileControls.cs        circular wheel, pedals, speedometer
Assets/Scripts/LoadingScreen.cs         real progress + loading audio
Assets/Scripts/HomeScreen.cs            showroom + PLAY flow
Assets/Scripts/ChaseCamera.cs           3 camera modes
Assets/Scripts/CityBuilder.cs           city orientation + colliders
Assets/Scripts/GameBootstrap.cs         async loading + lighting + material setup
Assets/Editor/ProjectSetup.cs           scene/Android setup + APK build
AUDIT.md                                audit and asset-license research
CREDITS.md                              model/tool attributions
```

## Verification note

Sandbox me Unity Editor/Android device nahi hai. Yahan se Unity Android build CI par compile aur package hota hai; actual driving feel/FPS device par confirm karna padega. Pichhla safe state Git tag `pre-full-overhaul-backup` par available hai.
