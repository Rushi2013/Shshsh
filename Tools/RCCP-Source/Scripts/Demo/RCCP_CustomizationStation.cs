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
/// Customization for the vehicle. Must be added to the box collider with trigger enabled.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Customization Station")]
public class RCCP_CustomizationStation : RCCP_GenericComponent {

    private void OnTriggerEnter(Collider other) {

        //  Getting car controller.
        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        //  If trigger is not a vehicle, return.
        if (!carController)
            return;

        //  If vehicle doesn't have a customizer component, return.
        if (!carController.Customizer)
            return;

        if (!RCCP_UIManager.Instance) {

            Debug.LogError("RCCP_UIManager couldn't be found in the scene.");
            return;

        }

        //  Enable customization mode.
        if (RCCP_UIManager.Instance.customization)
            RCCP_UIManager.Instance.customization.SetActive(true);

    }

}
