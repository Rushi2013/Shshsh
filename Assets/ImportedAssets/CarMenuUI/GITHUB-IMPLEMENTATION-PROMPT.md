# Prompt: GitHub ke Car Menu UI v2 se game ka main menu banao

Neeche poora prompt apne Unity coding agent ko do. UI assets branch mein available hain; code implementation is prompt ko follow karne wale agent ka task hai.

---

## PROMPT START

Tum Unity mobile UI developer ho. Mere **open-world car game** ka home screen aur garage/settings submenus update karo. GitHub ke Car Menu UI v2 assets ka use karo. **Guns, shooting, military ya weapon-related koi content nahi chahiye.**

### 1. Project aur exact asset paths

- Repository: `https://github.com/Rushi2013/Shshsh`, branch `arena/411eaf4f-shshsh`. Latest branch checkout karke code, git status, Unity version, pipeline, actual runtime flow aur current user changes verify karo.
- UI root: `Assets/ImportedAssets/CarMenuUI/`.
- `Assets/ImportedAssets/CarMenuUI/Car_Menu_UI_Kit_v2.zip` — original full ZIP; extracted assets same folder mein already available hain. Dobara unzip karke duplicate assets mat banao.
- Background: `Assets/ImportedAssets/CarMenuUI/Background/MainMenu_Background.png`, actual size **1672 × 941**.
- Main menu PNGs: `Assets/ImportedAssets/CarMenuUI/Main_Buttons/`.
- Submenu PNGs: `Assets/ImportedAssets/CarMenuUI/Sub_Buttons/`.
- Coin icon: `Assets/ImportedAssets/CarMenuUI/Icons/Icon_Coins.png`.
- `Assets/ImportedAssets/CarMenuUI/README.md` — complete kit notes; `Button_Preview.jpg` sirf reference image hai, runtime sprite nahi.
- Buttons labelled transparent PNGs hain, each **3840 × 960**, 4:1 aspect; labels image mein baked hain. Coin icon 3840 × 3840 hai. Original background 4K nahi hai. Ye visual assets hain, ready-to-run UI logic nahi.

### 2. Existing project ke relevant files pehle inspect karo

Latest branch par verify karo; previous snapshot mein ye files dikhi thi:
- `Assets/Scripts/HomeScreen.cs` — runtime home/showroom UI and Play transition.
- `Assets/Scripts/GameBootstrap.cs` — city/car load, home and gameplay flow.
- `Assets/Scripts/GarageController.cs` — garage screen aur car selection flow.
- `Assets/Scripts/LoadingScreen.cs`, `Assets/Scripts/UiTools.cs`, `Assets/Scripts/MobileControls.cs`, `Assets/Scripts/ChaseCamera.cs`, `Assets/Editor/ProjectSetup.cs`.

`HomeScreen.cs` ke comments mein Play, Garage, Customize, Settings, Credits, Quit aur local coins wallet ka zikr hai; actual implementation read and test karke current behavior confirm karo. Working callbacks, showroom and selected car data ko preserve karo. Assumption mat karo ki har feature ya button already fully functional hai.

### 3. Home screen design

- Full-screen background image use karo; aspect-ratio-aware crop/fit aur safe-area support do. Buttons left side, car image right side visible ho. Different aspect ratios aur landscape mobile layout test karo.
- Existing 3D showroom/car preview aur Play flow ko bina todhe image ke saath integrate karo. Agar 3D car overlapping visual mein galat lage to menu preview rendering/visibility ko menu-only state se adjust karo; gameplay car, physics aur camera ko nuksan na ho.
- Unity CanvasScaler, EventSystem aur touch-friendly spacing. Duplicate Canvas/EventSystem/AudioListener nahi. Responsive UI, smooth but lightweight transitions. Names aur data ko project se lo; third-party branding blindly copy mat karo.
- File mapping:
  - Play: `Main_Buttons/Button_PLAY.png`; alternate start `Button_START.png`, continue only if valid save `Button_CONTINUE.png`.
  - Garage: `Main_Buttons/Button_GARAGE.png`; cars `Button_CARS.png`; customize `Button_CUSTOMIZE.png`.
  - Settings: `Main_Buttons/Button_SETTINGS.png`; credits `Button_CREDITS.png`; exit `Button_EXIT.png`.
  - Store and wallet: `Main_Buttons/Button_STORE.png`, `Button_COINS.png`, `Icons/Icon_Coins.png`. Sirf existing real local-wallet/game functionality se connect karo.

