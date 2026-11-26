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
/// Applies body tilt to the chassis (visual only).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Body Tilt")]
public class RCCP_BodyTilt : RCCP_Component {

    private RCCP_BodyTilt_Anchor _anchor;

    /// <summary>
    /// Anchor to tilt. All other target gameobjects will move / rotate along with this anchor.
    /// </summary>
    public RCCP_BodyTilt_Anchor Anchor {

        get {

            if (_anchor == null)
                _anchor = GetComponentInChildren<RCCP_BodyTilt_Anchor>(true);

            if (_anchor == null) {

                GameObject newPivot = new GameObject("RCCP_BodyTilt_Anchor");
                newPivot.transform.SetParent(transform);
                newPivot.transform.localPosition = Vector3.zero;
                newPivot.transform.localRotation = Quaternion.identity;
                newPivot.AddComponent<RCCP_BodyTilt_Anchor>();
                _anchor = newPivot.GetComponent<RCCP_BodyTilt_Anchor>();

            }

            if (CarController.AeroDynamics)
                _anchor.transform.position = CarController.AeroDynamics.COM.position;

            return _anchor;

        }

    }

    /// <summary>
    /// Current tilt angle.
    /// </summary>
    private Vector2 currentAngle = Vector2.zero;

    /// <summary>
    /// Target rotation to tilt.
    /// </summary>
    private Quaternion rotation = Quaternion.identity;

    /// <summary>
    /// Maximum tilt angle.
    /// </summary>
    [Range(0f, 15f)] public float maxTiltAngle = 2f;

    /// <summary>
    /// Tilt multiplier.
    /// </summary>
    [Range(0f, 15f)] public float tiltMultiplier = 1f;

    /// <summary>
    /// Acceleration of the rigidbody.
    /// </summary>
    private float acceleration;

    private void FixedUpdate() {

        RCCP_WheelCollider[] allWheels = CarController.AllWheelColliders;

        if (allWheels != null && allWheels.Length > 0) {

            acceleration = 0f;

            for (int i = 0; i < allWheels.Length; i++) {

                //  Acceleration difference.
                acceleration += allWheels[i].wheelSlipAmountSideways;

            }

            acceleration /= allWheels.Length;

        } else {

            acceleration = 0f;

        }

        float sideways = -(acceleration * 2f);
        sideways *= tiltMultiplier;
        sideways = Mathf.Clamp(sideways, -maxTiltAngle, maxTiltAngle);

        //  Calculating the current angle.
        currentAngle = new Vector2(sideways, 0f);

        //  Set to 0 if the value is nan.
        if (float.IsNaN(currentAngle.x))
            currentAngle.x = 0f;

        //  Set to 0 if the value is nan.
        if (float.IsNaN(currentAngle.y))
            currentAngle.y = 0f;

    }

    private void Update() {

        //  Smoothly rotating the anchor. All other target gameobjects will be tilted along with this anchor.
        rotation = Quaternion.Euler(currentAngle.y, 0f, currentAngle.x);
        Anchor.transform.localRotation = Quaternion.Slerp(Anchor.transform.localRotation, rotation, Time.deltaTime * 5f);

    }

    /// <summary>
    /// Reload.
    /// </summary>
    public void Reload() {

        currentAngle = Vector2.zero;

        rotation = Quaternion.Euler(currentAngle.y, 0f, currentAngle.x);
        Anchor.transform.localRotation = rotation;

    }

}
