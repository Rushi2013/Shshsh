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
/// RCCP Camera will be parented to this gameobject when current camera mode is Hood Camera.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Hood Camera")]
public class RCCP_HoodCamera : RCCP_Component {

    public override void Start() {

        base.Start();

        CheckJoint();

    }

    /// <summary>
    /// Fixing shake bug of the rigid.
    /// </summary>
    public void FixShake() {

        StartCoroutine(FixShakeDelayed());

    }

    IEnumerator FixShakeDelayed() {

        //  If no rigid found, return.
        if (!GetComponent<Rigidbody>())
            yield break;

        yield return new WaitForFixedUpdate();
        GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;

    }

    /// <summary>
    /// Checking configurable joint.
    /// </summary>
    private void CheckJoint() {

        //  Getting configurable joint.
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        //  If no joint found, return.
        if (!joint)
            return;

        //  If connected body of the joint is null, set it to car controller itself.
        if (joint.connectedBody == null) {

            if (CarController) {

                joint.connectedBody = CarController.Rigid;

            } else {

                Debug.LogError("Hood camera of the " + transform.root.name + " has configurable joint with no connected body! Disabling rigid and joint of the camera.");
                Destroy(joint);

                Rigidbody rigid = GetComponent<Rigidbody>();

                if (rigid)
                    Destroy(rigid);

            }

        }

    }

    public void Reset() {

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        if (!joint)
            return;

        if (!GetComponentInParent<RCCP_CarController>(true))
            return;

        joint.connectedBody = GetComponentInParent<RCCP_CarController>(true).GetComponent<Rigidbody>();
        joint.connectedMassScale = 0f;

    }

}

