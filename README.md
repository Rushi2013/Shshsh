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

1. Phone/browser me GitHub → is repo → **Settings → Secrets and variables → Actions → New repository secret**.
2. Teen secrets add karein:
   - `UNITY_LICENSE` — Unity Hub se free Personal licence XML (`Unity → Help → Licensing → Re-activate licence → license.ulf` ki content).
   - `UNITY_EMAIL` — aapka Unity account email.
   - `UNITY_PASSWORD` — aapka Unity account password.
3. **Actions** tab → **Build Android APK** → **Run workflow**.
4. Build khatam hone par **Releases** me `CityDrive.apk` milta hai, ya workflow run ke **Artifacts** se download karein.
5. Phone me APK open karein → “Install unknown apps” allow karein → install.

### Option 2 — Unity Cloud Build (sirf browser)

1. [cloud.unity.com](https://cloud.unity.com) par login karein, ye GitHub repo connect karein.
2. New Target → Platform **Android**, project ye repo, build scene `Assets/Scenes/CityDrive.unity`.
3. Build start karein; downloaded APK phone me install karein.

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
  Resources/Vehicles/BMW/                       # BMW M3 GTR + original .mat/.fbx/.meta references
  Scripts/GameBootstrap.cs                      # entry point
  Scripts/CityBuilder.cs                        # GLB -> city + colliders + spawn point
  Scripts/ArcadeCarController.cs                # car physics
  Scripts/VehicleVisuals.cs                     # wheel spin/steer
  Scripts/MobileControls.cs                     # touch UI
  Scripts/ChaseCamera.cs                        # camera modes
  Editor/ProjectSetup.cs                        # auto scene + APK build method
.github/workflows/build-apk.yml                 # GitHub Actions -> APK + Release
CREDITS.md                                      # model/source attribution
```

## Note

- City model ~13 MB, car ~8 MB; APK expected size roughly 40–90 MB (textures + Unity runtime).
- Build ke liye Unity **Personal** licence required hai (free). CI me licence secret lagana zaroori hai.
- Unity Cloud Build / GitHub Actions dono cloud me Unity chalate hain; isliye aapko PC ki zarurat nahi.
