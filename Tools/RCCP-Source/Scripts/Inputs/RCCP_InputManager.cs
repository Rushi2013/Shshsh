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

#if BCG_NEWINPUTSYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Main input manager of the RCCP. Receives inputs from the corresponding device and let the other components use them.
/// </summary>
public class RCCP_InputManager : RCCP_Singleton<RCCP_InputManager> {

    public RCCP_Inputs inputs = new RCCP_Inputs();

#if BCG_NEWINPUTSYSTEM
    public InputActionAsset inputActionsInstance = null;
#endif

    public bool overrideInputs = false;

    public delegate void onGearShiftedUp();
    public static event onGearShiftedUp OnGearShiftedUp;

    public delegate void onGearShiftedDown();
    public static event onGearShiftedDown OnGearShiftedDown;

    public delegate void onGearToggle(RCCP_Gearbox.TransmissionType transmissionType);
    public static event onGearToggle OnGearToggle;

    public delegate void onAutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear);
    public static event onAutomaticGear OnAutomaticGear;

    public delegate void onGearShiftedTo(int gearIndex);
    public static event onGearShiftedTo OnGearShiftedTo;

    public delegate void onChangedCamera();
    public static event onChangedCamera OnChangedCamera;

    public delegate void onLookBackCamera(bool state);
    public static event onLookBackCamera OnLookBackCamera;

    public delegate void onHoldOrbitCamera(bool state);
    public static event onHoldOrbitCamera OnHoldOrbitCamera;

    public delegate void onPressedLowBeamLights();
    public static event onPressedLowBeamLights OnPressedLowBeamLights;

    public delegate void onPressedHighBeamLights();
    public static event onPressedHighBeamLights OnPressedHighBeamLights;

    public delegate void onPressedLeftIndicatorLights();
    public static event onPressedLeftIndicatorLights OnPressedLeftIndicatorLights;

    public delegate void onPressedRightIndicatorLights();
    public static event onPressedRightIndicatorLights OnPressedRightIndicatorLights;

    public delegate void onPressedIndicatorLights();
    public static event onPressedIndicatorLights OnPressedIndicatorLights;

    public delegate void onStartEngine();
    public static event onStartEngine OnStartEngine;

    public delegate void onStopEngine();
    public static event onStopEngine OnStopEngine;

    public delegate void onSteeringHelper();
    public static event onSteeringHelper OnSteeringHelper;

    public delegate void onTractionHelper();
    public static event onTractionHelper OnTractionHelper;

    public delegate void onAngularDragHelper();
    public static event onAngularDragHelper OnAngularDragHelper;

    public delegate void onABS();
    public static event onABS OnABS;

    public delegate void onESP();
    public static event onESP OnESP;

    public delegate void onTCS();
    public static event onTCS OnTCS;

    public delegate void onRecord();
    public static event onRecord OnRecord;

    public delegate void onReplay();
    public static event onReplay OnReplay;

    public delegate void onTrailerDetach();
    public static event onTrailerDetach OnTrailerDetach;

    public delegate void onOptions();
    public static event onOptions OnOptions;

    private void Awake() {

        DontDestroyOnLoad(gameObject);

        //  Creating inputs.
        inputs = new RCCP_Inputs();

    }

    private void Update() {

        //  Creating inputs.
        if (inputs == null)
            inputs = new RCCP_Inputs();

        //  Receive inputs from the controller.
        if (!overrideInputs) {

            if (!RCCPSettings.mobileControllerEnabled)
                inputs = KeyboardInputs();
            else
                inputs = MobileInputs();

        }

    }

    public void OverrideInputs(RCCP_Inputs overriddenInputs) {

        overrideInputs = true;
        inputs = overriddenInputs;

    }

    public void DisableOverrideInputs() {

        overrideInputs = false;

    }

    /// <summary>
    /// Returns player inputs.
    /// </summary>
    /// <returns></returns>
    public RCCP_Inputs GetInputs() {

        return inputs;

    }

    /// <summary>
    /// Keyboard inputs with old and new input system.
    /// </summary>
    /// <returns></returns>
    private RCCP_Inputs KeyboardInputs() {

        //  If new input system in use, listen events in the input actions script.
        if (RCCPSettings.useNewInputSystem) {

#if BCG_NEWINPUTSYSTEM

            if (inputActionsInstance == null) {

                inputActionsInstance = RCCP_InputActions.Instance.inputActions;
                inputActionsInstance.Enable();

                inputActionsInstance.actionMaps[0].actions[4].performed += StartEngine;
                inputActionsInstance.actionMaps[0].actions[5].performed += LowBeamHeadlights;
                inputActionsInstance.actionMaps[0].actions[6].performed += HighBeamHeadlights;
                inputActionsInstance.actionMaps[0].actions[7].performed += IndicatorRightlights;
                inputActionsInstance.actionMaps[0].actions[8].performed += IndicatorLeftlights;
                inputActionsInstance.actionMaps[0].actions[9].performed += Indicatorlights;
                inputActionsInstance.actionMaps[0].actions[10].performed += GearShiftUp;
                inputActionsInstance.actionMaps[0].actions[11].performed += GearShiftDown;
                inputActionsInstance.actionMaps[0].actions[13].performed += TrailDetach;

                inputActionsInstance.actionMaps[0].actions[15].performed += _1stGear_performed;
                inputActionsInstance.actionMaps[0].actions[16].performed += _2ndGear_performed;
                inputActionsInstance.actionMaps[0].actions[17].performed += _3rdGear_performed;
                inputActionsInstance.actionMaps[0].actions[18].performed += _4thGear_performed;
                inputActionsInstance.actionMaps[0].actions[19].performed += _5thGear_performed;
                inputActionsInstance.actionMaps[0].actions[20].performed += _6thGear_performed;
                inputActionsInstance.actionMaps[0].actions[21].performed += _RGear_performed;

                //inputActionsInstance.actionMaps[0].actions[15].canceled += _Gear_canceled;
                //inputActionsInstance.actionMaps[0].actions[16].canceled += _Gear_canceled;
                //inputActionsInstance.actionMaps[0].actions[17].canceled += _Gear_canceled;
                //inputActionsInstance.actionMaps[0].actions[18].canceled += _Gear_canceled;
                //inputActionsInstance.actionMaps[0].actions[19].canceled += _Gear_canceled;
                //inputActionsInstance.actionMaps[0].actions[20].canceled += _Gear_canceled;

                inputActionsInstance.actionMaps[1].actions[1].performed += ChangeCamera;
                inputActionsInstance.actionMaps[1].actions[2].performed += LookBackCameraPerformed;
                inputActionsInstance.actionMaps[1].actions[2].canceled += LookBackCameraCanceled;
                inputActionsInstance.actionMaps[1].actions[4].performed += HoldOrbitCameraPerformed;
                inputActionsInstance.actionMaps[1].actions[4].canceled += HoldOrbitCameraCanceled;

                inputActionsInstance.actionMaps[2].actions[0].performed += Record;
                inputActionsInstance.actionMaps[2].actions[1].performed += Replay;

            }

            inputs.throttleInput = inputActionsInstance.actionMaps[0].actions[0].ReadValue<float>();
            inputs.brakeInput = inputActionsInstance.actionMaps[0].actions[1].ReadValue<float>();
            inputs.steerInput = inputActionsInstance.actionMaps[0].actions[2].ReadValue<float>();
            inputs.handbrakeInput = inputActionsInstance.actionMaps[0].actions[3].ReadValue<float>();
            inputs.nosInput = inputActionsInstance.actionMaps[0].actions[12].ReadValue<float>();
            inputs.clutchInput = inputActionsInstance.actionMaps[0].actions[22].ReadValue<float>();
            inputs.mouseInput = inputActionsInstance.actionMaps[1].actions[0].ReadValue<Vector2>();

#endif

        } else {

            //  Receiving player inputs with the old legacy input system.
            inputs.throttleInput = Mathf.Clamp01(Input.GetAxis("RCCP_Throttle"));
            inputs.brakeInput = Mathf.Clamp01(-Input.GetAxis("RCCP_Brake"));
            inputs.steerInput = Mathf.Clamp(Input.GetAxis("RCCP_Steering"), -1f, 1f);
            inputs.clutchInput = Mathf.Clamp(Input.GetAxis("RCCP_Clutch"), 0f, 1f);
            inputs.handbrakeInput = Mathf.Clamp(Input.GetAxis("RCCP_Handbrake"), 0f, 1f);
            inputs.nosInput = Mathf.Clamp(Input.GetAxis("RCCP_Nos"), 0f, 1f);
            inputs.mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            if (Input.GetKeyDown(KeyCode.I))
                StartEngine();

            if (Input.GetKeyDown(KeyCode.O))
                StopEngine();

            if (Input.GetKeyDown(KeyCode.LeftShift))
                GearShiftUp();

            if (Input.GetKeyDown(KeyCode.LeftControl))
                GearShiftDown();

            if (Input.GetKeyDown(KeyCode.C))
                ChangeCamera();

            if (Input.GetKeyDown(KeyCode.L))
                LowBeamHeadlights();

            if (Input.GetKeyDown(KeyCode.K))
                HighBeamHeadlights();

            if (Input.GetKeyDown(KeyCode.Q))
                IndicatorLeftlights();

            if (Input.GetKeyDown(KeyCode.E))
                IndicatorRightlights();

            if (Input.GetKeyDown(KeyCode.Z))
                Indicatorlights();

            if (Input.GetKey(KeyCode.B))
                LookBackCamera(true);
            else
                LookBackCamera(false);

            if (Input.GetKeyDown(KeyCode.R))
                Record();

            if (Input.GetKeyDown(KeyCode.P))
                Replay();

            if (Input.GetKeyDown(KeyCode.T))
                TrailDetach();

        }

        return inputs;

    }

    /// <summary>
    /// Receiving mobile player inputs from the RCCP_MobileInputs script attached to the RCCP_Canvas.
    /// </summary>
    /// <returns></returns>
    private RCCP_Inputs MobileInputs() {

        //  Getting instance of the mobile controller.
        RCCP_MobileInputs mobileInputs = RCCP_MobileInputs.Instance;

        //  If no mobile inputs found, return.
        if (mobileInputs) {

            //  Receiving inputs.
            inputs.throttleInput = mobileInputs.throttleInput;
            inputs.brakeInput = mobileInputs.brakeInput;
            inputs.steerInput = mobileInputs.steerInput;
            inputs.handbrakeInput = mobileInputs.ebrakeInput;
            inputs.nosInput = mobileInputs.nosInput;

        }

        return inputs;

    }

    public void GearShiftUp() {

        if (OnGearShiftedUp != null)
            OnGearShiftedUp();

    }

    public void GearShiftDown() {

        if (OnGearShiftedDown != null)
            OnGearShiftedDown();

    }

    public void ToggleGear(RCCP_Gearbox.TransmissionType transmissionType) {

        if (OnGearToggle != null)
            OnGearToggle(transmissionType);

    }

    public void AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear semiAutomaticDNRPGear) {

        if (OnAutomaticGear != null)
            OnAutomaticGear(semiAutomaticDNRPGear);

    }

    public void ChangeCamera() {

        if (OnChangedCamera != null)
            OnChangedCamera();

    }

    public void LowBeamHeadlights() {

        if (OnPressedLowBeamLights != null)
            OnPressedLowBeamLights();

    }

    public void HighBeamHeadlights() {

        if (OnPressedHighBeamLights != null)
            OnPressedHighBeamLights();

    }

    public void IndicatorLeftlights() {

        if (OnPressedLeftIndicatorLights != null)
            OnPressedLeftIndicatorLights();

    }

    public void IndicatorRightlights() {

        if (OnPressedRightIndicatorLights != null)
            OnPressedRightIndicatorLights();

    }

    public void Indicatorlights() {

        if (OnPressedIndicatorLights != null)
            OnPressedIndicatorLights();

    }

    public void LookBackCamera(bool state) {

        if (OnLookBackCamera != null)
            OnLookBackCamera(state);

    }

    public void HoldOrbitCamera(bool state) {

        if (OnHoldOrbitCamera != null)
            OnHoldOrbitCamera(state);

    }

    public void StartEngine() {

        if (OnStartEngine != null)
            OnStartEngine();

    }

    public void StopEngine() {

        if (OnStopEngine != null)
            OnStopEngine();

    }

    public void SteeringHelper() {

        if (OnSteeringHelper != null)
            OnSteeringHelper();

    }

    public void TractionHelper() {

        if (OnTractionHelper != null)
            OnTractionHelper();

    }

    public void AngularDragHelper() {

        if (OnAngularDragHelper != null)
            OnAngularDragHelper();

    }

    public void ABS() {

        if (OnABS != null)
            OnABS();

    }

    public void ESP() {

        if (OnESP != null)
            OnESP();

    }

    public void TCS() {

        if (OnTCS != null)
            OnTCS();

    }

    public void Record() {

        if (OnRecord != null)
            OnRecord();

    }

    public void Replay() {

        if (OnReplay != null)
            OnReplay();

    }

    public void TrailDetach() {

        if (OnTrailerDetach != null)
            OnTrailerDetach();

    }

    public void Options() {

        if (OnOptions != null)
            OnOptions();

    }

