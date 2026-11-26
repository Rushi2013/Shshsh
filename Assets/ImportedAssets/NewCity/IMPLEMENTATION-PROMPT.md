# Prompt: New city replace + road spawn + road-following traffic AI

Is prompt ko Unity coding agent ko do. Asset GitHub par upload hone ke baad path neeche diya hai; is file ko banane se game code implement nahi hua hai.

---

## PROMPT START

Mere existing Unity **open-world car game** ki old city ko neeche diye hue naye city model se replace karo. Player car **actual driveable road** par spawn ho aur traffic cars sirf isi city ke roads/lane paths par chalें—city bounds ya empty space par random nahi. Textures/materials, colliders, scale, orientation, gameplay aur mobile performance ke saath complete integration karo. Actual code/scene inspect karo aur jo test nahi kiya use pass mat batao.

### A. GitHub source aur files

- Repository: `https://github.com/Rushi2013/Shshsh`; branch `arena/411eaf4f-shshsh`. Latest branch par checkout karke existing user changes, Unity version, render pipeline aur build/runtime flow inspect karo.
- New asset folder: `Assets/ImportedAssets/NewCity/`.
- Extracted model: `Assets/ImportedAssets/NewCity/Model/CIT.obj` aur `CIT.mtl`.
- Full untouched original archive: `Assets/ImportedAssets/NewCity/OriginalArchive/OBJ.rar.part01`. Yeh ek hi complete part hai; `.part01` hata kar `OBJ.rar` banta hai. Original RAR size 21,370,627 bytes; SHA-256 `58c036d08562fd5f8ca65e08f3828888cb317f5cd9e19fd0e90dc6ea74dee330`.
- `Assets/ImportedAssets/NewCity/README.md` mein source aur inspection notes hain. Model folder ka extracted version mein do path fixes already applied hain: OBJ ka `mtllib MIASTO.mtl` → `mtllib CIT.mtl`; MTL mein sky image ka absolute Windows path → local filename. Original RAR unmodified hai. Duplicate model imports se bachna.
- Archive mein asset-specific copyright/licence clear nahi mila. `TexturesCom_*` aur HDRI images ke rights/usage conditions verify karo. Bina evidence ke commercial rights assume mat karo.

### B. Exact textures aur material mapping — evidence vs guess

Yeh 11 image files `Assets/ImportedAssets/NewCity/Model/` mein hain:

1. `free-hdri-sky-071.jpg`
2. `MonValley_A_LookoutPoint_8k.jpg`
3. `Roads Aerial.jpg`
4. `TexturesCom_BuildingsHighRise0368_1_seamless_S.jpg`
5. `TexturesCom_BuildingsHighRise0504_2_M.jpg`
6. `TexturesCom_BuildingsHighRise0530_1_seamless_S.jpg`
7. `TexturesCom_ConcreteBare0321_1_seamless_S.jpg`
8. `TexturesCom_HighRiseResidential0103_1_seamless_S.jpg`
9. `TexturesCom_HighRiseResidential0139_1_seamless_S.jpg`
10. `TexturesCom_HighRiseResidential0144_1_seamless_S.jpg`
11. `TexturesCom_Skies0206_1_M.jpg`

`CIT.mtl` mein 8 materials hain. **Sirf ek verified texture binding** hai:

- `TexturesCom_Skies0206_1_M` → `TexturesCom_Skies0206_1_M.jpg` (`map_Kd`).
- `Material.004`, `Material.006`, `Material.007`, `build3`, `build3.001`, `build3.002`, `wall`: MTL mein numeric `Kd` colors hain, **koi texture image map specified nahi**. Unhe correct material slots mein import karo aur initially source colors preserve karo.

Isliye `Roads Aerial.jpg` ya building JPGs ke exact intended assignments source se proven nahi hain. Visual/model/UV inspection ke baad hi suitable materials mein optional assignment karo; before/after document karo, unknown assignments ko invented fact mat batao. Actual road geometry/material identify ho to `Roads Aerial.jpg` ko test kar sakte ho, par road UV/scale/stretching suitability verify karo. Concrete aur building textures ko matching facade/sidewalk meshes par sirf visually verified hone par use karo. Sky JPG/HDRI ko existing sky/UniStorm system se conflict karte hue blindly activate mat karo. 8K image mobile memory cost high ho sakti hai; compression/import size tune karo.

