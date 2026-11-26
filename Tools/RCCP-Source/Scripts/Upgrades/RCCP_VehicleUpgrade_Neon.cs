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
/// Upgradable neon.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Neon")]
[RequireComponent(typeof(DecalProjector))]
public class RCCP_VehicleUpgrade_Neon : RCCP_Component {

    private DecalProjector neonRenderer;     //  Renderer, actually a box.

    /// <summary>
    /// Sets target material of the neon.
    /// </summary>
    /// <param name="material"></param>
    public void SetNeonMaterial(Material material) {

        //  Getting the mesh renderer.
        if (!neonRenderer)
            neonRenderer = GetComponentInChildren<DecalProjector>();

        //  Return if renderer not found.
        if (!neonRenderer)
            return;

        //  Setting material of the renderer.
        neonRenderer.material = material;

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
/// Upgradable neon.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Neon")]
public class RCCP_VehicleUpgrade_Neon : RCCP_Component {

    /// <summary>
    /// Sets target material of the neon.
    /// </summary>
    /// <param name="material"></param>
    public void SetNeonMaterial(Material material) {

        //Debug.LogError("Neons are working with URP only!");
        return;

    }

}
#endif