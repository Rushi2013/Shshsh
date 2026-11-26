//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Upgrades speed of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Speed")]
public class RCCP_VehicleUpgrade_Speed : RCCP_Component {

    private int _speedLevel = 0;

    /// <summary>
    /// Current speed level. Maximum is 5.
    /// </summary>
    public int SpeedLevel {
        get {
            return _speedLevel;
        }
        set {
            if (value <= 5)
                _speedLevel = value;
        }
    }

    /// <summary>
    /// Default differential ratio of the vehicle.
    /// </summary>
    [HideInInspector] public float defRatio = -1f;

    /// <summary>
    /// Efficiency of the upgrade.
    /// </summary>
    [Range(1f, 2f)] public float efficiency = 1.2f;

    /// <summary>
    /// Updates differential ratio and initializes it.
    /// </summary>
    public void Initialize() {

        if (!CarController.Differential) {

            Debug.LogError("Differential couldn't found in the vehicle. RCCP_VehicleUpgrade_Speed needs it to upgrade the speed level");
            enabled = false;
            return;

        }

        if (defRatio <= 0)
            defRatio = CarController.Differential.finalDriveRatio;

        CarController.Differential.finalDriveRatio = Mathf.Lerp(defRatio, Mathf.Lerp(defRatio, defRatio * .6f, efficiency - 1f), SpeedLevel / 5f);

    }

    /// <summary>
    /// Updates speed and save it.
    /// </summary>
    public void UpdateStats() {

        if (!CarController.Differential) {

            Debug.LogError("Differential couldn't found in the vehicle. RCCP_VehicleUpgrade_Speed needs it to upgrade the speed level");
            enabled = false;
            return;

        }

        if (defRatio <= 0)
            defRatio = CarController.Differential.finalDriveRatio;

        CarController.Differential.finalDriveRatio = Mathf.Lerp(defRatio, Mathf.Lerp(defRatio, defRatio * .6f, efficiency - 1f), SpeedLevel / 5f);

    }

    public void Restore() {

        SpeedLevel = 0;

        if (defRatio <= 0)
            defRatio = CarController.Differential.finalDriveRatio;

        CarController.Differential.finalDriveRatio = defRatio;

    }

}
