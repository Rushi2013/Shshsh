//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// RCCP UI Canvas that manages the event systems, panels, gauges, images and texts related to the vehicle and player.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Manager")]
public class RCCP_UIManager : RCCP_UIComponent {

    private static RCCP_UIManager instance;

    /// <summary>
    /// Instance of the class.
    /// </summary>
    public static RCCP_UIManager Instance {

        get {

            if (instance == null)
                instance = FindObjectOfType<RCCP_UIManager>();

            return instance;

        }

    }

    /// <summary>
    /// Main car controller.
    /// </summary>
    private RCCP_CarController carController;

    [Header("Event Systems")]
    [Tooltip("Old event system")] public GameObject oldEventReceiver;
    [Tooltip("New event system")] public GameObject newEventReceiver;

    [Header("Panels")]
    [Tooltip("Dashboard panel")] public GameObject dashboard;
    [Tooltip("Mobile controllers panel")] public GameObject mobileControllers;
    [Tooltip("Customization panel")] public GameObject customization;
    [Tooltip("Settings panel")] public GameObject settings;
    [Tooltip("Rebind inputs panel")] public GameObject rebindInputs;

    [Tooltip("Spawn vehicles panel with demo vehicles")] public GameObject spawnVehicles;
    [Tooltip("Spawn vehicles panel with prototype vehicle")] public GameObject spawnPrototypeVehicles;

    public GameObject dnrp;
    public GameObject gearUp;
    public GameObject gearDown;

    [System.Serializable]
    public class SpeedOMeter {

        /// <summary>
        /// Needle gameobject.
        /// </summary>
        public GameObject needle;

        /// <summary>
        /// Turn axis.
        /// </summary>
        public enum TurnAxis {
            X, Y, Z
        }
        public TurnAxis turnAxis = TurnAxis.Z;

        /// <summary>
        /// Turn multiplier.
        /// </summary>
        public float multiplierRotation = -0.0245f;

        /// <summary>
        /// Default rotation of the needle.
        /// </summary>
        private float defRotation = -1f;

        /// <summary>
        /// Current rotation of the needle.
        /// </summary>
        private float currentRotation = 0f;

        /// <summary>
        /// Input value between 0f and 1f.
        /// </summary>
        private float input = 0f;

        /// <summary>
        /// Operates the needle with given input.
        /// </summary>
        /// <param name="_input"></param>
        public void Operate(float _input) {

            //  Taking default rotation of the needle.
            if (defRotation == -1f) {

                switch (turnAxis) {

                    case TurnAxis.X:
                        defRotation = needle.transform.localEulerAngles.x;
                        break;

                    case TurnAxis.Y:
                        defRotation = needle.transform.localEulerAngles.y;
                        break;

                    case TurnAxis.Z:
                        defRotation = needle.transform.localEulerAngles.z;
                        break;

                }

            }

            //  Input.
            input = _input;

            //  Current rotation of the needle.
            currentRotation = defRotation + (input * multiplierRotation);

            //  And turning the needle.
            switch (turnAxis) {

                case TurnAxis.X:
                    needle.transform.localEulerAngles = new Vector3(currentRotation, needle.transform.localEulerAngles.y, needle.transform.localEulerAngles.z);
                    break;

                case TurnAxis.Y:
                    needle.transform.localEulerAngles = new Vector3(needle.transform.localEulerAngles.x, currentRotation, needle.transform.localEulerAngles.z);
                    break;

                case TurnAxis.Z:
                    needle.transform.localEulerAngles = new Vector3(needle.transform.localEulerAngles.x, needle.transform.localEulerAngles.y, currentRotation);
                    break;

            }

        }

    }

    public SpeedOMeter speedometer;

    [Header("Images")]
    [Tooltip("Target image of the following system")] public Image left;
    [Tooltip("Target image of the following system")] public Image right;
    [Tooltip("Target image of the following system")] public Image headlights;
    [Tooltip("Target image of the following system")] public Image ESP;
    [Tooltip("Target image of the following system")] public Image ABS;
    [Tooltip("Target image of the following system")] public Image TCS;
    [Tooltip("Target image of the following system")] public Image NOS;
    [Tooltip("Target image of the following system")] public Image gas;

    [Header("Texts")]
    [Tooltip("Target image of the following system")] public TextMeshProUGUI speedText;
    [Tooltip("Target image of the following system")] public TextMeshProUGUI RPMText;
    [Tooltip("Target image of the following system")] public TextMeshProUGUI gearText;
    [Tooltip("Target image of the following system")] public GameObject recording;
    [Tooltip("Target image of the following system")] public GameObject replaying;

    [Header("Buttons")]
    [Tooltip("Button of rebinding inputs")] public Button rebindInputsButton;

    private void Awake() {

        //  Enabling / disabling correct event system for old and new input system.
        if ((newEventReceiver && oldEventReceiver)) {

            if (RCCPSettings.useNewInputSystem) {

                newEventReceiver.SetActive(true);
                oldEventReceiver.SetActive(false);

            } else {

                newEventReceiver.SetActive(false);
                oldEventReceiver.SetActive(true);

            }

        }

        if (spawnVehicles) {

#if RCCP_DEMO
            spawnVehicles.SetActive(true);
            spawnPrototypeVehicles.SetActive(false);
#else
            spawnVehicles.SetActive(false);
            spawnPrototypeVehicles.SetActive(true);
#endif

        }

    }

