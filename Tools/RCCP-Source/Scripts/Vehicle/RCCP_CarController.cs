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
/// Main car controller of the vehicle. Manages and observes every component attached to the vehicle.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Main/RCCP Car Controller")]
[DefaultExecutionOrder(-10)]
public class RCCP_CarController : RCCP_MainComponent {

    /// <summary>
    /// Is this vehicle controllable now? RCCP_Inputs (component) attached to the vehicle will receive inputs when enabled.
    /// </summary>
    public bool canControl = true;

    /// <summary>
    /// Is this vehicle controlled by an external controller?
    /// </summary>
    public bool externalControl = false;

    /// <summary>
    /// Selected behavior in RCCP_Settings won't affect this vehicle if this option is enabled.
    /// </summary>
    public bool ineffectiveBehavior = false;

    #region STATS

    /// <summary>
    /// Current engine rpm.
    /// </summary>
    public float engineRPM = 0f;

    /// <summary>
    /// Minimum engine rpm.
    /// </summary>
    public float minEngineRPM = 800f;

    /// <summary>
    /// Maximum engine rpm.
    /// </summary>
    public float maxEngineRPM = 8000f;

    /// <summary>
    /// Current gear.
    /// </summary>
    public int currentGear = 0;

    /// <summary>
    /// Current gear ratio.
    /// </summary>
    public float currentGearRatio = 1f;

    /// <summary>
    /// Last gear ratio.
    /// </summary>
    public float lastGearRatio = 1f;

    /// <summary>
    /// Differential ratio.
    /// </summary>
    public float differentialRatio = 1f;

    /// <summary>
    /// Speed of the vehicle.
    /// </summary>
    public float speed = 0f;

    [System.Obsolete("You can use ''speed'' variable instead of 'physicalSpeed'.")]
    public float physicalSpeed {

        get {

            return speed;

        }

    }

    //  Absolute speed of the vehicle.
    public float absoluteSpeed {

        get {

            return Mathf.Abs(speed);

        }

    }

    /// <summary>
    /// Wheel speed of the vehicle.
    /// </summary>
    public float wheelRPM2Speed = 0f;

    /// <summary>
    /// Maximum speed of the vehicle related to engine rpm, gear ratio, differential ratio, and wheel diameter.
    /// </summary>
    public float maximumSpeed = 0f;

    /// <summary>
    /// RPM of the traction wheels.
    /// </summary>
    public float tractionWheelRPM2EngineRPM = 0f;

    /// <summary>
    /// Target wheel speed for current gear.
    /// </summary>
    public float targetWheelSpeedForCurrentGear = 0f;

    /// <summary>
    /// Produced engine torque.
    /// </summary>
    public float producedEngineTorque = 0f;

    /// <summary>
    /// Produced gearbox torque.
    /// </summary>
    public float producedGearboxTorque = 0f;

    /// <summary>
    /// Produced differential torque.
    /// </summary>
    public float producedDifferentialTorque = 0f;

    /// <summary>
    /// 1 = Forward, -1 = Reverse.
    /// </summary>
    public int direction = 1;

    /// <summary>
    /// Is engine starting now?
    /// </summary>
    public bool engineStarting = false;

    /// <summary>
    /// Is engine running now?
    /// </summary>
    public bool engineRunning = false;

    /// <summary>
    /// Is gearbox shifting now?
    /// </summary>
    public bool shiftingNow = false;

    /// <summary>
    /// Is reversing now?
    /// </summary>
    public bool reversingNow = false;

    /// <summary>
    /// Current steer angle.
    /// </summary>
    public float steerAngle = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float fuelInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float throttleInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float brakeInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float steerInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float handbrakeInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float clutchInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float gearInput_V = 1f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    public float nosInput_V = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    public float throttleInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    public float brakeInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    public float steerInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    public float handbrakeInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    public float clutchInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    public float nosInput_P = 0f;

    /// <summary>
    /// Low beam headlights.
    /// </summary>
    public bool lowBeamLights = false;

    /// <summary>
    /// High beam headlights.
    /// </summary>
    public bool highBeamLights = false;

    /// <summary>
    /// Indicator lights to left side.
    /// </summary>
    public bool indicatorsLeftLights = false;

    /// <summary>
    /// Indicator lights to right side.
    /// </summary>
    public bool indicatorsRightLights = false;

    /// <summary>
    /// Indicator lights to all sides.
    /// </summary>
    public bool indicatorsAllLights = false;

    #endregion

