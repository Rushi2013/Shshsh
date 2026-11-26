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
/// ABS = Anti-skid braking system.
/// ESP = Detects vehicle skidding movements, and actively counteracts them.
/// TCS = Detects if a loss of traction occurs among the vehicle's wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Stability")]
public class RCCP_Stability : RCCP_Component {

    public RCCP_Axles AxleManager;
    public RCCP_Axle frontAxle;
    public RCCP_Axle rearAxle;

    public List<RCCP_Axle> poweredAxles = new List<RCCP_Axle>();
    public List<RCCP_Axle> steeringAxles = new List<RCCP_Axle>();
    public List<RCCP_Axle> brakedAxles = new List<RCCP_Axle>();

    /// <summary>
    /// ABS = Anti-skid braking system.
    /// </summary>
    public bool ABS = true;

    /// <summary>
    /// ESP = Detects vehicle skidding movements, and actively counteracts them.
    /// </summary>
    public bool ESP = true;

    /// <summary>
    /// TCS = Detects if a loss of traction occurs among the vehicle's wheels.
    /// </summary>
    public bool TCS = true;

    /// <summary>
    /// Engage point of the ABS. Will be more sensitive on lower values.
    /// </summary>
    [Range(.01f, .5f)] public float engageABSThreshold = .35f;

    /// <summary>
    /// Engage point of the ESP. Will be more sensitive on lower values.
    /// </summary>
    [Range(.01f, .5f)] public float engageESPThreshold = .5f;

    /// <summary>
    /// Engage point of the TCS. Will be more sensitive on lower values.
    /// </summary>
    [Range(.01f, .5f)] public float engageTCSThreshold = .35f;

    /// <summary>
    /// ABS intensity.
    /// </summary>
    [Range(0f, 1f)] public float ABSIntensity = 1f;

    /// <summary>
    /// ESP intensity.
    /// </summary>
    [Range(0f, 1f)] public float ESPIntensity = .5f;

    /// <summary>
    /// TCS intensity.
    /// </summary>
    [Range(0f, 1f)] public float TCSIntensity = 1f;

    /// <summary>
    /// ABS engaged now.
    /// </summary>
    public bool ABSEngaged = false;

    /// <summary>
    /// ESP engaged now.
    /// </summary>
    public bool ESPEngaged = false;

    /// <summary>
    /// TCS engaged now.
    /// </summary>
    public bool TCSEngaged = false;

    /// <summary>
    /// Steering helper. Add forces to right or left side depends on vehicle velocity.
    /// </summary>
    public bool steeringHelper = true;

    /// <summary>
    /// Traction helper. Reduces stiffness of the front wheels if vehicle is skidding to avoid spins.
    /// </summary>
    public bool tractionHelper = true;

    /// <summary>
    /// Angular drag helper. Adjusts angular drag intensity depends on the vehicle speed.
    /// </summary>
    public bool angularDragHelper = false;

    /// <summary>
    /// Limits the maximum drift angle.
    /// </summary>
    public bool driftAngleLimiter = false;

    /// <summary>
    /// Maximum drift angle.
    /// </summary>
    [Range(0f, 90f)] public float maxDriftAngle = 30f;

    /// <summary>
    /// Maximum drift angle correction factor.
    /// </summary>
    [Range(0f, 10f)] public float driftAngleCorrectionFactor = 3f;

    /// <summary>
    /// Steering helper strength.
    /// </summary>
    [Range(0f, 1f)] public float steerHelperStrength = .1f;

    /// <summary>
    /// Traction helper strength.
    /// </summary>
    [Range(0f, 1f)] public float tractionHelperStrength = .1f;

    /// <summary>
    /// Angular drag helper strength.
    /// </summary>
    [Range(0f, 1f)] public float angularDragHelperStrength = .1f;

    private float angle, angularVelo;

