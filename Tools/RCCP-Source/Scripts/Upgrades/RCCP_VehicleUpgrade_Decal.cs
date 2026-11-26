//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if BCG_URP
using UnityEngine.Rendering.Universal;
#endif

#if BCG_URP

/// <summary>
/// Upgradable decal.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Decal")]
[RequireComponent(typeof(DecalProjector))]
public class RCCP_VehicleUpgrade_Decal : RCCP_Component {

    private DecalProjector decalRenderer;     //  Renderer, actually a box.

    /// <summary>
    /// Sets target material of the decal.
    /// </summary>
    /// <param name="material"></param>
    public void SetDecal(Material material) {

        //  Getting the mesh renderer.
        if (!decalRenderer)
            decalRenderer = GetComponentInChildren<DecalProjector>();

        //  Return if renderer not found.
        if (!decalRenderer)
            return;

        //  Setting material of the renderer.
        decalRenderer.material = material;

    }

    public void OnValidate() {

        if (!GetComponent<DecalProjector>())
            return;

        DecalProjector dp = GetComponent<DecalProjector>();

        if (dp.scaleMode == DecalScaleMode.ScaleInvariant && dp.pivot == new Vector3(0, 0, .5f) && dp.drawDistance == 1000) {

            dp.scaleMode = DecalScaleMode.InheritFromHierarchy;
            dp.pivot = Vector3.zero;
            dp.drawDistance = 500f;

        }

    }

}

#else

/// <summary>
/// Upgradable decal.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Decal")]
public class RCCP_VehicleUpgrade_Decal : RCCP_Component {

    /// <summary>
    /// Sets target material of the decal.
    /// </summary>
    /// <param name="material"></param>
    public void SetDecal(Material material) {

        //Debug.LogError("Decals are working with URP only!");
        return;
         
    }

}
#endif