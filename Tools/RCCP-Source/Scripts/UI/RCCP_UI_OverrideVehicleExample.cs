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

public class RCCP_UI_OverrideVehicleExample : RCCP_UIComponent {

    /// <summary>
    /// Target vehicle to override inputs.
    /// </summary>
    public RCCP_CarController targetVehicle;

    /// <summary>
    /// Takes the player vehicle in the scene automatically.
    /// </summary>
    public bool takePlayerVehicle = true;

    /// <summary>
    /// New inputs will be used to override the vehicle.
    /// </summary>
    public RCCP_Inputs newInputs = new RCCP_Inputs();

    /// <summary>
    /// Override now?
    /// </summary>
    private bool overrideNow = false;

    /// <summary>
    /// Status text.
    /// </summary>
    public Text statusText;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    public Slider throttle;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    public Slider brake;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    public Slider steering;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    public Slider handbrake;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    public Slider nos;

    private void Update() {

        newInputs.throttleInput = throttle.value;
        newInputs.brakeInput = brake.value;
        newInputs.steerInput = steering.value;
        newInputs.handbrakeInput = handbrake.value;
        newInputs.nosInput = nos.value;

        if (takePlayerVehicle)
            targetVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        if (targetVehicle && overrideNow)
            targetVehicle.Inputs.OverrideInputs(newInputs);

        if (targetVehicle && targetVehicle.Inputs)
            statusText.text = "Status: " + (targetVehicle.Inputs.overridePlayerInputs ? "Enabled" : "False");
        else
            statusText.text = "Status: Disabled";

    }

    public void EnableOverride() {

        overrideNow = true;

        if (targetVehicle)
            targetVehicle.Inputs.OverrideInputs(newInputs);

    }

    public void DisableOverride() {

        overrideNow = false;

        if (targetVehicle)
            targetVehicle.Inputs.DisableOverrideInputs();

    }

}