OBJ mein `Plane`, `Plane.001`, `292041611...` aur `TexturesCom_Skies0206_1_M` jaise generic object names hain; `road` / `street` names guaranteed nahi. `Plane` par `Material.006`, `Plane.001` par `Material.007` hain, par unhe bina geometry inspection automatically road declare mat karo. Sky-plane collider ya texture ko drivable road na samjho. Source OBJ raw coordinate bounds roughly X `-269.35..185.24`, Y `-24.84..218.68`, Z `-396.14..117.45`; Blender→Unity import axis/scale/transform ki wajah se inko world coordinates assume mat karo.

### C. Current code ka starting point — actual branch verify karo

- `Assets/Scripts/GameBootstrap.cs` previously `CityResource = "City/city"` se old city load karta tha, `CityBuilder` banata aur `TrafficSystem.Build(city)` call karta tha.
- `Assets/Scripts/CityBuilder.cs` previously old city ka orientation, colliders, `Bounds`, `FindSpawnPoint()` aur `SpawnRotation` handle karta tha; road detection name substrings par depend karti hai. New OBJ mein generic names hain, to is heuristic kaam na kare to explicit classified surface metadata / imported child mesh mapping use karo.
- `Assets/Scripts/TrafficSystem.cs` previously city bounds se rectangular `BuildLoop` bana kar waypoint vehicles chalata tha. Yeh new roads ki guarantee nahi deta aur isliye **actual road network** se replace hona chahiye.
- Existing city ke exact asset path, prefab, runtime bootstrap, render setup aur current branch mein code changes ko source se verify karo. `Assets/Scripts/ArcadeCarController.cs`, `MobileControls.cs`, `HomeScreen.cs`, `ChaseCamera.cs` aur `ProjectSetup.cs` bhi inspect karo; existing physics, garage, menu, weather, saved data aur mobile controls break mat karo.

### D. New city import + old city replace

1. Unity Editor/asset pipeline mein `CIT.obj` + corrected `CIT.mtl` + JPGs import karo. OBJ material mapping automatic na ho to correct slots/materials manually assign karo. Use target project pipeline ke compatible shaders. Missing/pink materials ya textures fix karo; UV and repeat/wrap visually inspect karo. Agar source mismatch/blocker ho to exact report do.
2. Import settings (scale, axis, `Read/Write` only if required) aur transform set karo. City upright, consistent physically usable road width/car scale and correct ground orientation ho. OBJ ka raw bounding box sky-plane se inflated ho sakta hai: road bounds aur whole mesh bounds separate rakho.
3. Existing startup flow mein new city prefab/model ko reliable way se load karo. Agar runtime `Resources.Load` use hota hai, new imported asset us `Resources` path ke andar hona chahiye ya serialized scene reference/addressable/other project-supported method use karo. `Assets/ImportedAssets/...` ko directly `Resources.Load` path samajhkar assume mat karo. `.meta` GUIDs preserve karo, duplicate import avoid karo.
4. Old city ko **runtime mein replace/disable** karo; old city model aur big fallback roads dobara visible/collidable na rahen. Backup/undo-compatible approach use karo, unrelated folders delete mat karo. Agar old city ko remove karne se koi reference break hota hai to update karo. `CreateGroundFallback` ko inspect karo: uska giant flat collider traffic/vehicle ko empty space par chalne ka illusion na de. Existing fail-safe ko remove/restrict/disable in production after proper colliders; missing roads ke case mein warning do, unsafe invisible ground nahi.
5. Road/pavement/terrain ke liye accurate usable colliders, buildings/obstacles ke liye appropriate colliders; scene layers and collision matrix configure karo. MeshCollider performance, nonconvex static collision and ramp/curb geometry verify karo. Single huge collider ya per-triangle colliders blindly create mat karo; mobile-appropriate segmentation/optimization use karo.
6. Source texture mapping incomplete hai. Agar model mein actually usable drivable road surface na mile, to clearly report karo and obtain approval for minimal road geometry/texture fix; fake working model ya empty fallback ke upar traffic mat chalao.

### E. Player spawn real road par

