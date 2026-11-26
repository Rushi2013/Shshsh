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
/// Fills up the fuel tank of the target vehicle. Must be added to the box collider with trigger enabled.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Gas Station")]
public class RCCP_GasStation : RCCP_GenericComponent {

    [Min(0f)] public float refillSpeed = 1f;

    private void OnTriggerStay(Collider other) {

        //  Getting car controller.
        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        //  If car controller not found, return.
        if (!carController)
            return;

        //  If other addons manager is missing, return.
        if (!carController.OtherAddonsManager)
            return;

        //  If fuel tank is missing, return.
        if (!carController.OtherAddonsManager.FuelTank)
            return;

        //  Refill the fuel tank.
        if (carController.Damage)
            carController.OtherAddonsManager.FuelTank.Refill(refillSpeed);

    }

}
