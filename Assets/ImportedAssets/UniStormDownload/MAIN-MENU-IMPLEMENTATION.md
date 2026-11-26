# Main menu implementation notes

Reference asset: user-supplied `Assets/ImportedAssets/UniStormDownload/MainMenu.rar.part01` (single part, SHA-256 verified in CI), containing only `MainMenu.unity`. The scene was extracted to `Tools/MainMenuReference/` for layout inspection and deliberately **not imported** into `Assets/`: it contains shooter-themed panels and references external GUIDs whose sprites/scripts are not in the archive. No shooting content was copied.

The new menu uses the scene's general organisation (Panel / Image / Text / Button hierarchy, separate Back panel, profile/coins-style badge) and real UI textures staged from the user's RCC Pro v1.44 asset:

- `Assets/Resources/UI/Menu/ButtonSprite.png`
- `Assets/Resources/UI/Menu/ButtonFade.png`
- `Assets/Resources/UI/Menu/Gradient.png`
- `Assets/Resources/UI/Menu/RCCP_Sprite.png`
- `Assets/Resources/UI/Menu/Marker.png`

Original package copies remain under `Assets/Realistic Car Controller Pro/Textures/UI/` with their package GUIDs. The Resources copies have fresh GUIDs so there is no duplicate-GUID conflict.

## Button mapping

| Button | Action |
|---|---|
| PLAY / DRIVE | Existing `HomeScreen.Play()` showroom teardown + real game activation, without a new scene loader |
| GARAGE | Opens the runtime garage; prefabs from `Resources/GarageCars` and `Resources/TrafficCars`, live thumbnails, SELECT changes showroom and gameplay visual and saves choice |
| CUSTOMIZE | Opens paint panel; six paint choices apply to renderer instances and persist. Wheel/body-kit options are labelled "coming soon" |
| SETTINGS | Master volume → `AudioListener.volume`; engine/SFX → `VehicleAudio.SetMasterVolume`; graphics → `QualitySettings.SetQualityLevel`; all persisted in PlayerPrefs |
| CREDITS | Lists the actual supplied assets; no invented licences |
| QUIT | Confirmation panel; `Application.Quit()` on supported platforms, iOS shows home-gesture guidance |
| DAILY +250 | Local once-per-day coin bonus; no ads, purchases or SDK |

## Persistence

`MenuSettings.cs` uses versioned `CityDrive.v2.*` PlayerPrefs keys for coins, selected car, paint, master/SFX volume, quality and the daily-bonus date. `GameBootstrap` restores the selected car and paint at startup before the menu is shown.

## Verification status

- Unity compile/build: **NOT RUN for these menu changes** (per user instruction: do not build the APK now).
- Editor play mode / Android device: **NOT RUN**.
- Asset reference integrity, button callbacks, safe-area/touch behaviour and persistence need the manual checklist in the user's prompt on a real device.

## Rollback

Revert this commit and restore the previous `HomeScreen.cs`, `GarageController.cs`, `UiTools.cs`, `GameBootstrap.cs` and `VehicleAudio.cs`. The new Resources UI textures and report file can be removed independently; no existing asset was overwritten.
