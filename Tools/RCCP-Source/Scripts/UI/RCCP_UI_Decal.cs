//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI decal button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Decal Button")]
public class RCCP_UI_Decal : RCCP_UIComponent {

    /// <summary>
    /// Target location of the decal. 0 is front, 1 is back, 2 is left, and 3 is right.
    /// </summary>
    [Min(0)] public int location = 0;

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
        if (!playerVehicle.Customizer.DecalManager)
            return;

        //  Set the decal.
        playerVehicle.Customizer.DecalManager.Upgrade(location, material);

    }

    /// <summary>
    /// Sets the location of the decal. 0 is front, 1 is back, 2 is left, and 3 is right.
    /// </summary>
    /// <param name="_location"></param>
    public void SetLocation(int _location) {

        location = _location;

    }

}
