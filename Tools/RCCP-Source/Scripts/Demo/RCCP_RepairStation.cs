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
/// Repairs the vehicle. Must be added to the box collider with trigger enabled.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Repair Station")]
public class RCCP_RepairStation : RCCP_GenericComponent {

    private void OnTriggerEnter(Collider other) {

        //  Getting car controller.
        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        //  If car controller not found, return.
        if (!carController)
            return;

        //  If vehicle has damage component, repair it.
        if (carController.Damage)
            carController.Damage.repairNow = true;

    }

}