### 4. Garage aur submenus

`Assets/ImportedAssets/CarMenuUI/Sub_Buttons/` mein exact files:
- `Button_SELECT.png`, `Button_PURCHASE.png`, `Button_BUY.png`, `Button_EQUIP.png`, `Button_SELECTED.png`, `Button_LOCKED.png`.
- `Button_UPGRADE.png`, `Button_PAINT.png`, `Button_WHEELS.png`, `Button_NEXT.png`, `Button_PREVIOUS.png`, `Button_BACK.png`, `Button_CLOSE.png`.
- `Button_CONFIRM.png`, `Button_CANCEL.png`, `Button_YES.png`, `Button_NO.png`.
- `Button_MUSIC.png`, `Button_SOUND.png`, `Button_GRAPHICS.png`, `Button_CONTROLS.png`, `Button_ON.png`, `Button_OFF.png`, `Button_LOW.png`, `Button_MEDIUM.png`, `Button_HIGH.png`, `Button_RESET.png`, `Button_SAVE.png`.

Assets ko UI ke appropriate real actions se connect karo. Garage mein next/previous, car select, locked/selected state aur existing save/selection flow correctly wire karo. Car preview aur actual selected driving car consistent ho. Settings mein audio/graphics ke real supported controls, save and restore do. Unsupported purchasing/customization ko disabled ya clearly coming-soon rakho; real-money transactions, monetization SDK aur fake purchase flow bina user permission aur configured backend/SDK ke mat banao.

### 5. Unity import aur runtime access

1. PNG texture import `Sprite (2D and UI)`, `Single`, alpha transparency; UI Image par proper sprite assign karo. Labeled images ke upar default button label text double na render ho; UI ko 4:1 ratio maintain karna chahiye.
2. Existing code runtime mein UI generate karta ho sakta hai. **Important:** `Assets/ImportedAssets/CarMenuUI/` `Resources` ke andar nahi hai; `Resources.Load` is path ko directly load nahi karega. Project architecture ke hisaab se serialized sprite references, project-compatible editor asset setup, ya `Assets/Resources/...` mein controlled copy/move with preserved `.meta`/references choose karo. Koi editor-only API runtime build mein use mat karo. ZIP aur preview image ko runtime assets ki tarah load mat karo.
3. Source PNGs high resolution hain. Mobile build mein platform Max Size/compression/atlas settings assess karo; many 4K sprites full-resolution par load karne se GPU memory badhegi. Background ke liye original 1672 × 941 quality limitation mention karo.
4. Button graphics themselves OnClick logic nahi dete. Correct button callbacks, back navigation, disable states, safe-area layout and touch behavior implement and verify karo.

### 6. Tests aur final report

- Test home → Play → actual open-world game, garage → choose car → Play, settings → save → reopen, back/close, exit confirmation, locked/selected states. Existing controls, showroom, camera and physics ko regression-test karo.
- Android/phone aur multiple aspect ratios par possible ho to touch layout, responsiveness aur memory test karo. Missing Unity Editor or device ho to **NOT TESTED** clearly likho; success fabricate mat karo.
- Console missing sprite/script, broken callbacks, duplicate EventSystem/camera/audio, materials aur build errors check karo. Current scene/build settings ka actual path report karo.
- Final response mein exact changed files, button-to-action mapping, PNG import/loading method, settings/save behavior, tests and blockers do. User data aur original files delete mat karo; repo mein changes tabhi push karo jab is task mein explicitly requested ho.

## PROMPT END
