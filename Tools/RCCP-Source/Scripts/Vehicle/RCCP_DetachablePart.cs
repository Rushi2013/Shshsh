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
using System.Threading.Tasks;

/// <summary>
/// Detachable part of the vehicle.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Detachable Part")]
public class RCCP_DetachablePart : RCCP_Component {

    /// <summary>
    /// Configurable Joint.
    /// </summary>
    private ConfigurableJoint joint;
    private ConfigurableJoint Joint {

        get {

            if (joint == null)
                joint = GetComponent<ConfigurableJoint>();

            return joint;

        }

        set {

            joint = value;

        }

    }

    /// <summary>
    /// Rigidbody
    /// </summary>
    private Rigidbody rigid;
    private Rigidbody Rigid {

        get {

            if (rigid == null)
                rigid = GetComponent<Rigidbody>();

            return rigid;

        }

    }

    /// <summary>
    /// 	Joint properties class.
    /// </summary>
    private RCCP_Joint jointProperties = new RCCP_Joint();

    /// <summary>
    /// Center of mass.
    /// </summary>
    public Transform COM;

    /// <summary>
    /// Collider.
    /// </summary>
    [HideInInspector] public Collider[] partColliders;

    /// <summary>
    /// Detachable part type.
    /// </summary>
    public enum DetachablePartType { Hood, Trunk, Door, Bumper_F, Bumper_R, Other }

    /// <summary>
    /// Detachable part type.
    /// </summary>
    public DetachablePartType partType = DetachablePartType.Hood;

    /// <summary>
    /// Lock all motions of Configurable Joint at start.
    /// </summary>
    public bool lockAtStart = true;

    /// <summary>
    /// Strength of the part. 
    /// </summary>
    [Min(0f)] public float strength = 100f;

    /// <summary>
    /// Original strength of the part. We will be using this original value while restoring the part.
    /// </summary>
    [Min(0f)] internal float orgStrength = 100f;

    /// <summary>
    /// Can it break at certain damage?
    /// </summary>
    public bool isDetachable = true;

    /// <summary>
    /// Is this part broken currently?
    /// </summary>
    private bool broken = false;

    /// <summary>
    /// Part will be broken at this point.
    /// </summary>
    [Min(0)] public int loosePoint = 50;

    /// <summary>
    /// Part will be detached at this point.
    /// </summary>
    [Min(0)] public int detachPoint = 0;

    /// <summary>
    /// Part will be deactivated after the detachment.
    /// </summary>
    [Min(0f)] public float deactiveAfterSeconds = 5f;

    /// <summary>
    /// Adds angular velocity related to speed after the brake point reached.
    /// </summary>
    public Vector3 addTorqueAfterLoose = Vector3.zero;

    public override void Awake() {

        base.Awake();

        orgStrength = strength;     //	Getting original strength of the part. We will be using this original value while restoring the part.

        //  Getting collider.
        partColliders = GetComponentsInChildren<Collider>(true);

#if UNITY_2022_2_OR_NEWER
        LayerMask curLayerMask = -1;

        foreach (Collider collider in partColliders) {

            curLayerMask = collider.excludeLayers;
            curLayerMask |= (1 << LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPLayer));
            collider.excludeLayers = curLayerMask;

        }
#endif

        //	Setting center of mass if selected.
        if (COM)
            Rigid.centerOfMass = transform.InverseTransformPoint(COM.transform.position);

        //	Disable the script if configurable joint not found.
        if (!Joint) {

            Debug.LogWarning("Configurable Joint not found for " + gameObject.name + "!");
            enabled = false;
            return;

        }

        //	Getting original properties of the joint. We will be using the original data for restoring the part while repairing.
        GetJointProperties();

