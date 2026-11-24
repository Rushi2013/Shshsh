# City Drive — Unity 3D Open-World Car Game

Ek **real Unity project** (HTML/JS nahi). Ismein aapki upload ki hui city GLB, BMW M3 GTR car model,
raycast suspension physics, drifting, on-screen mobile controller aur poori city me collisions hai.

> **Important (sach):** Is sandbox/container me Unity Editor, Android SDK aur Java nahi hai. Isliye yahan
> se directly compile karke APK banana technically impossible hai — Unity ka apna engine/licence chahiye.
> Maine poora game + **automatic APK build pipeline** bana diya hai. APK GitHub Actions (cloud me Unity)
> ya Unity Cloud Build se banta hai; dono PC ke bina mobile browser se chalaye ja sakte hain. Neeche steps diye hain.

## Game kya karta hai

- **City:** aapki `city.glb` (Sketchfab CC-BY city, 713 meshes) glTFast se load hoti hai, centre me place hoti hai,
  har road/building par runtime MeshCollider/BoxCollider lagta hai — car kahin bhi ja sakti hai aur crash kar sakti hai.
- **Car:** BMW M3 GTR E46 (29 FBX sub-meshes + asli materials/textures, original wheel nodes ke saath).
- **Physics:** 4-wheel raycast suspension, spring/damper, rear-wheel drive, tyre slip + friction circle,
  speed-sensitive steering, downforce, braking/reverse, handbrake drift, rollover auto-reset.
- **Mobile controls:** fully touch-based steering pad, GAS, BRAKE, DRIFT, CAM, RESET. Keyboard fallback bhi hai
  (WASD / arrows, Space drift, C camera, R reset).
- **Camera:** 3 modes — chase, bonnet, top — speed ke saath FOV badalta hai, walls se clip nahi karta.
- **HUD:** live speed in km/h.

## APK kaise banayein (PC ke bina, mobile browser se)

### Option 1 — GitHub Actions (recommended)

**Poori step-by-step guide:** [UNITY-LICENSE-GUIDE.md](UNITY-LICENSE-GUIDE.md) — secrets kahan se laayein, kahan paste karein, build kaise chalayein, sab wahan hai.

**PC ke bina (recommended):**
1. GitHub → repo → **Settings → Secrets and variables → Actions** → 2 secrets: `UNITY_EMAIL`, `UNITY_PASSWORD` (no `.ulf` needed with GameCI v6).
2. **Actions → Build Android APK → Run workflow**.
3. Build ke baad **Releases** ya **Artifacts** se `CityDrive.apk` download karein.

Details: [UNITY-LICENSE-GUIDE.md](UNITY-LICENSE-GUIDE.md).

### Option 2 — Unity Cloud Build

Dashboard se repo connect karke Android target bana sakte hain, par cloud build automation Unity ke paid plans (Pro/Enterprise) ka feature hai; free Personal ke liye upar wala GitHub Actions tarika best hai.

### Option 3 — PC par Unity (agar aapke paas Unity ho)

Unity **2022.3.62f3** se project open karein. Project auto-setup ho jayega (`ProjectSetup.cs` scene aur settings banata hai).
Play dabayein ya menu **CityDrive → Build Android APK**.

## Controls

| Touch | Keyboard | Kaam |
| --- | --- | --- |
| Left pad (left/right slide) | A / D | Steering |
| GAS | W | Accelerator |
| BRAKE | S | Brake / reverse |
| DRIFT | Space | Handbrake drift |
| CAM | C | Camera change |
| RESET | R | Car ko road par wapas |

## Project structure

```
Assets/
  Resources/City/city.glb                       # aapki uploaded file (extension .txt hata diya gaya)
  Resources/Vehicles/BMW330i/bmw330i.glb        # naya BMW 330i xDrive model (aapki file)
  Resources/UI/                                  # AI-generated loading/showroom background + icons
  Scripts/GameBootstrap.cs                      # entry point
  Scripts/CityBuilder.cs                        # GLB -> city + colliders + spawn point
  Scripts/ArcadeCarController.cs                # car physics
  Scripts/VehicleVisuals.cs                     # wheel spin/steer
  Scripts/MobileControls.cs                     # touch UI with AI icon sprites + speedometer
  Scripts/LoadingScreen.cs                      # real-time yellow loading bar + percentage
  Scripts/HomeScreen.cs                         # 3D rotating-car home screen + PLAY button
  Scripts/ChaseCamera.cs                        # camera modes
  Editor/ProjectSetup.cs                        # auto scene + APK build method
.github/workflows/build-apk.yml                 # GitHub Actions -> APK + Release
CREDITS.md                                      # model/source attribution
```

## Latest APK

Direct download: https://github.com/Rushi2013/Shshsh/releases/download/city-drive-13/CityDrive.apk

Loading screen ke baad 3D home screen aata hai — BMW circular stand par ghoomta hai, PLAY dabate hi car city road par spawn hoti hai. City GLB ko +90° X se upright kiya gaya hai aur spawn nearest road mesh par hota hai.

## Note

- City model ~13 MB, naya car GLB ~10 MB; APK expected size roughly 40–90 MB (textures + Unity runtime).
- Build ke liye Unity **Personal** licence required hai (free). CI me licence secret lagana zaroori hai.
- Unity Cloud Build / GitHub Actions dono cloud me Unity chalate hain; isliye aapko PC ki zarurat nahi.
