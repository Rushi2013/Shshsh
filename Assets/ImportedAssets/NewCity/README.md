# New city asset

Source: https://www.mediafire.com/file/gduxzocld4mckwu/OBJ.rar/file

`OriginalArchive/OBJ.rar.part01` is the entire original RAR in one part (21,370,627 bytes; SHA-256 `58c036d08562fd5f8ca65e08f3828888cb317f5cd9e19fd0e90dc6ea74dee330`). Remove `.part01` to restore the original archive; no merging is needed.

`Model/` contains the extracted model, materials and all 11 JPG textures. The Windows thumbnail-cache file `Thumbs.db` is retained only in the original archive, not the extracted folder. Two text path corrections were made in this extracted copy only: `CIT.obj` references the existing `CIT.mtl` instead of missing `MIASTO.mtl`; `CIT.mtl` references local `TexturesCom_Skies0206_1_M.jpg` instead of an absolute Windows path. The original archive is unchanged.

The material file specifies eight materials and only one image map: `TexturesCom_Skies0206_1_M` → `TexturesCom_Skies0206_1_M.jpg`. The other seven materials have color parameters but no image map. The other JPGs are present but their intended material assignments cannot be determined from this MTL; inspect geometry, UVs and textures before assigning. `Roads Aerial.jpg` has a suggestive name but is not automatically linked to road geometry by the source MTL.

The source OBJ uses generic names such as `Plane`, `Plane.001` and numbered meshes; road geometry is not identified by a `road` name. The model has not been imported or run in Unity and road/surface mapping, scale, collision and traffic routes are not verified. No runtime city replacement is included in this asset-only commit. Review the texture and model licences before use or redistribution.
