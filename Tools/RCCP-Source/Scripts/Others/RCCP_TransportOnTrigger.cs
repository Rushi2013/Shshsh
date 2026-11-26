//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Transport On Trigger")]
public class RCCP_TransportOnTrigger : RCCP_GenericComponent {

    public Transform transportToHere;
    public bool resetVelocity = true;
    public bool resetRotation = true;

    private void OnTriggerEnter(Collider other) {

        if (other.isTrigger)
            return;

        //  Getting car controller.
        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        //  If trigger is not a vehicle, return.
        if (!carController)
            return;

        if (resetRotation)
            RCCP.Transport(carController, transportToHere.position, transportToHere.rotation, resetVelocity);
        else
            RCCP.Transport(carController, transportToHere.position, carController.transform.rotation, resetVelocity);

    }

}