1. Import ke baad road geometry, width, direction aur drivable collider manually identify/tag ya explicit references assign karo. `Plane` / `Plane.001` ko sirf preview, surface normal, UV, geometry aur collider check ke baad classify karo.
2. Actual road par **explicit player spawn marker** place karo, ya validated sampling karo: road-layer raycast, surface normal threshold, lane width, clearance for car bounds, nearby obstacle exclusion aur valid wheel support. Car ka front road direction ki taraf align karo. World-axis/old city coordinates ya raw OBJ coordinates hard-code mat karo.
3. No valid road mile to descriptive log/error and safe user-visible blocker do; player ko sky, building roof, empty space, below ground ya fallback plane par silently spawn mat karo.
4. Four wheel contact, slight height offset, slope, physics settling, Play transition, restart aur garage car swap test karo; car road ke neeche na gire ya hawa mein na rahe.

### F. Road-following traffic AI — sirf roads, not rectangle bounds

1. `TrafficSystem.BuildLoop(Bounds)` ke rectangle/fallback ko remove/replace karo. Actual visible drivable road network ke upar **explicit authored lane waypoints/splines** ya reliably extracted road graph banao. Generic-name-only automatic guessing is model mein sufficient nahi. Agar fully automatic road recognition verified na ho, editor tooling/gizmos + manually authored connected lane graph use karo.
2. Har lane point aur consecutive path segment ko actual road collider ke against validate karo—downward road-layer checks along entire segment (sirf endpoints nahi), slope, enough road width and obstacle clearance. City ke sky plane, building tops aur giant fallback plane road valid nahi. Sharp turns, sidewalks aur intersections ko visually inspect karo.
3. One/two-way roads ke hisaab se lane direction/offset sahi set karo; city mein jo lanes clearly identifiable hain unhi ko mark karo. Adjacent waypoints connect, turns smooth, speed curves reasonable. No arbitrary straight cuts across buildings, open gaps or pavements. Invalid/short route par traffic spawn mat karo; clear warning and actionable route-authoring steps do.
4. Vehicles existing `Resources/TrafficCars` ya current valid traffic prefabs se lo. Spawn on validated lane with car spacing and height alignment; no stacking/intersection blocking. Distance-based spawn/despawn, cap, pooling/mobile budget use karo. Frame-independent motion; supported colliders aur Rigidbody kinematic movement ke saath player collisions test karo.
5. Basic AI: follow lane, waypoint advance, turns mein slow, same-lane vehicle/player ke aage sensor se brake aur safe following distance; junction priority/yield + obstacle stop where practicable. Sensors appropriate layers ko use karein, khud ko ignore karein, continuously update safely. Road route end / blocked road handling documented; off-road, invisible ghost car, sudden teleport, arbitrary illegal lane movement avoid karo.
6. Route debug gizmos (lane direction, nodes, spawn, off-road validation errors) editor/development build mein available ho. If mesh/roads or editor missing, partial work ko pass mat bolo; no imaginary route coordinates.

### G. Acceptance tests and report

- Unity Editor mein imported city visual verification: orientation, material slots, actual 11 textures, sky, road geometry, scale, collisions; specific material-to-image assignments aur manual guesses list karo.
- Play se player car actual road par spawn kare and drive/brake/steer/handbrake/reverse, curb/ramp aur restart work kare; old city ke colliders aur flat infinite ground interfere na karein.
- 3+ connected road sections par multiple traffic vehicles observe karo: lane/slope alignment, no off-road movement, no building cuts, following/braking, intersections, player collision, scene reload and menu transitions. Invalid road geometry/route test par safe disable + informative warning.
- Android/mobile environment available ho to texture memory, draw calls, frame time, vehicle count and CPU test karo; Editor/device unavailable ho to **NOT RUN** likho and manual verification steps do. No fabricated screenshots or test status.
- Final report mein model and scene/prefab paths, any relocated/copied Resources path, actual `CIT.mtl` assignments, adjusted materials, road classification method, source texture uncertainties, player spawn coordinates (only after actual scene verification), lane graph/authoring file locations, AI settings, old city runtime removal, modified scripts, test results and blockers list karo. User files/data preserve karo. Push only when explicitly requested for that implementation task.

## PROMPT END