#if BCG_NEWINPUTSYSTEM

    public void GearShiftUp(InputAction.CallbackContext obj) {

        if (OnGearShiftedUp != null)
            OnGearShiftedUp();

    }

    public void GearShiftDown(InputAction.CallbackContext obj) {

        if (OnGearShiftedDown != null)
            OnGearShiftedDown();

    }

    public void ChangeCamera(InputAction.CallbackContext obj) {

        if (OnChangedCamera != null)
            OnChangedCamera();

    }

    public void LowBeamHeadlights(InputAction.CallbackContext obj) {

        if (OnPressedLowBeamLights != null)
            OnPressedLowBeamLights();

    }

    public void HighBeamHeadlights(InputAction.CallbackContext obj) {

        if (OnPressedHighBeamLights != null)
            OnPressedHighBeamLights();

    }

    public void IndicatorLeftlights(InputAction.CallbackContext obj) {

        if (OnPressedLeftIndicatorLights != null)
            OnPressedLeftIndicatorLights();

    }

    public void IndicatorRightlights(InputAction.CallbackContext obj) {

        if (OnPressedRightIndicatorLights != null)
            OnPressedRightIndicatorLights();

    }

    public void Indicatorlights(InputAction.CallbackContext obj) {

        if (OnPressedIndicatorLights != null)
            OnPressedIndicatorLights();

    }

    public void LookBackCameraPerformed(InputAction.CallbackContext obj) {

        if (OnLookBackCamera != null)
            OnLookBackCamera(true);

    }

    public void LookBackCameraCanceled(InputAction.CallbackContext obj) {

        if (OnLookBackCamera != null)
            OnLookBackCamera(false);

    }

    public void HoldOrbitCameraPerformed(InputAction.CallbackContext obj) {

        if (OnHoldOrbitCamera != null)
            OnHoldOrbitCamera(true);

    }

    public void HoldOrbitCameraCanceled(InputAction.CallbackContext obj) {

        if (OnHoldOrbitCamera != null)
            OnHoldOrbitCamera(false);

    }

    public void StartEngine(InputAction.CallbackContext obj) {

        if (OnStartEngine != null)
            OnStartEngine();

    }

    public void StopEngine(InputAction.CallbackContext obj) {

        if (OnStopEngine != null)
            OnStopEngine();

    }

    public void SteeringHelper(InputAction.CallbackContext obj) {

        if (OnSteeringHelper != null)
            OnSteeringHelper();

    }

    public void TractionHelper(InputAction.CallbackContext obj) {

        if (OnTractionHelper != null)
            OnTractionHelper();

    }

    public void AngularDragHelper(InputAction.CallbackContext obj) {

        if (OnAngularDragHelper != null)
            OnAngularDragHelper();

    }

    public void ABS(InputAction.CallbackContext obj) {

        if (OnABS != null)
            OnABS();

    }

    public void ESP(InputAction.CallbackContext obj) {

        if (OnESP != null)
            OnESP();

    }

    public void TCS(InputAction.CallbackContext obj) {

        if (OnTCS != null)
            OnTCS();

    }

    public void Record(InputAction.CallbackContext obj) {

        if (OnRecord != null)
            OnRecord();

    }

    public void Replay(InputAction.CallbackContext obj) {

        if (OnReplay != null)
            OnReplay();

    }

    public void TrailDetach(InputAction.CallbackContext obj) {

        if (OnTrailerDetach != null)
            OnTrailerDetach();

    }
    private static void _1stGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(0);

    }

    private static void _Gear_canceled(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(-2);

    }

    private static void _2ndGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(1);

    }

    private static void _3rdGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(2);

    }

    private static void _4thGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(3);

    }

    private static void _5thGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(4);

    }

    private static void _6thGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(5);

    }

    private static void _RGear_performed(InputAction.CallbackContext obj) {

        if (OnGearShiftedTo != null)
            OnGearShiftedTo(-1);

    }

#endif

}
