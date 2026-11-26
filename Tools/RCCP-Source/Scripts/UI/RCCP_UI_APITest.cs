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
/// Usage and examples of API. 
/// </summary>
public class RCCP_UI_APITest : RCCP_UIComponent {

    /// <summary>
    /// Reference vehicle to spawn.
    /// </summary>
    public RCCP_CarController spawnableVehicle;

    /// <summary>
    /// Spawned vehicle.
    /// </summary>
    private RCCP_CarController spawnedVehicle;

    public void SpawnRCC() {

        spawnedVehicle = RCCP.SpawnRCC(spawnableVehicle, Camera.main.transform.position, Camera.main.transform.rotation, false, false, false);

    }

    public void Register(bool state) {

        if (!spawnedVehicle)
            return;

        if (state)
            RCCP.RegisterPlayerVehicle(spawnedVehicle, false);
        else
            RCCP.DeRegisterPlayerVehicle();

    }

    public void SetCanControl(bool state) {

        if (!spawnedVehicle)
            return;

        RCCP.SetControl(spawnedVehicle, state);

    }

    public void SetEngine(bool state) {

        if (!spawnedVehicle)
            return;

        RCCP.SetEngine(spawnedVehicle, state);

    }

}
