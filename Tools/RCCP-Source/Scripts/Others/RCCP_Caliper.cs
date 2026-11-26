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

/// <summary>
/// Rotates the brake caliper.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Visual Brake Caliper")]
public class RCCP_Caliper : RCCP_GenericComponent {

    /// <summary>
    /// Actual WheelCollider.
    /// </summary>
    public RCCP_WheelCollider wheelCollider;

    /// <summary>
    /// Creating new center pivot for correct position.
    /// </summary>
    private GameObject newPivot;

    /// <summary>
    /// Default local rotation.
    /// </summary>
    private Quaternion defLocalRotation;

    private void Awake() {

        //	No need to go further if no wheelcollider found.
        if (!wheelCollider) {

            Debug.LogError("WheelCollider is not selected for this caliper named " + transform.name);
            enabled = false;
            return;

        }

        //	Creating new center pivot for correct position.
        newPivot = new GameObject("Pivot_" + transform.name);
        newPivot.transform.SetParent(wheelCollider.WheelCollider.transform, false);
        transform.SetParent(newPivot.transform, true);

        //	Assigning default rotation.
        defLocalRotation = newPivot.transform.localRotation;

    }

    private void LateUpdate() {

        //	No need to go further if no wheelcollider or no wheelmodel found.
        if (!wheelCollider.wheelModel || !wheelCollider.WheelCollider)
            return;

        // Left or right side?
        int side = 1;

        //  If left side...
        if (wheelCollider.transform.localPosition.x < 0)
            side = -1;

        //	Re-positioning camber pivot.
        newPivot.transform.position = wheelCollider.wheelModel.position;
        newPivot.transform.localPosition += Vector3.up * wheelCollider.WheelCollider.suspensionDistance / 2f;

        //	Re-rotationing camber pivot.
        newPivot.transform.localRotation = defLocalRotation * Quaternion.Euler(wheelCollider.caster * side, wheelCollider.WheelCollider.steerAngle, wheelCollider.camber * side);

    }

}
