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
/// Input receiver from the RCCP_InputManager.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Input")]
public class RCCP_Input : RCCP_Component {

    public RCCP_InputManager RCCPInputManager {

        get {

            if (_RCCPInputManager == null)
                _RCCPInputManager = RCCP_InputManager.Instance;

            return _RCCPInputManager;

        }

    }
    private RCCP_InputManager _RCCPInputManager;

    /// <summary>
    /// Overrides the internal inputs such as throttle, brake, steer, handbrake, nos inputs, etc...
    /// </summary>
    public bool overridePlayerInputs = false;

    /// <summary>
    /// Overrides the external inputs such as counter steer, reverse gear, etc...
    /// </summary>
    public bool overrideExternalInputs = false;

    [System.Obsolete("Use 'overridePlayerInputs' instead of this.")]
    public bool overrideInternalInputs {

        get {

            return overridePlayerInputs;

        }
        set {

            overridePlayerInputs = value;

        }

    }

    /// <summary>
    /// Inputs.
    /// </summary>
    public RCCP_Inputs inputs = new RCCP_Inputs();

    /// <summary>
    /// Internal inputs.
    /// </summary>
    [Range(0f, 1f)] public float throttleInput = 0f;

    /// <summary>
    /// Internal inputs.
    /// </summary>
    [Range(-1f, 1f)] public float steerInput = 0f;

    /// <summary>
    /// Internal inputs.
    /// </summary>
    [Range(0f, 1f)] public float brakeInput = 0f;

    /// <summary>
    /// Internal inputs.
    /// </summary>
    [Range(0f, 1f)] public float handbrakeInput = 0f;

    /// <summary>
    /// Internal inputs.
    /// </summary>
    [Range(0f, 1f)] public float clutchInput = 0f;

    /// <summary>
    /// Internal inputs.
    /// </summary>
    [Range(0f, 1f)] public float nosInput = 0f;