    public override void Start() {

        AxleManager = CarController.AxleManager;
        frontAxle = CarController.FrontAxle;
        rearAxle = CarController.RearAxle;
        poweredAxles = CarController.PoweredAxles;
        steeringAxles = CarController.SteeredAxles;
        brakedAxles = CarController.BrakedAxles;

    }

    private void FixedUpdate() {

        if (ESP)
            UpdateESP();

        if (TCS)
            UpdateTCS();

        if (ABS)
            UpdateABS();

        if (steeringHelper)
            SteerHelper();

        if (tractionHelper)
            TractionHelper();

        if (angularDragHelper)
            AngularDragHelper();

        if (driftAngleLimiter)
            LimitDriftAngle();

        if (RCCPSettings.SelectedBehaviorType != null && RCCPSettings.SelectedBehaviorType.driftMode)
            Drift();

    }

    private void Drift() {

        float rearWheelSlipAmountForward = 0f;
        float rearWheelSlipAmountSideways = 0f;

        if (rearAxle) {

            rearWheelSlipAmountForward = (rearAxle.leftWheelCollider.wheelSlipAmountForward + rearAxle.rightWheelCollider.wheelSlipAmountForward) * .5f;
            rearWheelSlipAmountSideways = (rearAxle.leftWheelCollider.wheelSlipAmountSideways + rearAxle.rightWheelCollider.wheelSlipAmountSideways) * .5f;

        }

        float pRearWheelSlipAmountForward = (rearWheelSlipAmountForward * rearWheelSlipAmountForward) * Mathf.Sign(rearWheelSlipAmountForward);
        float pRearWheelSlipAmountSideways = (rearWheelSlipAmountSideways * rearWheelSlipAmountSideways) * Mathf.Sign(rearWheelSlipAmountSideways);

        Transform comTransform = transform;
        RCCP_AeroDynamics aeroDynamics = CarController.AeroDynamics;

        if (aeroDynamics && aeroDynamics.COM != null)
            comTransform = aeroDynamics.COM;

        CarController.Rigid.AddRelativeTorque(Vector3.up * CarController.steerInput_P * CarController.direction * 1f, ForceMode.Acceleration);

        CarController.Rigid.AddForceAtPosition(transform.forward * 3500f * Mathf.Abs(pRearWheelSlipAmountSideways) * Mathf.Clamp01(Mathf.Abs(pRearWheelSlipAmountForward * 10f)) * CarController.direction, comTransform.position, ForceMode.Force);
        CarController.Rigid.AddForceAtPosition(transform.right * 1000f * pRearWheelSlipAmountSideways * Mathf.Clamp01(Mathf.Abs(Mathf.Clamp(pRearWheelSlipAmountForward, .5f, 1f) * 10f)) * CarController.direction, comTransform.position, ForceMode.Force);

    }

    /// <summary>
    /// ABS.
    /// </summary>
    private void UpdateABS() {

        //  Setting abs to false before checking it.
        ABSEngaged = false;

        //  If no axle found, return.
        if (AxleManager == null)
            return;

        //  Finding braked axles. If not found, return.
        if (brakedAxles == null)
            return;

        //  Finding braked axles. If not found, return.
        if (brakedAxles.Count < 1)
            return;

        //  Checking braked axles and their connected wheel's forward slip amount. If vehicle is applying brake, and wheels are skidding, engage ABS. Otherwise, disable it.
        for (int i = 0; i < brakedAxles.Count; i++) {

            if ((Mathf.Abs(brakedAxles[i].leftWheelCollider.wheelSlipAmountForward) * brakedAxles[i].brakeInput) >= engageABSThreshold) {

                brakedAxles[i].leftWheelCollider.CutBrakeABS(ABSIntensity);
                ABSEngaged = true;

            }

            if ((Mathf.Abs(brakedAxles[i].rightWheelCollider.wheelSlipAmountForward) * brakedAxles[i].brakeInput) >= engageABSThreshold) {

                brakedAxles[i].rightWheelCollider.CutBrakeABS(ABSIntensity);
                ABSEngaged = true;

            }

        }

    }

