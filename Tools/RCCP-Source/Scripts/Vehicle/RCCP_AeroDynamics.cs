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
/// Manages the dynamics of the vehicle.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Dynamics")]
public class RCCP_AeroDynamics : RCCP_Component {

    /// <summary>
    /// Centre of mass.
    /// </summary>
    public Transform COM {

        get {

            if (com == null) {

                if (transform.Find("COM")) {

                    com = transform.Find("COM");
                    return com;

                }

                GameObject newCom = new GameObject("COM");
                newCom.transform.SetParent(transform, false);
                com = newCom.transform;
                com.transform.localPosition = new Vector3(0f, -.3f, 0f);

                return com;

            }

            return com;

        }
        set {

            com = value;

        }

    }

    private Transform com;

    [Min(0f)] public float downForce = 10f;

    /// <summary>
    /// Downforce will be applied to the vehicle with speed related. Vehicle will be more controllable on higher speeds.
    /// </summary>
    [Range(0f, 100f)] public float airResistance = 10f;

    /// <summary>
    /// Decceleration will be applied to the vehicle with speed related. Vehicle will slow down on higher values.
    /// </summary>
    [Range(0f, 100f)] public float wheelResistance = 10f;

    /// <summary>
    /// Sets COM position in te update method. Enable it if you are going to change COM position at runtime.
    /// </summary>
    public bool dynamicCOM = false;

    /// <summary>
    /// Resets the vehicle if upside down.
    /// </summary>
    public bool autoReset = true;

    /// <summary>
    /// Timer for reset.
    /// </summary>
    [Min(0f)] public float autoResetTime = 3f;
    private float autoResetTimer = 0f;

    public override void Start() {

        base.Start();

        //  Assigning center of mass position.
        CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

    }

    private void FixedUpdate() {

        //  Assigning center of mass position.
        if (dynamicCOM)
            CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

        //  Applying downforce to the vehicle.
        CarController.Rigid.AddRelativeForce(Vector3.down * downForce * Mathf.Abs(CarController.speed), ForceMode.Force);

        float correctedForce = airResistance * Mathf.Lerp(0f, 50f, Mathf.Abs(CarController.speed / 180f));

        //  Applying air drag resistance to the vehicle.
        CarController.Rigid.AddRelativeForce(-Vector3.forward * correctedForce * CarController.direction, ForceMode.Force);

        int direction = 1;

        if (transform.InverseTransformDirection(CarController.Rigid.velocity).z < 0)
            direction = -1;

        float correctedWheelForce = wheelResistance * Mathf.Lerp(0f, 1f, CarController.absoluteSpeed);
        correctedWheelForce *= 1f - CarController.throttleInput_V;
        correctedWheelForce *= 15f;

        if (CarController.IsGrounded)
            CarController.Rigid.AddRelativeForce(-Vector3.forward * correctedWheelForce * direction, ForceMode.Force);

        //  If auto-reset is enabled, check upside down.
        if (autoReset)
            CheckUpsideDown();

    }

    /// <summary>
    /// Resets the car if upside down.
    /// </summary>
    private void CheckUpsideDown() {

        //  If vehicle speed is below 5 and upside down, it will count to the target seconds and resets the vehicle.
        if (Mathf.Abs(CarController.speed) < 5 && !CarController.Rigid.isKinematic) {

            if (CarController.transform.eulerAngles.z < 300 && CarController.transform.eulerAngles.z > 60) {

                autoResetTimer += Time.deltaTime;

                if (autoResetTimer > autoResetTime) {

                    CarController.transform.SetPositionAndRotation(

                        new Vector3(CarController.transform.position.x, CarController.transform.position.y + 3, CarController.transform.position.z),
                        Quaternion.Euler(0f, CarController.transform.eulerAngles.y, 0f)

                        );

                    autoResetTimer = 0f;

                }

            }

        }

    }

    public void Reload() {

        autoResetTimer = 0f;

    }

}