    /// <summary>
    /// Steering Curve. Reduces maximum steering angle on higher speeds.
    /// </summary>
    public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(100f, .2f), new Keyframe(200f, .15f));

    /// <summary>
    /// Steering limiter. Limits the maximum steering angle if vehicle is skidding.
    /// </summary>
    public bool steeringLimiter = true;

    /// <summary>
    /// Applies counter steering based on steering wheels sideways friction.
    /// </summary>
    public bool counterSteering = true;

    /// <summary>
    /// Counter steering multiplier.
    /// </summary>
    [Range(0f, 1f)] public float counterSteerFactor = .5f;

    /// <summary>
    /// Current steering input.
    /// </summary>
    private float steerInputCounter = 0f;

    /// <summary>
    /// Gearbox will be called for reverse gear if player applies the brake input while vehicle is stopped.
    /// </summary>
    public bool autoReverse = true;

    /// <summary>
    /// Inverse throttle / brake inputs on reverse gear. 
    /// </summary>
    public bool inverseThrottleBrakeOnReverse = true;

    /// <summary>
    /// Cuts throttle while shifting.
    /// </summary>
    public bool cutThrottleWhenShifting = true;

    /// <summary>
    /// Applies brake torque if vehicle is not controllable.
    /// </summary>
    public bool applyBrakeOnDisable = true;

    /// <summary>
    /// Applies handbrake torque if vehicle is not controllable.
    /// </summary>
    public bool applyHandBrakeOnDisable = true;

    private bool oldCanControl, oldExternalControl;

    public override void OnEnable() {

        base.OnEnable();

        oldCanControl = CarController.canControl;
        oldExternalControl = CarController.externalControl;

        //  Listening events for inputs.
        RCCP_InputManager.OnStartEngine += RCCP_InputManager_OnStartEngine;
        RCCP_InputManager.OnStopEngine += RCCP_InputManager_OnStopEngine;
        RCCP_InputManager.OnSteeringHelper += RCCP_InputManager_OnSteeringHelper;
        RCCP_InputManager.OnTractionHelper += RCCP_InputManager_OnTractionHelper;
        RCCP_InputManager.OnAngularDragHelper += RCCP_InputManager_OnAngularDragHelper;
        RCCP_InputManager.OnABS += RCCP_InputManager_OnABS;
        RCCP_InputManager.OnESP += RCCP_InputManager_OnESP;
        RCCP_InputManager.OnTCS += RCCP_InputManager_OnTCS;
        RCCP_InputManager.OnGearShiftedUp += RCCP_InputManager_OnGearShiftedUp;
        RCCP_InputManager.OnGearShiftedDown += RCCP_InputManager_OnGearShiftedDown;
        RCCP_InputManager.OnGearShiftedTo += RCCP_InputManager_OnGearShiftedTo;
        RCCP_InputManager.OnGearToggle += RCCP_InputManager_OnGearToggle;
        RCCP_InputManager.OnAutomaticGear += RCCP_InputManager_OnAutomaticGearChanged;
        RCCP_InputManager.OnPressedLowBeamLights += RCCP_InputManager_OnPressedLowBeamLights;
        RCCP_InputManager.OnPressedHighBeamLights += RCCP_InputManager_OnPressedHighBeamLights;
        RCCP_InputManager.OnPressedLeftIndicatorLights += RCCP_InputManager_OnPressedLeftIndicatorLights;
        RCCP_InputManager.OnPressedRightIndicatorLights += RCCP_InputManager_OnPressedRightIndicatorLights;
        RCCP_InputManager.OnPressedIndicatorLights += RCCP_InputManager_OnPressedIndicatorLights;
        RCCP_InputManager.OnTrailerDetach += RCCP_InputManager_OnTrailerDetach;

    }

    private void Update() {

        if (CarController.canControl != oldCanControl || CarController.externalControl != oldExternalControl)
            inputs = new RCCP_Inputs();

        oldCanControl = CarController.canControl;
        oldExternalControl = CarController.externalControl;

        //  Get internal inputs from the RCCP_InputManager.
        if (!overridePlayerInputs)
            PlayerInputs();

        if (inputs != null) {

            throttleInput = inputs.throttleInput;
            steerInput = inputs.steerInput;
            brakeInput = inputs.brakeInput;
            clutchInput = inputs.clutchInput;
            handbrakeInput = inputs.handbrakeInput;
            nosInput = inputs.nosInput;

            //  Clamping inputs.
            throttleInput = Mathf.Clamp01(throttleInput);
            brakeInput = Mathf.Clamp01(brakeInput);
            steerInput = Mathf.Clamp(steerInput, -1f, 1f);
            clutchInput = Mathf.Clamp01(clutchInput);
            handbrakeInput = Mathf.Clamp01(handbrakeInput);
            nosInput = Mathf.Clamp01(nosInput);

        }

        if (!overrideExternalInputs)
            VehicleControlledInputs();

    }

    /// <summary>
    /// Overrides inputs with given inputs.
    /// </summary>
    /// <param name="overridedInputs"></param>
    public void OverrideInputs(RCCP_Inputs overridedInputs) {

        overridePlayerInputs = true;
        inputs = overridedInputs;

    }

    /// <summary>
    /// Disables overriding inputs mode.
    /// </summary>
    public void DisableOverrideInputs() {

        overridePlayerInputs = false;

    }

    /// <summary>
    /// Internal inputs mainly focused on direct inputs.
    /// </summary>
    private void PlayerInputs() {

        if (CarController.canControl && !CarController.externalControl)
            inputs = RCCP_InputManager.Instance.GetInputs();

    }

    /// <summary>
    /// External inputs mainly focused on processing additional inputs.
    /// </summary>
    private void VehicleControlledInputs() {

        //  If vehicle has a gearbox...
        if (CarController.Gearbox) {

            if (CarController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic && autoReverse) {

                //  If speed of the vehicle is below 1, and brake input is still high, put it to reverse gear.
                if (CarController.speed <= 1f && inputs.brakeInput >= .75f) {

                    if (!CarController.reversingNow)
                        CarController.Gearbox.ShiftReverse();

                } else {

                    //  If speeed of the vehicle is above -1 and still at reverse gear, put it to first gear.
                    if (CarController.speed >= -1 && CarController.reversingNow)
                        CarController.Gearbox.ShiftToGear(0);

                }

            }

        }

        //  Cuts throttle input when shifting.
        if (cutThrottleWhenShifting && CarController.shiftingNow)
            throttleInput = 0;

        bool canInverseInputs = inverseThrottleBrakeOnReverse && CarController.reversingNow;

        if (CarController.Gearbox && CarController.Gearbox.transmissionType != RCCP_Gearbox.TransmissionType.Automatic)
            canInverseInputs = false;

        //  Inverse throttle and brake inputs on reverse gear.
        if (canInverseInputs) {

            throttleInput = inputs.brakeInput;
            brakeInput = inputs.throttleInput;

        }

        //  If counter steering is enabled, get sideways slip of the steering wheels and apply it as steer input counter. 
        if (counterSteering) {

            float sidewaysSlip = 0f;

            if (CarController.FrontAxle)
                sidewaysSlip = (CarController.FrontAxle.leftWheelCollider.wheelSlipAmountSideways + CarController.FrontAxle.rightWheelCollider.wheelSlipAmountSideways) / 2f;

            steerInputCounter = (sidewaysSlip * counterSteerFactor);
            steerInputCounter = Mathf.Clamp(steerInputCounter, -1f, 1f);

            steerInput += steerInputCounter * (1f - Mathf.Abs(steerInput));

        }

        // Steering limiter. Limits the maximum steering angle if vehicle is skidding.
        if (steeringLimiter) {

            //  If speed of the vehicle is below 5, return.
            if (Mathf.Abs(CarController.speed) < 5f)
                return;

            float sidewaysSlip = 0f;        //	Total sideways slip of all wheels.

            //  Getting all sideways slips average.
            foreach (RCCP_WheelCollider w in CarController.AllWheelColliders)
                sidewaysSlip += w.wheelSlipAmountSideways;

            sidewaysSlip /= CarController.AllWheelColliders.Length;

            float maxSteerInput = Mathf.Clamp(1f - Mathf.Abs(sidewaysSlip), -1f, 1f);      //	Subtract total average sideways slip from max steer input (1f).;
            float sign = -Mathf.Sign(sidewaysSlip);      //	Is sideways slip is left or right?

            //	If slip is high enough, apply counter input.
            if (maxSteerInput > 0f)
                steerInput = Mathf.Clamp(steerInput, -maxSteerInput, maxSteerInput);
            else
                steerInput = Mathf.Clamp(steerInput, sign * maxSteerInput, sign * maxSteerInput);

        }

        //  Steering curve based on speed. Reduces the maximum steering angle on higher speeds.
        if (steeringCurve != null)
            steerInput *= steeringCurve.Evaluate(Mathf.Abs(CarController.speed));

    }

    /// <summary>
    /// When pressed indicator all lights button.
    /// </summary>
    private void RCCP_InputManager_OnPressedIndicatorLights() {

        //  If no lights component found, return.
        if (!CarController.Lights)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting lights.
        CarController.Lights.indicatorsAll = !CarController.Lights.indicatorsAll;
        CarController.Lights.indicatorsLeft = false;
        CarController.Lights.indicatorsRight = false;

        //  Informing.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched All Indicators To " + CarController.Lights.indicatorsAll);

    }

    /// <summary>
    /// When pressed indicators right button.
    /// </summary>
    private void RCCP_InputManager_OnPressedRightIndicatorLights() {

        //  If no lights found, return.
        if (!CarController.Lights)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting lights.
        CarController.Lights.indicatorsRight = !CarController.Lights.indicatorsRight;
        CarController.Lights.indicatorsLeft = false;
        CarController.Lights.indicatorsAll = false;

        //  Informing.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Right Indicators To " + CarController.Lights.indicatorsRight);

    }

    /// <summary>
    /// When pressed indicators left button.
    /// </summary>
    private void RCCP_InputManager_OnPressedLeftIndicatorLights() {

        //  If no lights found, return.
        if (!CarController.Lights)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting lights.
        CarController.Lights.indicatorsLeft = !CarController.Lights.indicatorsLeft;
        CarController.Lights.indicatorsRight = false;
        CarController.Lights.indicatorsAll = false;

        //  Informing.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Left Indicators To " + CarController.Lights.indicatorsLeft);

    }

    /// <summary>
    /// When pressed high beam lights button.
    /// </summary>
    private void RCCP_InputManager_OnPressedHighBeamLights() {

        //  If no lights found, return.
        if (!CarController.Lights)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting lights.
        CarController.Lights.highBeamHeadlights = !CarController.Lights.highBeamHeadlights;

        //  Informing.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched High Beam Lights To " + CarController.Lights.highBeamHeadlights);

    }

    /// <summary>
    /// When pressed low beam lights button.
    /// </summary>
    private void RCCP_InputManager_OnPressedLowBeamLights() {

        //  If no lights found, return.
        if (!CarController.Lights)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting lights.
        CarController.Lights.lowBeamHeadlights = !CarController.Lights.lowBeamHeadlights;

        //  Informing.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Low Beam Lights To " + CarController.Lights.lowBeamHeadlights);

    }

    /// <summary>
    /// When pressed steering helper button.
    /// </summary>
    private void RCCP_InputManager_OnSteeringHelper() {

        //  If no stability found, return.
        if (!CarController.Stability)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting.
        CarController.Stability.steeringHelper = !CarController.Stability.steeringHelper;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Steering Helper To " + CarController.Stability.steeringHelper);

    }

    /// <summary>
    /// When pressed to traction helper button.
    /// </summary>
    private void RCCP_InputManager_OnTractionHelper() {

        //  If no stability found, return.
        if (!CarController.Stability)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting.
        CarController.Stability.tractionHelper = !CarController.Stability.tractionHelper;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Traction Helper To " + CarController.Stability.tractionHelper);

    }

    /// <summary>
    /// When pressed to angular drag helper button.
    /// </summary>
    private void RCCP_InputManager_OnAngularDragHelper() {

        //  If no stability found, return.
        if (!CarController.Stability)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting.
        CarController.Stability.angularDragHelper = !CarController.Stability.angularDragHelper;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched Angular Drag Helper To " + CarController.Stability.angularDragHelper);

    }

    /// <summary>
    /// When pressed abs button.
    /// </summary>
    private void RCCP_InputManager_OnABS() {

        //  If no stability found, return.
        if (!CarController.Stability)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting.
        CarController.Stability.ABS = !CarController.Stability.ABS;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched ABS To " + CarController.Stability.ABS);

    }

    /// <summary>
    /// When pressed esp button.
    /// </summary>
    private void RCCP_InputManager_OnESP() {

        //  If no stability found, return.
        if (!CarController.Stability)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting.
        CarController.Stability.ESP = !CarController.Stability.ESP;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched ESP To " + CarController.Stability.ESP);

    }

    /// <summary>
    /// When pressed tcs button.
    /// </summary>
    private void RCCP_InputManager_OnTCS() {

        //  If no stability found, return.
        if (!CarController.Stability)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Setting.
        CarController.Stability.TCS = !CarController.Stability.TCS;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Switched TCS To " + CarController.Stability.TCS);

    }

    /// <summary>
    /// When pressed stop engine button.
    /// </summary>
    private void RCCP_InputManager_OnStopEngine() {

        //  If no car controller found, return.
        if (!CarController.Engine)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Killing the engine.
        CarController.Engine.StopEngine();

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Stopped Engine");

    }

    /// <summary>
    /// When pressed start engine button.
    /// </summary>
    private void RCCP_InputManager_OnStartEngine() {

        //  If no car controller found, return.
        if (!CarController.Engine)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer(!CarController.Engine.engineRunning ? "Starting Engine" : "Killing Engine");

        //  Starting the engine.
        if (!CarController.Engine.engineRunning)
            CarController.Engine.StartEngine();
        else
            CarController.Engine.StopEngine();

    }

    /// <summary>
    /// When pressed gear shift down button.
    /// </summary>
    private void RCCP_InputManager_OnGearShiftedDown() {

        //  If no gearbox found, return.
        if (!CarController.Gearbox)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Shifting down.
        CarController.Gearbox.ShiftDown();

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted Down");

    }

    /// <summary>
    /// When pressed gear shift to button.
    /// </summary>
    private void RCCP_InputManager_OnGearShiftedTo(int gear) {

        //  If no gearbox found, return.
        if (!CarController.Gearbox)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Shifting down.
        CarController.Gearbox.ShiftToGear(gear);

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted To: " + gear.ToString());

    }

    /// <summary>
    /// When pressed gear shift down button.
    /// </summary>
    private void RCCP_InputManager_OnGearShiftedUp() {

        //  If no gearbox found, return.
        if (!CarController.Gearbox)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Shifting up.
        CarController.Gearbox.ShiftUp();

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Shifted Up");

    }

    /// <summary>
    /// When pressed gear toggle button.
    /// </summary>
    private void RCCP_InputManager_OnGearToggle(RCCP_Gearbox.TransmissionType transmissionType) {

        //  If no gearbox found, return.
        if (!CarController.Gearbox)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Shifting down.
        CarController.Gearbox.transmissionType = transmissionType;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Automatic Gearbox = " + CarController.Gearbox.transmissionType.ToString());

    }

    /// <summary>
    /// When automatic gear changed.
    /// </summary>
    private void RCCP_InputManager_OnAutomaticGearChanged(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear) {

        //  If no gearbox found, return.
        if (!CarController.Gearbox)
            return;

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  Shifting down.
        CarController.Gearbox.transmissionType = RCCP_Gearbox.TransmissionType.Automatic_DNRP;
        CarController.Gearbox.automaticGearSelector = semiAutomaticDNRPGear;

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Automatic Gearbox = " + CarController.Gearbox.automaticGearSelector.ToString());

    }

    /// <summary>
    /// When pressed trailer detach button.
    /// </summary>
    private void RCCP_InputManager_OnTrailerDetach() {

        //  Return if canControl is disabled.
        if (!CarController.canControl || CarController.externalControl)
            return;

        //  If no other addons found, return.
        if (!CarController.OtherAddonsManager)
            return;

        //  If no trailer attacher found, return.
        if (!CarController.OtherAddonsManager.TrailAttacher)
            return;

        //  Detaching the railer.
        CarController.OtherAddonsManager.TrailAttacher.attachedTrailer.DetachTrailer();

        //  Informer.
        if (RCCPSettings.useInputDebugger)
            RCCP_Events.Event_OnRCCPUIInformer("Trailer Detached");

    }

    public override void OnDisable() {

        base.OnDisable();

        RCCP_InputManager.OnStartEngine -= RCCP_InputManager_OnStartEngine;
        RCCP_InputManager.OnStopEngine -= RCCP_InputManager_OnStopEngine;
        RCCP_InputManager.OnSteeringHelper -= RCCP_InputManager_OnSteeringHelper;
        RCCP_InputManager.OnTractionHelper -= RCCP_InputManager_OnTractionHelper;
        RCCP_InputManager.OnAngularDragHelper -= RCCP_InputManager_OnAngularDragHelper;
        RCCP_InputManager.OnABS -= RCCP_InputManager_OnABS;
        RCCP_InputManager.OnESP -= RCCP_InputManager_OnESP;
        RCCP_InputManager.OnTCS -= RCCP_InputManager_OnTCS;
        RCCP_InputManager.OnGearShiftedUp -= RCCP_InputManager_OnGearShiftedUp;
        RCCP_InputManager.OnGearShiftedDown -= RCCP_InputManager_OnGearShiftedDown;
        RCCP_InputManager.OnGearShiftedTo -= RCCP_InputManager_OnGearShiftedTo;
        RCCP_InputManager.OnGearToggle -= RCCP_InputManager_OnGearToggle;
        RCCP_InputManager.OnAutomaticGear -= RCCP_InputManager_OnAutomaticGearChanged;
        RCCP_InputManager.OnPressedLowBeamLights -= RCCP_InputManager_OnPressedLowBeamLights;
        RCCP_InputManager.OnPressedHighBeamLights -= RCCP_InputManager_OnPressedHighBeamLights;
        RCCP_InputManager.OnPressedLeftIndicatorLights -= RCCP_InputManager_OnPressedLeftIndicatorLights;
        RCCP_InputManager.OnPressedRightIndicatorLights -= RCCP_InputManager_OnPressedRightIndicatorLights;
        RCCP_InputManager.OnPressedIndicatorLights -= RCCP_InputManager_OnPressedIndicatorLights;
        RCCP_InputManager.OnTrailerDetach -= RCCP_InputManager_OnTrailerDetach;

    }

    /// <summary>
    /// Resets all inputs to 0.
    /// </summary>
    public void ResetInputs() {

        inputs = new RCCP_Inputs();

        throttleInput = 0f;
        steerInput = 0f;
        brakeInput = 0f;
        handbrakeInput = 0f;
        clutchInput = 0f;
        nosInput = 0f;
        steerInputCounter = 0f;

    }

    private void Reset() {

        Keyframe[] ks = new Keyframe[3];

        ks[0] = new Keyframe(0f, 1f);
        ks[0].outTangent = -.0135f;    // -5 units on the y axis for 1 unit on the x axis.

        ks[1] = new Keyframe(100f, .2f);
        ks[1].inTangent = -.0015f;    // straight
        ks[1].outTangent = -.001f;    // straight

        ks[2] = new Keyframe(200f, .15f);

        steeringCurve = new AnimationCurve(ks);

    }

}
