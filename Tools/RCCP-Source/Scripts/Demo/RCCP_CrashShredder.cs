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
/// Operates the shredder in the damage demo scene.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Crash Shredder")]
public class RCCP_CrashShredder : RCCP_GenericComponent {

    /// <summary>
    /// Hinge point of the joint.
    /// </summary>
    public Transform hingePoint;

    /// <summary>
    /// Rigidbody.
    /// </summary>
    private Rigidbody rigid;

    /// <summary>
    /// Direction of the force.
    /// </summary>
    public Vector3 direction;

    /// <summary>
    /// Strength of the force.
    /// </summary>
    public float force = 1f;

    private void Start() {

        //  Getting rigidbody.
        rigid = GetComponent<Rigidbody>();

        //  Creating hinge with configurable joint.
        CreateHinge();

    }

    private void FixedUpdate() {

        //  If no rigid, return.
        if (!rigid)
            return;

        //  Apply force.
        rigid.AddRelativeTorque(direction * force, ForceMode.Acceleration);

    }

    /// <summary>
    /// Creates hinge with configurable joint.
    /// </summary>
    private void CreateHinge() {

        GameObject hinge = new GameObject("Hinge_" + transform.name);
        hinge.transform.SetPositionAndRotation(hingePoint.position, hingePoint.rotation);

        Rigidbody hingeRigid = hinge.AddComponent<Rigidbody>();
        hingeRigid.isKinematic = true;
        hingeRigid.useGravity = false;

        AttachHinge(hingeRigid);

    }

    /// <summary>
    /// Sets connected body of the configurable joint.
    /// </summary>
    /// <param name="hingeRigid"></param>
    private void AttachHinge(Rigidbody hingeRigid) {

        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

        if (!joint) {

            print("Configurable Joint of the " + transform.name + " not found! Be sure this gameobject has Configurable Joint with right config.");
            return;

        }

        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = hingeRigid;
        joint.connectedAnchor = Vector3.zero;

    }

    private void Reset() {

        if (hingePoint == null) {

            hingePoint = new GameObject("Hinge Point").transform;
            hingePoint.SetParent(transform, false);

        }

    }

}
