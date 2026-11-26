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
/// Customization trigger used on customization demo scene. It will enable customization mode when player vehicle triggers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Customization Trigger")]
public class RCCP_CustomizationTrigger : RCCP_GenericComponent {

    /// <summary>
    /// Trigger object.
    /// </summary>
    public GameObject trigger;

    /// <summary>
    /// Current vehicle.
    /// </summary>
    private RCCP_CarController vehicle;

    private void OnTriggerEnter(Collider other) {

        //  Getting car controller.
        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        //  If trigger is not a vehicle, return.
        if (!carController)
            return;

        //  If vehicle doesn't have a customizer component, return.
        if (!carController.Customizer)
            return;

        if (!RCCP_CustomizationDemo.Instance) {

            Debug.LogError("RCCP_CustomizationDemo couldn't be found in the scene.");
            return;

        }

        //  Enable customization mode, disable trigger.
        RCCP_CustomizationDemo.Instance.EnableCustomization(carController);
        trigger.SetActive(false);
        vehicle = carController;

    }

    private void Update() {

        //  If no any vehicle triggered, return.
        if (!vehicle || trigger.activeSelf)
            return;

        //  If distance is higher than 30 meters, reenable the trigger again.
        if (vehicle && Vector3.Distance(transform.position, vehicle.transform.position) >= 30f) {

            trigger.SetActive(true);
            vehicle = null;

        }

    }

}