    /// <summary>
    /// Vehicle is grounded or not?
    /// </summary>
    public bool IsGrounded {

        get {

            bool grounded = false;

            if (AxleManager != null && AxleManager.Axles.Count >= 1) {

                for (int i = 0; i < AxleManager.Axles.Count; i++) {

                    if (AxleManager.Axles[i].isGrounded)
                        grounded = true;

                }

            }

            return grounded;

        }

    }

    private void OnEnable() {

        //  Firing an event when a vehicle spawned.
        if (OtherAddonsManager != null) {

            if (OtherAddonsManager.AI == null)
                RCCP_Events.Event_OnRCCPSpawned(this);

        } else {

            RCCP_Events.Event_OnRCCPSpawned(this);

        }

        //  Listening an event when behavior changed.
        RCCP_Events.OnBehaviorChanged += CheckBehavior;

        //  Checking current behavior if selected.
        CheckBehavior();

        //  Make sure some variables are set to default values before doing anything.
        ResetVehicle();

    }

    private void Update() {

        //  Receiving player inputs from RCCP_InputManager.
        PlayerInputs();

        //  Receiving vehicle inputs taken from the components.
        VehicleInputs();

        //  Calculating drivetrain components.
        Drivetrain();

    }

    /// <summary>
    /// Finding drivetrain components.
    /// </summary>
    private void Drivetrain() {

        //  Getting important variables from the engine.
        if (Engine) {

            engineStarting = Engine.engineStarting;
            engineRunning = Engine.engineRunning;
            engineRPM = Engine.engineRPM;
            minEngineRPM = Engine.minEngineRPM;
            maxEngineRPM = Engine.maxEngineRPM;

        }

        //  Getting important variables from the gearbox.
        if (Gearbox) {

            currentGear = Gearbox.currentGear;
            currentGearRatio = Gearbox.gearRatios[currentGear];
            lastGearRatio = Gearbox.gearRatios[Gearbox.gearRatios.Length - 1];

            if (Gearbox.reverseGearEngaged)
                direction = -1;
            else
                direction = 1;

            shiftingNow = Gearbox.shiftingNow;
            reversingNow = Gearbox.reverseGearEngaged;

        }

        //  Getting important variables from the differential.
        if (Differential)
            differentialRatio = Differential.finalDriveRatio;

        //  Calculating average traction wheel rpm.
        float averagePowerWheelRPM = 0f;

        List<RCCP_Axle> poweredAxles = PoweredAxles;

        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++) {

                if (poweredAxles[i].leftWheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider.enabled)
                    averagePowerWheelRPM += Mathf.Abs(poweredAxles[i].leftWheelCollider.WheelCollider.rpm);

                if (poweredAxles[i].rightWheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider.enabled)
                    averagePowerWheelRPM += Mathf.Abs(poweredAxles[i].rightWheelCollider.WheelCollider.rpm);

            }