    private void OnEnable() {

        //  Firing an event when RCCP Canvas spawns.
        RCCP_Events.Event_OnRCCPUISpawned(this);

        RCCP_InputManager.OnOptions += RCCP_InputManager_OnOptions;

    }

    private void RCCP_InputManager_OnOptions() {

        settings.SetActive(!settings.activeSelf);

        if (!settings.activeSelf)
            customization.SetActive(false);

    }

    private void LateUpdate() {

        //  Finding player vehicle on the scene.
        carController = RCCP_SceneManager.Instance.activePlayerVehicle;

        //  If car controller not found, at disable ui option is enabled, disable panels.
        if (RCCP_SceneManager.Instance.disableUIWhenNoPlayerVehicle) {

            if (RCCPSettings.mobileControllerEnabled && mobileControllers && mobileControllers.activeSelf != carController)
                mobileControllers.SetActive(carController);

            if (dashboard && dashboard.activeSelf != carController)
                dashboard.SetActive(carController);

        }

        if (rebindInputsButton)
            rebindInputsButton.interactable = !RCCP_Settings.Instance.mobileControllerEnabled;

        //  If no car controller found, return.
        if (!carController)
            return;

        if (speedometer != null && speedometer.needle)
            speedometer.Operate(carController.engineRPM);

        //  If vehicle has stability component, control the ESP and ABS images.
        if (carController.Stability) {

            if (ESP)
                ESP.color = carController.Stability.ESPEngaged ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (ABS)
                ABS.color = carController.Stability.ABSEngaged ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (TCS)
                TCS.color = carController.Stability.TCSEngaged ? Color.white : new Color(0f, 0f, 0f, .2f);

        }

        //  If vehicle has lights component, control the light images.
        if (carController.Lights) {

            if (headlights)
                headlights.color = carController.Lights.lowBeamHeadlights ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (left)
                left.color = carController.Lights.indicatorsLeft ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (right)
                right.color = carController.Lights.indicatorsRight ? Color.white : new Color(0f, 0f, 0f, .2f);

            //if (hazard)
            //    hazard.color = carController.Lights.indicatorsAll ? Color.white : new Color(0f, 0f, 0f, .2f);

        }

        //  If vehicle has nos component, control the nos sliders.
        if (carController.OtherAddonsManager && carController.OtherAddonsManager.Nos) {

            if (NOS)
                NOS.fillAmount = carController.OtherAddonsManager.Nos.amount;

        } else {

            if (NOS)
                NOS.fillAmount = 0f;

        }

        //  If vehicle has fuel tank component, control the gas sliders.
        if (carController.OtherAddonsManager && carController.OtherAddonsManager.FuelTank) {

            if (gas)
                gas.fillAmount = carController.OtherAddonsManager.FuelTank.fuelTankFillAmount;

        } else {

            if (gas)
                gas.fillAmount = 0f;

        }

        //  Assigning text of the speed.
        if (speedText)
            speedText.text = carController.speed.ToString("F0");

        //  Assigning text of the rpm.
        if (RPMText)
            RPMText.text = carController.engineRPM.ToString("F0");

        //  Assigning text of the gear.
        if (gearText) {

            if (carController.direction == 1) {

                if (!carController.shiftingNow)
                    gearText.text = (carController.currentGear + 1).ToString("F0");
                else
                    gearText.text = "N";

            } else {

                gearText.text = "R";

            }

        }

        //  If vehicle has recorder component, control the recording and playing texts.
        if (carController.OtherAddonsManager && carController.OtherAddonsManager.Recorder) {

            switch (carController.OtherAddonsManager.Recorder.mode) {

                case RCCP_Recorder.RecorderMode.Neutral:

                    if (recording)
                        recording.SetActive(false);

                    if (replaying)
                        replaying.SetActive(false);

                    break;

                case RCCP_Recorder.RecorderMode.Record:

                    if (recording)
                        recording.SetActive(true);

                    if (replaying)
                        replaying.SetActive(false);

                    break;

                case RCCP_Recorder.RecorderMode.Play:

                    if (recording)
                        recording.SetActive(false);

                    if (replaying)
                        replaying.SetActive(true);

                    break;

            }

        }

        if (carController.Gearbox) {

            if (gearUp)
                gearUp.SetActive(carController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Manual ? true : false);

            if (gearDown)
                gearDown.SetActive(carController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Manual ? true : false);

            if (dnrp)
                dnrp.SetActive(carController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic_DNRP ? true : false);

        } else {

            if (gearUp)
                gearUp.SetActive(false);

            if (gearDown)
                gearDown.SetActive(false);

            if (dnrp)
                dnrp.SetActive(false);

        }

    }

    private void OnDisable() {

        RCCP_Events.Event_OnRCCPUIDestroyed(this);

        RCCP_InputManager.OnOptions -= RCCP_InputManager_OnOptions;

    }

}
