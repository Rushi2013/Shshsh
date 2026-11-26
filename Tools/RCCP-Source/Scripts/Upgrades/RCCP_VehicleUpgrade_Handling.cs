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
/// Upgrades traction strength of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Handling")]
public class RCCP_VehicleUpgrade_Handling : RCCP_Component {

    /// <summary>
    /// Current handling level.
    /// </summary>
    private int _handlingLevel = 0;
    public int HandlingLevel {
        get {
            return _handlingLevel;
        }
        set {
            if (value <= 5)
                _handlingLevel = value;
        }
    }

    /// <summary>
    /// Default handling strength.
    /// </summary>
    [HideInInspector] public float defHandling = -1f;

    /// <summary>
    /// Efficiency of the upgrade.
    /// </summary>
    [Range(1f, 2f)] public float efficiency = 1.2f;

    /// <summary>
    /// Updates handling and initializes it.
    /// </summary>
    public void Initialize() {

        if (!CarController.Stability) {

            Debug.LogError("Stability component couldn't found in the vehicle. RCCP_VehicleUpgrade_Handling needs it to upgrade handling.");
            enabled = false;
            return;

        }

        if (defHandling <= 0)
            defHandling = CarController.Stability.tractionHelperStrength;

        CarController.Stability.tractionHelperStrength = Mathf.Lerp(defHandling, defHandling * efficiency, HandlingLevel / 5f);

    }

    /// <summary>
    /// Updates handling strength and save it.
    /// </summary>
    public void UpdateStats() {

        if (!CarController.Stability) {

            Debug.LogError("Stability component couldn't found in the vehicle. RCCP_VehicleUpgrade_Handling needs it to upgrade handling.");
            enabled = false;
            return;

        }

        if (defHandling <= 0)
            defHandling = CarController.Stability.tractionHelperStrength;

        CarController.Stability.tractionHelperStrength = Mathf.Lerp(defHandling, defHandling * efficiency, HandlingLevel / 5f);

    }

    private void Update() {

        if (!CarController.Stability) {

            Debug.LogError("Stability component couldn't found in the vehicle. RCCP_VehicleUpgrade_Handling needs it to upgrade handling.");
            enabled = false;
            return;

        }

    }

    public void Restore() {

        HandlingLevel = 0;

        if (defHandling <= 0)
            defHandling = CarController.Stability.tractionHelperStrength;

        CarController.Stability.tractionHelperStrength = defHandling;

    }

}
