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
/// Transmits the received power from the engine --> clutch --> gearbox to the axle. 
/// Open differential = RPM difference between both wheels will decide to which wheel needs more traction or not. 
/// Limited = almost same with open with slip limitation. Higher percents = more close to the locked system. 
/// Locked = both wheels will have the same traction.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Differential")]
public class RCCP_Differential : RCCP_Component {

    /// <summary>
    /// Overrides the differential with external data.
    /// </summary>
    public bool overrideDifferential = false;

    /// <summary>
    /// Differential types. Open differential = RPM difference between both wheels will decide to which wheel needs more traction or not. 
    /// Limited = almost same with open with slip limitation. Higher percents = more close to the locked system. 
    /// Locked = both wheels will have the same traction.
    /// </summary>
    public enum DifferentialType {

        Open,
        Limited,
        FullLocked,
        Direct

    }

    /// <summary>
    /// Differential types. Open differential = RPM difference between both wheels will decide to which wheel needs more traction or not. 
    /// Limited = almost same with open with slip limitation. Higher percents = more close to the locked system. 
    /// Locked = both wheels will have the same traction.
    /// </summary>
    public DifferentialType differentialType = DifferentialType.Limited;

    /// <summary>
    /// LSD.
    /// </summary>
    [Range(50f, 100f)] public float limitedSlipRatio = 80f;

    /// <summary>
    /// Final drive ratio multiplier. Faster accelerations and lower top speeds on higher values.
    /// </summary>
    [Min(0.01f)] public float finalDriveRatio = 3.73f;

    /// <summary>
    /// Received torque from the component. It should be the gearbox in this case.
    /// </summary>
    [Min(0f)] public float receivedTorqueAsNM = 0f;

    /// <summary>
    /// Delivered torque to the component. It should be an axle in this case.
    /// </summary>
    [Min(0f)] public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Left wheel rpm.
    /// </summary>
    public float leftWheelRPM = 0f;

    /// <summary>
    /// Right wheel rpm.
    /// </summary>
    public float rightWheelRPM = 0f;

    /// <summary>
    /// Wheel slip ratio.
    /// </summary>
    public float wheelSlipRatio = 0f;

    /// <summary>
    /// Left slip ratio.
    /// </summary>
    public float leftWheelSlipRatio = 0f;

    /// <summary>
    /// Right slip ratio.
    /// </summary>
    public float rightWheelSlipRatio = 0f;

    /// <summary>
    /// Output of the left wheel.
    /// </summary>
    public float outputLeft = 0f; 

    /// <summary>
    /// Output of the right wheel.
    /// </summary>
    public float outputRight = 0f;

    /// <summary>
    /// Connected axle to this differential. Each differential must have an axle.
    /// </summary>
    public RCCP_Axle connectedAxle;

    private void FixedUpdate() {

        //  Return if overriding the differential. This means an external class is adjusting differential inputs.
        if (overrideDifferential)
            return;

        //  Return if not connected to any axle.
        if (!connectedAxle)
            return;

        Gears();
        Output();

    }

    /// <summary>
    /// Calculating output torque of the left and right side.
    /// </summary>
    private void Gears() {

        //  Get rpm.
        if (connectedAxle.leftWheelCollider && connectedAxle.leftWheelCollider.isActiveAndEnabled)
            leftWheelRPM = Mathf.Abs(connectedAxle.leftWheelCollider.WheelCollider.rpm);
        else
            leftWheelRPM = 0f;

        if (connectedAxle.rightWheelCollider && connectedAxle.rightWheelCollider.isActiveAndEnabled)
            rightWheelRPM = Mathf.Abs(connectedAxle.rightWheelCollider.WheelCollider.rpm);
        else
            rightWheelRPM = 0f;

        //  Sum rpm and difference.
        float sumRPM = leftWheelRPM + rightWheelRPM;
        float diffRPM = leftWheelRPM - rightWheelRPM;

        //  Calculating the wheel slip ratio between left and right wheel.
        wheelSlipRatio = Mathf.InverseLerp(0f, sumRPM, Mathf.Abs(diffRPM));

        switch (differentialType) {

            //  If differential type is open...
            case DifferentialType.Open:

                if (Mathf.Sign(diffRPM) == 1) {

                    leftWheelSlipRatio = wheelSlipRatio;
                    rightWheelSlipRatio = -wheelSlipRatio;

                } else {

                    leftWheelSlipRatio = -wheelSlipRatio;
                    rightWheelSlipRatio = wheelSlipRatio;

                }

                break;

            //  If differential type is LSD...
            case DifferentialType.Limited:

                wheelSlipRatio *= Mathf.Lerp(1f, 0f, (limitedSlipRatio / 100f));

                if (Mathf.Sign(diffRPM) == -1) {

                    leftWheelSlipRatio = -wheelSlipRatio;
                    rightWheelSlipRatio = wheelSlipRatio;

                } else {

                    leftWheelSlipRatio = wheelSlipRatio;
                    rightWheelSlipRatio = -wheelSlipRatio;

                }

                break;

            //  If differential type is full locked...
            case DifferentialType.FullLocked:

                if (Mathf.Sign(diffRPM) == -1) {

                    leftWheelSlipRatio = -.5f;
                    rightWheelSlipRatio = .5f;

                } else {

                    leftWheelSlipRatio = .5f;
                    rightWheelSlipRatio = -.5f;

                }

                break;

            case DifferentialType.Direct:

                leftWheelSlipRatio = 0f;
                rightWheelSlipRatio = 0f;

                break;

        }

    }

    /// <summary>
    /// Overrides the differential output with given values.
    /// </summary>
    /// <param name="targetOutputLeft"></param>
    /// <param name="targetOutputRight"></param>
    public void OverrideDifferential(float targetOutputLeft, float targetOutputRight) {

        outputLeft = targetOutputLeft;
        outputRight = targetOutputRight;
        producedTorqueAsNM = outputLeft + outputRight;

        connectedAxle.isPower = true;
        connectedAxle.ReceiveOutput(targetOutputLeft, targetOutputRight);

    }

    /// <summary>
    /// Receive torque from the component.
    /// </summary>
    /// <param name="output"></param>
    public void ReceiveOutput(RCCP_Output output) {

        if (overrideDifferential)
            return;

        receivedTorqueAsNM = output.NM;

    }

    /// <summary>
    /// Output to the left and right wheels.
    /// </summary>
    private void Output() {

        producedTorqueAsNM = receivedTorqueAsNM * finalDriveRatio;

        outputLeft = producedTorqueAsNM / 2f;
        outputRight = producedTorqueAsNM / 2f;

        outputLeft -= producedTorqueAsNM * leftWheelSlipRatio;
        outputRight -= producedTorqueAsNM * rightWheelSlipRatio;

        connectedAxle.isPower = true;
        connectedAxle.ReceiveOutput(outputLeft, outputRight);

    }

    public void Reload() {

        leftWheelRPM = 0f;
        rightWheelRPM = 0f;
        wheelSlipRatio = 0f;
        leftWheelSlipRatio = 0f;
        rightWheelSlipRatio = 0f;
        outputLeft = 0f;
        outputRight = 0f;
        producedTorqueAsNM = 0f;

    }

}
