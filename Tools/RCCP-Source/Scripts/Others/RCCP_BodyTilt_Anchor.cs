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
using UnityEngine.Animations;

/// <summary>
/// Anchor of the body tilt component. All target transforms will be tilted along with this transform.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Body Tilt Anchor")]
public class RCCP_BodyTilt_Anchor : RCCP_GenericComponent {

    /// <summary>
    /// All target transforms will be tilted along with this transform.
    /// </summary>
    public List<Transform> targetTransforms = new List<Transform>();

    /// <summary>
    /// Tiltable components such as lights, exhausts, cameras, etc...
    /// </summary>
    public List<RCCP_Component> targetComponents = new List<RCCP_Component>();

    /// <summary>
    /// Adds new target.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool AddTarget(Transform target) {

        bool added = false;

        if (targetTransforms == null)
            targetTransforms = new List<Transform>();

        if (!targetTransforms.Contains(target)) {

            added = true;
            targetTransforms.Add(target);

        }

        return added;

    }

    /// <summary>
    /// Removes the target.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool RemoveTarget(Transform target) {

        bool removed = false;

        if (targetTransforms == null)
            targetTransforms = new List<Transform>();

        if (targetTransforms.Contains(target)) {

            removed = true;
            targetTransforms.Remove(target);

        }

        return removed;

    }

    /// <summary>
    /// Adds new target.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool AddComponentTarget(RCCP_Component target) {

        bool added = false;

        if (targetComponents == null)
            targetComponents = new List<RCCP_Component>();

        if (!targetComponents.Contains(target)) {

            added = true;
            targetComponents.Add(target);

        }

        return added;

    }

    /// <summary>
    /// Removes the target.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool RemoveComponentTarget(RCCP_Component target) {

        bool removed = false;

        if (targetComponents == null)
            targetComponents = new List<RCCP_Component>();

        if (targetComponents.Contains(target)) {

            removed = true;
            targetComponents.Remove(target);

        }

        return removed;

    }

    /// <summary>
    /// Clears the targets.
    /// </summary>
    /// <returns></returns>
    public bool ClearTargets() {

        bool removed = false;

        targetTransforms = new List<Transform>();
        targetComponents = new List<RCCP_Component>();

        return removed;

    }

    /// <summary>
    /// Checking the RCCP_ParentConst component on all target gameobjects.
    /// </summary>
    /// <returns></returns>
    public bool CheckParentConsts() {

        bool created = false;

        if (targetComponents == null)
            targetComponents = new List<RCCP_Component>();

        for (int i = 0; i < targetComponents.Count; i++) {

            if (targetComponents[i] != null) {

                if (targetComponents[i].transform.GetComponent<RCCP_ParentConst>() == null) {

                    created = true;

                    RCCP_ParentConst pc = targetComponents[i].gameObject.AddComponent<RCCP_ParentConst>();
                    ConstraintSource sc = new ConstraintSource();
                    sc.sourceTransform = transform;
                    sc.weight = 1f;
                    pc.ParentConstraint.AddSource(sc);
                    pc.ParentConstraint.constraintActive = true;

                }

            }

        }

        if (targetTransforms == null)
            targetTransforms = new List<Transform>();

        for (int i = 0; i < targetTransforms.Count; i++) {

            if (targetTransforms[i] != null) {

                if (targetTransforms[i].parent.GetComponent<RCCP_ParentConst>() == null) {

                    created = true;

                    GameObject newParentGO = new GameObject("ParentConstraint_" + targetTransforms[i].name);
                    newParentGO.transform.SetParent(GetComponentInParent<RCCP_CarController>(true).transform, true);
                    newParentGO.transform.localPosition = Vector3.zero;
                    newParentGO.transform.localRotation = Quaternion.identity;
                    newParentGO.transform.SetParent(targetTransforms[i].parent, true);

                    targetTransforms[i].SetParent(newParentGO.transform, true);

                    RCCP_ParentConst pc = newParentGO.AddComponent<RCCP_ParentConst>();
                    ConstraintSource sc = new ConstraintSource();
                    sc.sourceTransform = transform;
                    sc.weight = 1f;
                    pc.ParentConstraint.AddSource(sc);
                    pc.ParentConstraint.constraintActive = true;

                    ConfigurableJoint cJointParent = newParentGO.AddComponent<ConfigurableJoint>();

                    cJointParent.angularXMotion = ConfigurableJointMotion.Locked;
                    cJointParent.angularYMotion = ConfigurableJointMotion.Locked;
                    cJointParent.angularZMotion = ConfigurableJointMotion.Locked;

                    cJointParent.xMotion = ConfigurableJointMotion.Locked;
                    cJointParent.yMotion = ConfigurableJointMotion.Locked;
                    cJointParent.zMotion = ConfigurableJointMotion.Locked;

                    cJointParent.projectionAngle = 0f;
                    cJointParent.projectionDistance = 0f;
                    cJointParent.projectionMode = JointProjectionMode.PositionAndRotation;
                    cJointParent.connectedMassScale = 0f;

                    Rigidbody parentRigid = cJointParent.GetComponent<Rigidbody>();
                    parentRigid.interpolation = RigidbodyInterpolation.Interpolate;

                    cJointParent.connectedBody = GetComponentInParent<RCCP_CarController>(true).Rigid;

                    RCCP_DetachablePart detachablePartFound = targetTransforms[i].GetComponent<RCCP_DetachablePart>();

                    if (detachablePartFound) {

                        foreach (Transform tr in cJointParent.GetComponentsInChildren<Transform>(true))
                            tr.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

                        foreach (Transform tr in detachablePartFound.GetComponentsInChildren<Transform>(true))
                            tr.gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPDetachablePartLayer);

                        ConfigurableJoint cJointDetachablePart = detachablePartFound.GetComponent<ConfigurableJoint>();

                        cJointDetachablePart.connectedBody = cJointParent.GetComponent<Rigidbody>();
                        cJointDetachablePart.projectionAngle = 0f;
                        cJointDetachablePart.projectionDistance = 0f;
                        cJointDetachablePart.projectionMode = JointProjectionMode.PositionAndRotation;
                        cJointDetachablePart.connectedMassScale = 0f;

                        Rigidbody detachablePartRigid = detachablePartFound.GetComponent<Rigidbody>();
                        detachablePartRigid.interpolation = RigidbodyInterpolation.None;

                    }

                }

            }

        }

        return created;

    }

}
