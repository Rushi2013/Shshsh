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
/// Teleports the vehicle in zone to the target spawn point.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Teleporter")]
public class RCCP_Teleporter : RCCP_GenericComponent {

    /// <summary>
    /// Target spawn point.
    /// </summary>
    public Transform spawnPoint;

    private void OnTriggerEnter(Collider col) {

        //  If trigger enabled for other collider, return.
        if (col.isTrigger)
            return;

        //  Getting car controller.
        RCCP_CarController carController = col.gameObject.GetComponentInParent<RCCP_CarController>();

        //  If no car controller found, return.
        if (!carController)
            return;

        //  Transport the vehicle.
        RCCP.Transport(carController, spawnPoint.position, spawnPoint.rotation);

    }

}
