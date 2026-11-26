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
/// Limits the maximum speed of the vehicle per each gear.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Limiter")]
public class RCCP_Limiter : RCCP_Component {

    /// <summary>
    /// Gears for limiting the speed.
    /// </summary>
    [Min(0f)] public float[] limitSpeedAtGear = new float[0];

    /// <summary>
    /// Limiting now?
    /// </summary>
    public bool limitingNow = false;

    private void Update() {

        //  Return if vehicle has no gearbox.
        if (!CarController.Gearbox)
            return;

        //  Getting current gear.
        int currentGear = CarController.Gearbox.currentGear;

        //  If speed is higher than the limit, limitingNow.
        if (Mathf.Abs(CarController.speed) > limitSpeedAtGear[currentGear])
            limitingNow = true;
        else
            limitingNow = false;

        //  Cutting fuel if limitingNow is true.
        CarController.Engine.cutFuel = limitingNow;

    }

    public void Reload() {

        limitingNow = false;

    }

    private void Reset() {

        if (!GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Gearbox>(true)) {

            Debug.LogError("Gearbox couldn't found in your vehicle. RCCP_Limiter needs it to limit the speed per gear.");
            enabled = false;
            return;

        }

        limitSpeedAtGear = new float[GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Gearbox>(true).gearRatios.Length];

        for (int i = 0; i < limitSpeedAtGear.Length; i++)
            limitSpeedAtGear[i] = 999f;

    }

}
