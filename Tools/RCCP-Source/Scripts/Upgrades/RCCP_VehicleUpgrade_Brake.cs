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
/// Upgrades brake torque of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Brake")]
public class RCCP_VehicleUpgrade_Brake : RCCP_Component {

    private int _brakeLevel = 0;

    /// <summary>
    /// Current brake level. Maximum is 5.
    /// </summary>
    public int BrakeLevel {
        get {
            return _brakeLevel;
        }
        set {
            if (value <= 5)
                _brakeLevel = value;
        }
    }

    /// <summary>
    /// Default brake torque of the vehicle.
    /// </summary>
    [HideInInspector] public float defBrake = -1f;

    /// <summary>
    /// Efficiency of the upgrade.
    /// </summary>
    [Range(1f, 2f)] public float efficiency = 1.2f;

    /// <summary>
    /// Updates brake torque and initializes it.
    /// </summary>
    public void Initialize() {

        if (CarController.AxleManager.Axles == null || CarController.AxleManager.Axles != null && CarController.AxleManager.Axles.Count < 1) {

            Debug.LogError("Axles couldn't found in your vehicle. RCCP_VehicleUpgrade_Brake needs it to upgrade the brake level.");
            enabled = false;
            return;

        }

        if (defBrake <= 0)
            defBrake = CarController.FrontAxle.maxBrakeTorque;

        for (int i = 0; i < CarController.AxleManager.Axles.Count; i++)
            CarController.AxleManager.Axles[i].maxBrakeTorque = Mathf.Lerp(defBrake, defBrake * efficiency, BrakeLevel / 5f);

    }

    /// <summary>
    /// Updates brake torque and save it.
    /// </summary>
    public void UpdateStats() {

        if (CarController.AxleManager.Axles == null || CarController.AxleManager.Axles != null && CarController.AxleManager.Axles.Count < 1) {

            Debug.LogError("Axles couldn't found in your vehicle. RCCP_VehicleUpgrade_Brake needs it to upgrade the brake level.");
            enabled = false;
            return;

        }

        if (defBrake <= 0)
            defBrake = CarController.FrontAxle.maxBrakeTorque;

        for (int i = 0; i < CarController.AxleManager.Axles.Count; i++)
            CarController.AxleManager.Axles[i].maxBrakeTorque = Mathf.Lerp(defBrake, defBrake * efficiency, BrakeLevel / 5f);

    }

    public void Restore() {

        BrakeLevel = 0;

        if (defBrake <= 0)
            defBrake = CarController.FrontAxle.maxBrakeTorque;

        CarController.FrontAxle.maxBrakeTorque = defBrake;
        CarController.RearAxle.maxBrakeTorque = defBrake;

    }

}