        //	Locks all motions of Configurable Joint at start.
        if (lockAtStart)
            RCCP_Joint.LockPart(Joint);

    }

    private IEnumerator Fix() {

        yield return new WaitForFixedUpdate();
        Rigid.interpolation = RigidbodyInterpolation.Interpolate;
        yield return new WaitForFixedUpdate();
        Rigid.interpolation = RigidbodyInterpolation.None;

    }

    /// <summary>
    /// Getting original properties of the joint. We will be using the original data for restoring the part while repairing.
    /// </summary>
    private void GetJointProperties() {

        jointProperties = new RCCP_Joint();
        jointProperties.GetProperties(Joint);

    }

    private void Update() {

        // If part is broken, return.
        if (broken)
            return;

        //	If part is weak and loosen, apply angular velocity related to vehicle speed.
        if (addTorqueAfterLoose != Vector3.zero && strength <= loosePoint) {

            float speed = transform.InverseTransformDirection(Rigid.velocity).z;        //	Local speed.
            Rigid.AddRelativeTorque(new Vector3(addTorqueAfterLoose.x * speed, addTorqueAfterLoose.y * speed, addTorqueAfterLoose.z * speed));      //	Applying local torque.

        }

    }

    public void OnCollision(float impulse) {

        if (!enabled)
            return;

        // If part is broken, return.
        if (broken)
            return;

        //	Decreasing strength of the part related to collision impulse.
        strength -= impulse * 5f;
        strength = Mathf.Clamp(strength, 0f, Mathf.Infinity);

        //	Check joint of the part based on strength.
        CheckJoint();

    }

    /// <summary>
    /// Checks joint of the part based on strength.
    /// </summary>
    private void CheckJoint() {

        // If part is broken, return.
        if (broken)
            return;

        // If strength is 0, unlock the parts and set their joint limits to none. Detach them from the vehicle. If strength is below detach point, only set joint limits to none.
        if (isDetachable && strength <= detachPoint) {

            if (Joint) {

                broken = true;
                RCCP_Joint.LoosePart(Joint);
                transform.SetParent(null);
                StartCoroutine(DisablePart(deactiveAfterSeconds));

            }

        } else if (strength <= loosePoint) {

            if (Joint) {

                Joint.angularXMotion = jointProperties.jointMotionAngularX;
                Joint.angularYMotion = jointProperties.jointMotionAngularY;
                Joint.angularZMotion = jointProperties.jointMotionAngularZ;

                Joint.xMotion = jointProperties.jointMotionX;
                Joint.yMotion = jointProperties.jointMotionY;
                Joint.zMotion = jointProperties.jointMotionZ;

            }

        }

    }

    /// <summary>
    /// Repairs, and restores the part.
    /// </summary>
    public void OnRepair() {

        // Setting strength to original strength value. And make sure part is not broken anymore.
        strength = orgStrength;
        broken = false;

        //	If joint is removed and part is detached, adding new configurable joint component. Configurable Joints cannot be toggled on or off. Therefore, we need to destroy and create configurable joints.
        if (Joint == null) {

            // Setting properties of the configurable joint to original properties.
            Joint = gameObject.AddComponent<ConfigurableJoint>();

        }

        jointProperties.SetProperties(Joint);
        RCCP_Joint.LockPart(Joint);

        // Enabling gameobject first if it's disabled.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        StartCoroutine(nameof(Fix));

    }

    /// <summary>
    /// Disables the part with delay.
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator DisablePart(float delay) {

        yield return new WaitForSeconds(delay);

        if (broken)
            gameObject.SetActive(false);

        yield return null;

    }

    private void Reset() {

        if (!COM) {

            COM = new GameObject("COM").transform;
            COM.SetParent(transform, false);
            COM.localPosition = Vector3.zero;
            COM.localRotation = Quaternion.identity;

        }

        ConfigurableJoint cJoint = GetComponent<ConfigurableJoint>();

        if (!cJoint)
            cJoint = gameObject.AddComponent<ConfigurableJoint>();

        cJoint.connectedBody = GetComponentInParent<Rigidbody>(true);
        cJoint.massScale = 1f;
        cJoint.connectedMassScale = 0f;

        Rigidbody cJointRigid = cJoint.GetComponent<Rigidbody>();

        if (cJointRigid) {

            cJointRigid.mass = 10f;
            cJointRigid.drag = 0f;
            cJointRigid.angularDrag = .05f;
            cJointRigid.interpolation = RigidbodyInterpolation.None;
            cJointRigid.collisionDetectionMode = CollisionDetectionMode.Discrete;

        }

        if (partColliders == null || (partColliders != null && partColliders.Length < 1))
            partColliders = GetComponentsInChildren<Collider>(true);

    }

}