            if (averagePowerWheelRPM > .1f)
                averagePowerWheelRPM /= (float)Mathf.Clamp(poweredAxles.Count * 2f, 1f, 40f);

        }

        //  Calculating average traction wheel radius.
        float averagePowerWheelRadius = 0f;

        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++) {

                if (poweredAxles[i].leftWheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider.enabled)
                    averagePowerWheelRadius += poweredAxles[i].leftWheelCollider.WheelCollider.radius;

                if (poweredAxles[i].rightWheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider.enabled)
                    averagePowerWheelRadius += poweredAxles[i].rightWheelCollider.WheelCollider.radius;

            }

            if (averagePowerWheelRadius >= .1f)
                averagePowerWheelRadius /= (float)Mathf.Clamp(poweredAxles.Count * 2f, 1f, 40f);

        }

        //  Calculating speed as km/h unit.
        speed = transform.InverseTransformDirection(Rigid.velocity).z * 3.6f;

        //  Converting traction wheel rpm to engine rpm.
        tractionWheelRPM2EngineRPM = (averagePowerWheelRPM * differentialRatio * currentGearRatio) * (1f - clutchInput_V) * gearInput_V;

        //  Converting wheel rpm to speed as km/h unit.
        wheelRPM2Speed = (averagePowerWheelRPM * averagePowerWheelRadius * Mathf.PI * 2f) * 60f / 1000f;

        //  Calculating target max speed for the current gear.
        targetWheelSpeedForCurrentGear = engineRPM / currentGearRatio / differentialRatio;
        targetWheelSpeedForCurrentGear *= (averagePowerWheelRadius * Mathf.PI * 2f) * 60f / 1000f;

        //  Calculating max speed at last gear as km/h unit.
        maximumSpeed = maxEngineRPM / lastGearRatio / differentialRatio;
        maximumSpeed = (maximumSpeed * 60f) / 1000f;
        maximumSpeed *= 2 * Mathf.PI * averagePowerWheelRadius;

        //  Produced torques.
        if (Engine)
            producedEngineTorque = Engine.producedTorqueAsNM;

        if (Gearbox)
            producedGearboxTorque = Gearbox.producedTorqueAsNM;

        if (Differential)
            producedDifferentialTorque = Differential.producedTorqueAsNM;

    }

    /// <summary>
    /// Inputs of the vehicle, not player. 
    /// </summary>
    private void VehicleInputs() {

        //  Resetting all inputs to 0 before assigning them.
        fuelInput_V = 0f;
        throttleInput_V = 0f;
        brakeInput_V = 0f;
        steerInput_V = 0f;
        handbrakeInput_V = 0f;
        clutchInput_V = 0f;
        gearInput_V = 0f;
        nosInput_V = 0f;

        //  Fuel input of the engine.
        if (Engine)
            fuelInput_V = Engine.fuelInput;

        List<RCCP_Axle> poweredAxles = PoweredAxles;

        //  Throttle input.
        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++)
                throttleInput_V += poweredAxles[i].throttleInput;

            throttleInput_V /= (float)Mathf.Clamp(poweredAxles.Count, 1, 20);

        }

        List<RCCP_Axle> brakedAxles = BrakedAxles;

        //  Brake input.
        if (brakedAxles != null && brakedAxles.Count > 0) {

            for (int i = 0; i < brakedAxles.Count; i++)
                brakeInput_V += brakedAxles[i].brakeInput;

            brakeInput_V /= (float)Mathf.Clamp(brakedAxles.Count, 1, 20);

        }

        List<RCCP_Axle> steeringAxles = SteeredAxles;

        //  Steer input.
        if (steeringAxles != null && steeringAxles.Count > 0) {

            for (int i = 0; i < steeringAxles.Count; i++)
                steerInput_V += steeringAxles[i].steerInput;

            steerInput_V /= (float)Mathf.Clamp(steeringAxles.Count, 1, 20);

        }

        List<RCCP_Axle> handbrakedAxles = HandbrakedAxles;

        //  Handbrake input.
        if (handbrakedAxles != null && handbrakedAxles.Count > 0) {

            for (int i = 0; i < handbrakedAxles.Count; i++)
                handbrakeInput_V += handbrakedAxles[i].handbrakeInput;

            handbrakeInput_V /= (float)Mathf.Clamp(handbrakedAxles.Count, 1, 20);

        }

        //  Clutch input.
        if (Clutch)
            clutchInput_V = Clutch.clutchInput;

        //  Gearbox input.
        if (Gearbox)
            gearInput_V = Gearbox.gearInput;

        //  Nos input.
        if (OtherAddonsManager && OtherAddonsManager.Nos)
            nosInput_V = OtherAddonsManager.Nos.nosInUse ? 1f : 0f;

        //  Lights input.
        if (Lights) {

            lowBeamLights = Lights.lowBeamHeadlights;
            highBeamLights = Lights.highBeamHeadlights;
            indicatorsLeftLights = Lights.indicatorsLeft;
            indicatorsRightLights = Lights.indicatorsRight;
            indicatorsAllLights = Lights.indicatorsAll;

        }

        if (FrontAxle)
            steerAngle = FrontAxle.steerAngle;

    }

    /// <summary>
    /// Player inputs, not vehicle inputs.
    /// </summary>
    private void PlayerInputs() {

        //  Early out if vehicle has no input component.
        if (!Inputs)
            return;

        //  If can control is not enabled, return with 0 inputs except handbrake. Vehicle will stop. If you don't want to stop the vehicle, change handbrake input to 0.
        if (!canControl) {

            throttleInput_P = 0f;
            brakeInput_P = 0f;
            steerInput_P = 0f;
            handbrakeInput_P = 1f;
            clutchInput_P = 0f;
            nosInput_P = 0f;

            if (!Inputs.applyBrakeOnDisable)
                brakeInput_P = 0f;

            if (!Inputs.applyHandBrakeOnDisable)
                handbrakeInput_P = 0f;

            return;

        }

        //  Getting player inputs.
        throttleInput_P = Inputs.throttleInput;
        brakeInput_P = Inputs.brakeInput;
        steerInput_P = Inputs.steerInput;
        handbrakeInput_P = Inputs.handbrakeInput;
        clutchInput_P = Inputs.clutchInput;
        nosInput_P = Inputs.nosInput;

    }

    /// <summary>
    /// Sets controllable state of the vehicle.
    /// </summary>
    /// <param name="state"></param>
    public void SetCanControl(bool state) {

        canControl = state;

    }

    /// <summary>
    /// Starts the engine.
    /// </summary>
    public void StartEngine() {

        if (Engine)
            Engine.engineRunning = true;

    }

    /// <summary>
    /// Kills the engine.
    /// </summary>
    public void KillEngine() {

        if (Engine)
            Engine.engineRunning = false;

    }

    /// <summary>
    /// Sets the engine.
    /// </summary>
    /// <param name="state"></param>
    public void SetEngine(bool state) {

        if (Engine)
            Engine.engineRunning = state;

    }

    /// <summary>
    /// Firing an event on collisions, and calling methods in the damage and particle components if they are attached.
    /// </summary>
    /// <param name="collision"></param>
    public void OnCollisionEnter(Collision collision) {

        RCCP_Events.Event_OnRCCPCollision(this, collision);

        if (Damage)
            Damage.OnCollision(collision);

        if (Particles)
            Particles.OnCollision(collision);

        if (Audio)
            Audio.OnCollision(collision);

    }

    /// <summary>
    /// Firing an event on collisions, and calling methods in the particle component if attached.
    /// </summary>
    /// <param name="collision"></param>
    public void OnCollisionStay(Collision collision) {

        RCCP_Events.Event_OnRCCPCollision(this, collision);

        if (Particles)
            Particles.OnCollisionStay(collision);

    }

    /// <summary>
    /// Firing an event on collision exits, and calling methods in the particle component if attached.
    /// </summary>
    /// <param name="collision"></param>
    public void OnCollisionExit(Collision collision) {

        if (Particles)
            Particles.OnCollisionExit(collision);

    }

    /// <summary>
    /// When a wheel is deflated, call method in the audio component if attached.
    /// </summary>
    public void OnWheelDeflated() {

        if (Audio)
            Audio.DeflateWheel();

    }

    /// <summary>
    /// When a wheel is inflated, call method in the audio component if attached.
    /// </summary>
    public void OnWheelInflated() {

        if (Audio)
            Audio.InflateWheel();

    }

    /// <summary>
    /// Checks and overrides the behavior selected in the RCCP_Settings.
    /// </summary>
    private void CheckBehavior() {

        //	If override is enabled, return.
        if (ineffectiveBehavior)
            return;

        //  Early out if override behavior is disabled in the RCCP_Settings.
        if (!RCCPSettings.overrideBehavior)
            return;

        //	If selected behavior is none, return as well.
        if (RCCPSettings.SelectedBehaviorType == null)
            return;

        StartCoroutine(CheckBehaviorDelayed());

    }

    private IEnumerator CheckBehaviorDelayed() {

        yield return new WaitForFixedUpdate();

        // If any behavior is selected in RCCP_Settings, override changes.
        RCCP_Settings.BehaviorType currentBehaviorType = RCCPSettings.SelectedBehaviorType;

        //  Setting angular drag of the rigidbody.
        Rigid.angularDrag = currentBehaviorType.angularDrag;

        //  Setting stability settings if attached to the vehicle.
        if (Stability) {

            Stability.ABS = currentBehaviorType.ABS;
            Stability.ESP = currentBehaviorType.ESP;
            Stability.TCS = currentBehaviorType.TCS;

            Stability.steeringHelper = currentBehaviorType.steeringHelper;
            Stability.tractionHelper = currentBehaviorType.tractionHelper;
            Stability.angularDragHelper = currentBehaviorType.angularDragHelper;

            Stability.steerHelperStrength = Mathf.Clamp(Stability.steerHelperStrength, currentBehaviorType.steeringHelperStrengthMinimum, currentBehaviorType.steeringHelperStrengthMaximum);
            Stability.tractionHelperStrength = Mathf.Clamp(Stability.tractionHelperStrength, currentBehaviorType.tractionHelperStrengthMinimum, currentBehaviorType.tractionHelperStrengthMaximum);
            Stability.angularDragHelperStrength = Mathf.Clamp(Stability.angularDragHelperStrength, currentBehaviorType.angularDragHelperMinimum, currentBehaviorType.angularDragHelperMaximum);

        }

        //  Setting input settings if attached to the vehicle.
        if (Inputs) {

            Inputs.steeringCurve = currentBehaviorType.steeringCurve;
            Inputs.steeringLimiter = currentBehaviorType.limitSteering;
            Inputs.counterSteering = currentBehaviorType.counterSteering;
            Inputs.counterSteerFactor = Mathf.Clamp(Inputs.counterSteerFactor, currentBehaviorType.counterSteeringMinimum, currentBehaviorType.counterSteeringMaximum);

            Inputs.ResetInputs();

        }

        //  Setting axle settings if attached to the vehicle.
        if (AxleManager != null && AxleManager.Axles.Count > 1) {

            for (int i = 0; i < AxleManager.Axles.Count; i++) {

                AxleManager.Axles[i].antirollForce = Mathf.Clamp(AxleManager.Axles[i].antirollForce, currentBehaviorType.antiRollMinimum, Mathf.Infinity);
                AxleManager.Axles[i].steerSpeed = Mathf.Clamp(AxleManager.Axles[i].steerSpeed, currentBehaviorType.steeringSpeedMinimum, currentBehaviorType.steeringSpeedMaximum);

                if (AxleManager.Axles[i].leftWheelCollider) {

                    AxleManager.Axles[i].leftWheelCollider.driftMode = currentBehaviorType.driftMode;


                    if (AxleManager.Axles[i].leftWheelCollider.transform.localPosition.z > 0) {

                        AxleManager.Axles[i].leftWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_F, currentBehaviorType.forwardExtremumValue_F, currentBehaviorType.forwardAsymptoteSlip_F, currentBehaviorType.forwardAsymptoteValue_F);
                        AxleManager.Axles[i].leftWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_F, currentBehaviorType.sidewaysExtremumValue_F, currentBehaviorType.sidewaysAsymptoteSlip_F, currentBehaviorType.sidewaysAsymptoteValue_F);

                    } else {

                        AxleManager.Axles[i].leftWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_R, currentBehaviorType.forwardExtremumValue_R, currentBehaviorType.forwardAsymptoteSlip_R, currentBehaviorType.forwardAsymptoteValue_R);
                        AxleManager.Axles[i].leftWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_R, currentBehaviorType.sidewaysExtremumValue_R, currentBehaviorType.sidewaysAsymptoteSlip_R, currentBehaviorType.sidewaysAsymptoteValue_R);

                    }

                }

                if (AxleManager.Axles[i].rightWheelCollider) {

                    AxleManager.Axles[i].rightWheelCollider.driftMode = currentBehaviorType.driftMode;

                    if (AxleManager.Axles[i].rightWheelCollider.transform.localPosition.z > 0) {

                        AxleManager.Axles[i].rightWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_F, currentBehaviorType.forwardExtremumValue_F, currentBehaviorType.forwardAsymptoteSlip_F, currentBehaviorType.forwardAsymptoteValue_F);
                        AxleManager.Axles[i].rightWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_F, currentBehaviorType.sidewaysExtremumValue_F, currentBehaviorType.sidewaysAsymptoteSlip_F, currentBehaviorType.sidewaysAsymptoteValue_F);

                    } else {

                        AxleManager.Axles[i].rightWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_R, currentBehaviorType.forwardExtremumValue_R, currentBehaviorType.forwardAsymptoteSlip_R, currentBehaviorType.forwardAsymptoteValue_R);
                        AxleManager.Axles[i].rightWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_R, currentBehaviorType.sidewaysExtremumValue_R, currentBehaviorType.sidewaysAsymptoteSlip_R, currentBehaviorType.sidewaysAsymptoteValue_R);

                    }

                }

            }

        }

        //  Setting gearbox settings if attached to the vehicle.
        if (Gearbox) {

            Gearbox.shiftThreshold = currentBehaviorType.gearShiftingThreshold;
            Gearbox.shiftingTime = Mathf.Clamp(Gearbox.shiftingTime, currentBehaviorType.gearShiftingDelayMinimum, currentBehaviorType.gearShiftingDelayMaximum);

        }

        //  Setting differential settings if attached to the vehicle.
        if (Differential)
            Differential.differentialType = currentBehaviorType.differentialType;

    }

    private void OnDisable() {

        //  Firing an event when disabing / destroying the vehicle.
        if (OtherAddonsManager != null) {

            if (OtherAddonsManager.AI == null)
                RCCP_Events.Event_OnRCCPDestroyed(this);

        } else {

            RCCP_Events.Event_OnRCCPDestroyed(this);

        }
;
        RCCP_Events.OnBehaviorChanged -= CheckBehavior;

    }

    private void Reset() {

        //Rigidbody rigidbody = GetComponent<Rigidbody>();
        //rigidbody.mass = 1350f;
        //rigidbody.drag = .01f;
        //rigidbody.angularDrag = .25f;
        //rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        //rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

    }

}
