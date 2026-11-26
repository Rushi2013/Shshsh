# Six additional garage car models

These are third-party derivative models obtained from commit `472d997fb8db83da4eb360cb05b6a4591cd258c6` of https://github.com/hunghdvn/car-racing . That project's `ASSETS.md` identifies the underlying Sketchfab authors and licence terms below. Original models and this downstream copy are not officially affiliated with or endorsed by the vehicle manufacturers. Names and logos may have separate trademark restrictions.

| File | Car | Creator | Licence | Original source |
|---|---|---|---|---|
| `r34.glb` | Nissan Skyline GT-R R34 | Lexyc16 | CC BY 4.0 | https://sketchfab.com/3d-models/nissan-skyline-gtr-r34-ff8fb2251dfa4bb9979e7022c5a6666c |
| `930.glb` | Porsche 911 Turbo (930) | Lionsharp | CC BY 4.0 | https://sketchfab.com/3d-models/porsche-911-turbo-8568d9d14a994b9cae59499f0dbed21e |
| `s15.glb` | Nissan Silvia S15 | autoNgraphic | CC BY 4.0 | https://sketchfab.com/3d-models/nissan-silvia-s15-18348c1698f74d9db1497a791bd21a2f |
| `centenario.glb` | Lamborghini Centenario LP 770-4 | SDC | CC BY 4.0 | https://sketchfab.com/3d-models/lamborghini-centenario-lp-770-4-d679af35b5694301a185c7454a700c73 |
| `terzo.glb` | Lamborghini Terzo Millennio | SDC | CC BY-NC 4.0 | https://sketchfab.com/3d-models/lamborghini-terzo-millennio-7ad3dffa9d344c3c978eafcc220cb709 |
| `carrera.glb` | Porsche 911 Carrera 4S | Lionsharp Studios | CC BY-SA 4.0 | https://sketchfab.com/3d-models/free-porsche-911-carrera-4s-d01b254483794de3819786d93e0e1ebf |

CC BY 4.0: https://creativecommons.org/licenses/by/4.0/ ; CC BY-NC 4.0: https://creativecommons.org/licenses/by-nc/4.0/ ; CC BY-SA 4.0: https://creativecommons.org/licenses/by-sa/4.0/ . Preserve creator credit, sources, licence and modification notices. The noncommercial model (`terzo.glb`) is not suitable for a monetized game without additional permission. The share-alike model (`carrera.glb`) carries CC BY-SA obligations for adaptations. Review the licences and trademarks yourself before distribution; a CC label does not guarantee that the upstream uploader held all rights.

## Changes and technical notes

The upstream project had already optimized and modified these models (decimation, texture compression and rig canonicalization). This copy additionally converts embedded WebP images to embedded PNG and removes the `EXT_texture_webp` requirement to improve importer compatibility. For `930.glb`, the paint material's clearcoat-normal texture reference was removed to avoid a glTF validation error involving missing tangents; this may slightly alter appearance. The original upstream bytes are not included here; obtain them from the pinned upstream commit if needed.

All six converted GLBs are below 30 MB and contain embedded images. The Khronos glTF validator reported zero errors and zero warnings on each converted file; this is a structural check, not a Unity rendering or driving test. Existing optional material extensions may be rendered differently depending on the Unity glTF importer/pipeline. They have not been integrated into the garage, wheel setup or car physics. Test model completeness, orientation, scale, textures, separate wheels and mobile performance before use. File size does not represent runtime memory usage.
