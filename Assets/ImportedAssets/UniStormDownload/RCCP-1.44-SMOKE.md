# Realistic Car Controller Pro v1.44.0

The original RAR is split into five binary parts, not independently extractable archives. Reassemble the parts in order:

- macOS/Linux: `cat 'Realistic Car Controller Pro v1.44.0.rar.part01' 'Realistic Car Controller Pro v1.44.0.rar.part02' 'Realistic Car Controller Pro v1.44.0.rar.part03' 'Realistic Car Controller Pro v1.44.0.rar.part04' 'Realistic Car Controller Pro v1.44.0.rar.part05' > 'Realistic Car Controller Pro v1.44.0.rar'`
- Windows Command Prompt: `copy /b "Realistic Car Controller Pro v1.44.0.rar.part01"+"Realistic Car Controller Pro v1.44.0.rar.part02"+"Realistic Car Controller Pro v1.44.0.rar.part03"+"Realistic Car Controller Pro v1.44.0.rar.part04"+"Realistic Car Controller Pro v1.44.0.rar.part05" "Realistic Car Controller Pro v1.44.0.rar"`

Original length 203,915,353 bytes; SHA-256 `78337d1bc70766ad5a6a6f4d8c4493c207b8851a06ea48442bf7dfb9190e98b4`. Extract with a current RAR-compatible program; the archive contains a `.unitypackage`. Import that in Unity using Assets > Import Package > Custom Package. Split parts alone are not automatically imported.

## Smoke inspection

The package contains `Assets/Realistic Car Controller Pro/Prefabs/Particles/RCCP_WheelSlipAsphalt.prefab`, `RCCP_WheelSlipBurnout.prefab`, particle textures including `BurnoutSmoke.png`, and `Scripts/Vehicle/RCCP_Particles.cs`. The script configures per-wheel particles from ground material settings and enables them when its RCCP wheel reports skidding. The included ground-material asset maps asphalt particles and skidmarks. The package was inspected as files, not run in Unity; integration with other car controllers and project versions is not verified. Confirm that you have the necessary licence before importing or distributing the package.
