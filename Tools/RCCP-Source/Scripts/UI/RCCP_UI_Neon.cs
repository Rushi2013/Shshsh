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

/// <summary>
/// UI neon button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Neon Button")]
public class RCCP_UI_Neon : RCCP_UIComponent {

    /// <summary>
    /// Target material.
    /// </summary>
    public Material material;

    public void Upgrade() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        //  If player vehicle doesn't have the decal manager component, return.
        if (!playerVehicle.Customizer.NeonManager)
            return;

        //  Set the decal.
        playerVehicle.Customizer.NeonManager.Upgrade(material);

    }

}