    /// <summary>
    /// ESP.
    /// </summary>
    private void UpdateESP() {

        ESPEngaged = false;

        //  If no axle found, return.
        if (AxleManager == null)
            return;

        //  If there are no front and rear axle found, return.
        if (!frontAxle || !rearAxle)
            return;

        //  Getting front and rear wheels sideways slip.
        float frontSlip = frontAxle.leftWheelCollider.wheelSlipAmountSideways + frontAxle.rightWheelCollider.wheelSlipAmountSideways;
        float rearSlip = rearAxle.leftWheelCollider.wheelSlipAmountSideways + rearAxle.rightWheelCollider.wheelSlipAmountSideways;

        //  If front wheels are skidding, under steering. If rear wheels are skidding, over steering.
        bool underSteering, overSteering;

        if (Mathf.Abs(frontSlip) >= engageESPThreshold)
            underSteering = true;
        else
            underSteering = false;

        if (Mathf.Abs(rearSlip) >= engageESPThreshold)
            overSteering = true;
        else
            overSteering = false;

        //  If over steering or under steering, set esp engaged to true. Otherwise to false.
        if (overSteering || underSteering)
            ESPEngaged = true;

        //  If under steering, apply brake and cut traction of the corresponding wheel.
        if (underSteering) {

            if (frontAxle.isBrake) {

                frontAxle.leftWheelCollider.AddBrakeTorque((frontAxle.maxBrakeTorque * (ESPIntensity * .1f)) * Mathf.Clamp(-rearSlip, 0f, Mathf.Infinity));
                frontAxle.rightWheelCollider.AddBrakeTorque((frontAxle.maxBrakeTorque * (ESPIntensity * .1f)) * Mathf.Clamp(rearSlip, 0f, Mathf.Infinity));

            }

        }

        //  If over steering, apply brake and cut traction of the corresponding wheel.
        if (overSteering) {

            if (rearAxle.isBrake) {

                rearAxle.leftWheelCollider.AddBrakeTorque((rearAxle.maxBrakeTorque * (ESPIntensity * .1f)) * Mathf.Clamp(-frontSlip, 0f, Mathf.Infinity));
                rearAxle.rightWheelCollider.AddBrakeTorque((rearAxle.maxBrakeTorque * (ESPIntensity * .1f)) * Mathf.Clamp(frontSlip, 0f, Mathf.Infinity));

            }

        }

    }

    /// <summary>
    /// TCS.
    /// </summary>
    private void UpdateTCS() {

        //  Setting TCS engaged to false before checking it.
        TCSEngaged = false;

        //  If no axle found, return.
        if (AxleManager == null)
            return;

        //  If there are no powered axle found, return.
        if (poweredAxles == null)
            return;

        // If there are no powered axle found, return.
        if (poweredAxles.Count < 1)
            return;

        //  If powered axles found, check their forward slips. If they breaks the point, engage TCS.
        for (int i = 0; i < poweredAxles.Count; i++) {

            if (CarController.direction == 1) {

                if (poweredAxles[i].leftWheelCollider.wheelSlipAmountForward >= engageTCSThreshold) {

                    poweredAxles[i].leftWheelCollider.CutTractionTCS(TCSIntensity);
                    TCSEngaged = true;

                }

                if (poweredAxles[i].rightWheelCollider.wheelSlipAmountForward >= engageTCSThreshold) {

                    poweredAxles[i].rightWheelCollider.CutTractionTCS(TCSIntensity);
                    TCSEngaged = true;

                }

            } else if (CarController.direction == -1) {

                if (poweredAxles[i].leftWheelCollider.wheelSlipAmountForward <= -engageTCSThreshold) {

                    poweredAxles[i].leftWheelCollider.CutTractionTCS(TCSIntensity);
                    TCSEngaged = true;

                }

                if (poweredAxles[i].rightWheelCollider.wheelSlipAmountForward <= -engageTCSThreshold) {

                    poweredAxles[i].rightWheelCollider.CutTractionTCS(TCSIntensity);
                    TCSEngaged = true;

                }

            }

        }

    }

    /// <summary>
    /// Steering helper.
    /// </summary>
    private void SteerHelper() {

        //  If vehicle is not grounded, return.
        if (!CarController.IsGrounded)
            return;

        //  Return if kinematic rigid is enabled.
        if (CarController.Rigid.isKinematic)
            return;

        Vector3 localVelocity = transform.InverseTransformDirection(CarController.Rigid.velocity);
        localVelocity.x *= 1f - (steerHelperStrength * .15f);

        CarController.Rigid.velocity = transform.TransformDirection(localVelocity);
        CarController.Rigid.AddForce(-transform.right * localVelocity.x * steerHelperStrength * .015f, ForceMode.VelocityChange);

        int direction = 1;

        if (localVelocity.z < 0)
            direction = -1;

        // Adjusting angular velocity based on the steering angle and speed
        Vector3 angularVelocity = CarController.Rigid.angularVelocity;
        float adjustedSteerAngle = CarController.FrontAxle.steerAngle * .1f;

        adjustedSteerAngle *= direction;
        adjustedSteerAngle *= Mathf.Clamp(Mathf.InverseLerp(0f, 40f, CarController.absoluteSpeed), 0f, Mathf.Infinity);
        angularVelocity.y = Mathf.Lerp(angularVelocity.y, Mathf.Lerp(angularVelocity.y, adjustedSteerAngle, steerHelperStrength * .075f), 1f);

        if (steerHelperStrength > 0)
            CarController.Rigid.angularVelocity = angularVelocity;

    }

    /// <summary>
    /// Traction helper.
    /// </summary>
    private void TractionHelper() {

        // If vehicle is not grounded, return.
        if (!CarController.IsGrounded)
            return;

        //  Return if kinematic rigid is enabled.
        if (CarController.Rigid.isKinematic)
            return;

        //  If no front axle found, return.
        if (!frontAxle)
            return;

        //  Getting velocity of the vehicle and taking dot of the velocity with transform.up ref.
        Vector3 velocity = CarController.Rigid.velocity;
        velocity -= transform.up * Vector3.Dot(velocity, transform.up);
        velocity.Normalize();

        angle = -Mathf.Asin(Vector3.Dot(Vector3.Cross(transform.forward, velocity), transform.up));
        angularVelo = CarController.Rigid.angularVelocity.y;

        if (angle * frontAxle.steerAngle < 0)
            frontAxle.tractionHelpedSidewaysStiffness = (1f - Mathf.Clamp01(tractionHelperStrength * Mathf.Abs(angularVelo)));
        else
            frontAxle.tractionHelpedSidewaysStiffness = 1f;

    }

    /// <summary>
    /// Angular drag helper.
    /// </summary>
    private void AngularDragHelper() {

        CarController.Rigid.angularDrag = Mathf.Lerp(0f, 10f, (Mathf.Abs(CarController.speed) * angularDragHelperStrength) / 1000f);

    }

    private void LimitDriftAngle() {

        Vector3 velocity = CarController.Rigid.velocity;
        Vector3 forward = transform.forward;

        // Calculate the current drift angle
        float angle = Vector3.SignedAngle(forward, velocity, Vector3.up);

        // Check if the drift angle exceeds the maximum allowed
        if (Mathf.Abs(angle) > maxDriftAngle) {

            // Smoothly adjust the current velocity to the desired velocity
            CarController.Rigid.angularVelocity = Vector3.Lerp(CarController.Rigid.angularVelocity, Vector3.zero, Time.fixedDeltaTime * driftAngleCorrectionFactor);

        }

    }

    public void Reload() {

        ABSEngaged = false;
        ESPEngaged = false;
        TCSEngaged = false;
        angle = 0f;
        angularVelo = 0f;

    }

}
